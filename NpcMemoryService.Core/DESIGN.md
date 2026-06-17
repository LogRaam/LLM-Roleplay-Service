# NpcMemoryService — Design Notes

This document captures the architectural decisions, design rationale, and known
limitations of the NpcMemoryService SDK. It is intended for project maintainers,
modders who consume the library, and future contributors evaluating extensions.

It is **not** an API reference (the code carries that responsibility through XML
documentation) nor a usage guide (see the repository `README.md`).

---

## 1. Project intent

The system provides persistent, semantically rich memory for NPCs in games whose
dialogue is driven by an LLM. It exists because most LLM-based NPC mods
(notably AI Influence for Bannerlord) suffer from a structural defect: the model
has no durable recollection between encounters, so every conversation effectively
resets the character.

The differentiator of this SDK is **memory that travels through the save game**
and **influences the LLM at every dialogue turn**.

---

## 2. Core architectural decisions

### 2.1. Layered structure

```
Core (netstandard2.0)                  ← pure logic, no UI, no game integration
├── Models                              ← profiles, world state, parsed responses
├── Parsing                             ← decodes the LLM's bracketed-section output
├── Prompts                             ← assembles system prompts from state
├── LlmClient                           ← internal protocol + provider adapters
├── Services                            ← orchestrates one chat turn end-to-end
├── Storage                             ← persistence with Unit of Work semantics
└── Compression                         ← LLM-driven memory compaction
```

`Core` carries no dependency on game frameworks, UI toolkits, or DI containers.
Consumers (console runner, future WPF diagnostic, Bannerlord mod) wire dependencies
themselves. This keeps `Core` testable and forward-compatible with environments
we haven't anticipated.

### 2.2. Internal LLM protocol (anti-corruption layer)

`ILlmClient` speaks our own message format (`LlmRequest`, `LlmResponse`,
`LlmMessage`). Concrete providers (`OpenRouterClient`, future `OllamaClient`,
etc.) translate to and from the provider's native wire format.

This isolates the rest of the system from provider-specific concerns
(authentication headers, parameter naming, response shapes). When a provider
breaks compatibility or a new provider appears, only one adapter changes.

### 2.3. The bracketed-section response format

The LLM emits structured output via tagged sections: `[DIALOGUE]`, `[MEMORY]`,
`[EVENT]`, `[REPUTATION]`. The parser is tolerant by design:

- Missing sections degrade to `null` rather than throwing
- Malformed sections are skipped; sibling sections survive
- Unknown event types fall back to `NotableEventType.Other`

This format was preferred over JSON output for two reasons. First, LLMs follow
free-form delimited formats with higher reliability than strict JSON across
providers, especially when the response is mostly natural language. Second,
malformed JSON aborts the whole parse; malformed bracket sections degrade gracefully.

The contract is frozen by the unit test suite — changes to format require
deliberate test updates first.

### 2.4. Two-tier memory model

- **`MemoryDigest`** — appended one entry per conversation. Compact format:
  `[topic] sentiment:X decision:Y`. Currently not injected into the prompt
  (proven cryptic to LLMs); retained for diagnostics. May be removed later.

- **`Events` (list of `NotableEvent`)** — natural-language summaries of
  significant moments. This is the **primary long-term memory** surfaced to
  the LLM in every prompt. Events are emitted by the LLM via `[EVENT]` sections
  according to explicit trigger criteria in the system prompt.

The split exists because raw conversation summaries (the digest) lose meaning
quickly when stripped to keywords, while natural-language event summaries
preserve narrative significance over arbitrary time spans.

### 2.5. The `Farewell` event as encounter boundary

Each dialogue scene ends with a `Farewell` event. This serves three functions:

- **Captures pending obligations** — the summary preserves what the player
  promised at parting, allowing the NPC to follow up on next encounter.
- **Triggers `ChatSession` reset** — the consumer (console runner, mod) clears
  the per-encounter dialogue history when a Farewell is detected, preventing
  unbounded growth of conversation context.
- **Distinguishes continuation from reunion** — the LLM can detect that the
  most recent event is a past-day farewell and frame its next response as a
  reunion rather than a continuation.

In Bannerlord, the mod can emit a Farewell directly when the dialogue UI
closes — same data structure, different trigger.

### 2.6. Unit of Work persistence

`INpcMemoryStore` separates synchronous in-memory operations (`Get`, `Set`,
`Remove`) from explicit persistence (`InitializeAsync`, `CommitAsync`). The
working set lives in memory between commits.

This mirrors game save semantics: a crash mid-conversation leaves no partial
state on disk. Only an explicit save persists changes. In Bannerlord, the mod
will hook `CommitAsync` to the campaign save behavior; in the console runner,
the `save` command triggers it.

The `JsonFileNpcMemoryStore` implementation uses temp-file + rename for atomic
commits, and prunes orphan files for removed profiles.

### 2.6.1. Resilient single-blob serialization

`JsonFileNpcMemoryStore` gets crash-isolation for free: one file per NPC means a
corrupt or un-round-trippable profile costs at most that one file. Consumers that
persist every profile as a **single blob** — a game's native save slot
(Bannerlord's `IDataStore`), a config string, a database column — have no such
natural isolation: one bad profile in the blob can make the entire memory
unreadable, which for a save-embedded store means a campaign that won't load.

`ResilientProfileBundle` (in `Storage`) is the engine-agnostic kernel for that case:

- **`Serialize`** proves each profile survives a full serialize→deserialize
  round-trip *before* including it; any that cannot is dropped and reported through
  a `DroppedProfileHandler` callback. The returned document is always valid.
- **`Deserialize`** parses the outer document as raw tokens, then materializes each
  entry individually; a single malformed entry is skipped and reported through a
  `SkippedProfileHandler`. A malformed *outer* document still throws, leaving the
  recovery decision to the consumer.

The helper never references a game API. A consumer wraps the string in whatever its
host save system expects — the Bannerlord store, for instance, encodes it as a
UTF-8 `byte[]` to dodge the native string-length ceiling and hands that to
`IDataStore`. The fragile part is written and tested once in the SDK; each store
keeps only its own thin bridge.

This pattern was extracted from the Bannerlord store after a production bug where
the native string serializer corrupted saves past ~65535 bytes once many NPCs were
remembered. The `byte[]` bridge solved the size ceiling; this helper is the
reusable resilience layer that travels with it.

### 2.7. Prompt caching via section ordering

Section order in `PromptBuilder` is **stability-descending** to maximize prefix
cache hit rate on providers with automatic prefix caching (xAI Grok, OpenAI
GPT-4o) or explicit cache control (Anthropic Claude):

1. Format instructions — identical across all NPCs and all turns
2. NPC identity & personality — stable per NPC
3. Background context — changes only on compression
4. Event history — changes when significant turns occur
5. Current stance — volatile (per turn)
6. World state — volatile (per turn)

The `OpenRouterClient` also emits a `cache_control: ephemeral` breakpoint on
the system message; providers that support it honor it, others ignore it.

Observed cache hit rate with Grok 4.1 Fast: 90–95% of prompt tokens served
from cache after the first turn of a session.

### 2.8. LLM-driven memory compression

`LlmMemoryCompressor` asks the LLM to identify which events can be dropped
because later, stronger events have superseded them. The LLM returns indices
to keep plus a background summary of dropped events.

Safety net: regardless of the LLM's decision, the implementation always
preserves:

- The first `FirstMeeting` event
- The N most recent events (configurable)
- Every `Betrayal`, `Intimacy`, `Confrontation`, `Agreement`, and `Farewell`
  event

This ensures narratively critical events are never lost even if the LLM
hallucinates or misjudges. Compression is triggered manually for now (`compress`
command in the console runner); for the mod, a token-threshold trigger is
planned.

### 2.9. Scene discipline (single-speaker rule)

The system prompt explicitly forbids the LLM from voicing characters other
than the active NPC. The model may narrate other characters' presence and
brief reactions but may not simulate full dialogue exchanges with them.

This addresses a tendency observed in tests where the LLM, given narrative
latitude, would invent and voice secondary characters (e.g., Raganvad making
his daughter Siga speak full lines). In Bannerlord this is moot because the
game engine only puts one character in dialogue at a time, but the constraint
keeps the console runner aligned with future game behavior and prevents
inconsistent memory states.

### 2.10. Game actions — narrative-to-mechanical bridge

The `[ACTION]` section is the LLM's channel to request concrete changes in
the game world. While `[EVENT]` *describes* what happened (retrospective,
for memory), `[ACTION]` *commands* the engine (proactive, to change state).
When an NPC says *"Guards, take him to the dungeons"*, the LLM should emit
both an event (capture) and an action (`imprison`) — the mod consumes the
action and effects the imprisonment via the game's API.

Design constraints:

- **The SDK is action-vocabulary agnostic.** `GameAction.Type` is a free
  string. Each consumer defines its own supported actions and how to execute
  them. The Bannerlord mod's vocabulary will be different from a hypothetical
  visual-novel consumer's vocabulary.
- **The consumer provides the vocabulary to the LLM** via
  `PromptBuilder.ActionVocabulary`, a list of `GameActionDefinition`
  entries. Each entry describes the action and its parameters in natural
  language. The LLM only emits actions it has been told exist.
- **Multiple actions per turn are allowed.** A single response can imprison
  the player, confiscate weapons, and schedule a follow-up interrogation.
- **Unknown actions degrade gracefully.** If the LLM emits an action the
  consumer doesn't recognize (vocabulary drift, malformed type), the
  consumer logs and ignores it. No exception, no broken turn.
- **The free-form `Parameters` dictionary** lets each action carry its own
  data without requiring SDK changes when vocabulary expands.

The bridge to game execution is consumer-defined. In Bannerlord, this will
take the form of an `IGameActionHandler` interface and a dispatcher that
maps `action.Type` to TaleWorlds API calls. The SDK provides the directive;
the mod implements the verb.

---

## 3. Known limitations and future considerations

### 3.1. Cross-NPC memory propagation

Currently each NPC has its own memory. If the player performs an action in
the presence of multiple NPCs (e.g., insults Lord A in front of Lord B), only
the active NPC's memory is updated. Lord B will not "remember" witnessing
the insult.

In Bannerlord this is rarely a practical issue because dialogues are one-on-one.
A future enhancement could allow the active NPC's response to flag which other
NPCs were "present" so their profiles receive a derived event with reduced
weight. This is deferred until a concrete use case appears.

### 3.2. Scene context awareness

Observed problem (from AI Influence experience): an intimate dialogue can
develop in a public tavern setting, narratively progressing toward physical
intimacy that would be wildly inappropriate to the location. The model has
no awareness of the physical context of the encounter.

Proposed direction (not yet implemented):

- Extend `WorldState` with a `SceneContext` field describing the physical
  setting: location type (tavern / throne room / private chamber / wilderness),
  observers present, social register (formal / casual / hostile).
- Inject this into the system prompt with explicit guidance: *"You are aware
  of your surroundings. If the player proposes something inappropriate to
  this setting, you may refuse, redirect, or propose moving to a more suitable
  location."*
- Allow NPCs to emit a structured "scene change request" the consumer can
  honor: *"Let us continue this somewhere more private."*

This is best designed with concrete Bannerlord integration constraints in hand
(what scene types exist, how does the game expose them, can we trigger location
changes from a mod, etc.). Deferred.

### 3.3. Player physical state awareness

Related to scene context but distinct: the LLM has no awareness of the
player's **physical condition or constraints** during a conversation.

Observed problem (console runner test): a player held captive by an NPC
typed `farewell` and triggered a normal encounter reset. The Farewell
mechanism itself behaved correctly (an encounter unit ended, the chat
session cleared), but the LLM's response had no grounds to reject the
implicit "I depart" — Raganvad has no way to know the player is in chains.

In Bannerlord this is largely mitigated by the engine:

- The game tracks player state (free, captive, restrained, wounded, etc.)
- Dialogue options are filtered by the UI based on that state
- The mod can read this state and include it in the prompt

Future enhancement: introduce a `PlayerState` field (separate from `WorldState`)
covering:

- Physical condition (free, captive, restrained, wounded)
- Equipment status (armed, disarmed)
- Spatial constraint (which room/zone the player can/cannot leave)

The system prompt would direct the LLM to honor these constraints when
generating dialogue and events: refuse narratively impossible actions, frame
Farewells appropriately for the situation, maintain consistency with the
game's truth.

This is also deferred to mod integration time, when concrete game-state
representations are available.

### 3.4. `MemoryDigest` removal

`MemoryDigest` is currently retained for diagnostic value but is no longer
injected into prompts. Once production use confirms the events-only model
suffices, `MemoryDigest` and `ConversationMemory` can be removed entirely,
along with the `[MEMORY]` section in the LLM response format.

### 3.5. Event de-duplication

Observed in tests: the LLM occasionally emits two `[EVENT]` entries for what
is essentially one moment (e.g., "presents the necklace" and "gifts the
necklace" as two separate Flirt events within the same turn). The compressor
will eventually handle this, but a lighter de-dup pass at parse time could
catch the trivial cases.

### 3.6. Async LLM calls during commit

The Bannerlord save system is synchronous. `CommitAsync` on the store is
async-friendly today, but any future LLM call inside the commit path (e.g.,
running compression automatically on save) must be either fire-and-forget
or run before the save trigger fires. This needs deliberate design when
integrating with the mod.

### 3.7. Action execution feedback to the LLM

Currently, `[ACTION]` directives flow one-way: LLM → consumer → game engine.
If an action fails (engine rejects it, parameters invalid, prerequisites
not met), the LLM has no way to know on the next turn.

This will matter in Bannerlord. Example: Raganvad emits `give_money` for
10,000 denars, but his clan treasury holds 200. The mod can't fulfill the
request. On the next turn, the LLM would happily reference the gift as if
it succeeded.

Proposed future direction: the consumer can append a synthetic system
message between turns reporting outcomes — *"Your action `give_money(10000)`
failed: insufficient funds. You actually transferred 200."* The LLM
incorporates this into its next response and may emit a corrective dialogue
turn.

This requires the `NpcChatService` to support injecting system messages
between user turns, or extending `ChatSession` with a richer message-kind
model. Deferred until the mod has concrete failure modes to test against.

---

## 4. Decisions deliberately deferred

These are options we considered and chose **not** to pursue at this stage. They
are listed so future contributors don't re-litigate without new information.

- **`ILlmProvider` registry pattern.** Discussed and skipped (YAGNI). Adding a
  second provider currently requires writing one `ILlmClient` implementation
  and wiring it; no registry is needed.
- **Restructuring `LlmRequest` to expose stable / volatile prompt parts.**
  Discussed for explicit cache control. The current approach (single system
  prompt + cache breakpoint at end) works well enough; revisit only if a
  provider requires a different split.
- **Persisting `ChatSession` across encounters.** Confirmed not desired —
  dialogues are atomic, only the digested memory carries forward.
- **DI container in `Core`.** Each consumer wires dependencies itself with or
  without a container of its choice.

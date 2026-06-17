# NpcMemoryService

A modding SDK that wires a Large Language Model into a game so NPCs can hold real
conversations **and remember them across the whole campaign**. The memory is
injected into the model on every dialogue turn, so characters react to what the
player did days — or months of game-time — ago.

The core is engine-agnostic: it targets **.NET Standard 2.0**, depends only on
**Newtonsoft.Json**, and has zero dependency on any game engine, UI toolkit, or DI
container. It was built to power the Bannerlord mod *Calradia Remembers*, but
anything that can run C# and make an HTTP call can use it.

- **Architecture & design rationale:** see [`NpcMemoryService.Core/DESIGN.md`](NpcMemoryService.Core/DESIGN.md).
- **API reference:** the XML documentation on the public types in `NpcMemoryService.Core`.
- **Usage:** this file.

---

## Project layout

| Project | Target | Role |
|---|---|---|
| `NpcMemoryService.Core` | netstandard2.0 | The library: models, parsing, prompts, LLM client, services, storage, compression. **This is what you reference.** |
| `NpcMemoryService.ConsoleRunner` | net8.0 | A complete, runnable **reference integration** — an interactive chat harness. Read its `Program.cs` to see the full wiring end-to-end. |
| `NpcMemoryServiceTests` | net10.0 | Unit tests that freeze the response-format contract. |

---

## Requirements

- A .NET Standard 2.0-compatible host (Bannerlord's .NET Framework runtime, modern
  .NET, Unity, etc.)
- `Newtonsoft.Json` 13.0.1
- An **OpenRouter** API key (or your own `ILlmClient` adapter for another provider)

---

## Quick start

The whole loop is: **wire the services once → run a turn → apply the result →
commit on save.**

### 1. Wire the services (once, at startup)

```csharp
// --- LLM provider ---
var llmConfig = new OpenRouterConfig
{
    ApiKey = GetYourApiKeySomehow(),   // never hard-code or commit this
    Model  = "x-ai/grok-4.20"          // any OpenRouter model id
};
var http      = new HttpClient();
var llmClient = new OpenRouterClient(http, llmConfig);

// --- Parsing + prompt assembly ---
var parser = new SectionResponseParser();
var promptBuilder = new PromptBuilder
{
    // Tell the model which game actions it may request. YOU define what each means.
    ActionVocabulary = new List<GameActionDefinition>
    {
        new() { Type = "imprison",   Description = "Take the player prisoner." },
        new() { Type = "give_money", Description = "Give denars to the player.",
                Parameters = new[] { "amount" } },
        // ...as many as your game supports
    }
};

// --- Orchestration + persistence ---
var chatService = new NpcChatService(llmClient, parser, promptBuilder);
var store       = new JsonFileNpcMemoryStore(
                      new JsonFileStoreConfig { Directory = "npc-data" });
await store.InitializeAsync();
```

### 2. Run a turn and apply the result

```csharp
NpcProfile npc = store.Get("npc_raganvad") ?? new NpcProfile
{
    Id          = "npc_raganvad",
    Name        = "Raganvad",
    Faction     = "Sturgia",
    Clan        = "Vagiroving",
    Personality = "Proud, honorable Sturgian king. Values loyalty above all."
};

var world   = new WorldState { CurrentDay = 142,
                               ActiveConflicts = "At war with the Western Empire." };
var session = new ChatSession();   // one session per encounter

NpcChatResult result = await chatService.ChatAsync(
    npc, world, session, "My king, I've come to pledge my sword.");

if (result.IsSuccess)
{
    ParsedResponse r = result.Response!;
    Display(npc.Name, r.Dialogue);            // what the NPC says

    foreach (GameAction a in r.Actions)       // execute requested actions
        ExecuteInYourGame(a);                 // a.Type + a.Parameters

    npc.ApplyConversationResult(r, world.CurrentDay);  // commit memory/reputation/event
    store.Set(npc);
}
```

### 3. End the encounter, and persist on save

```csharp
// The model emits a Farewell event when the encounter is over:
if (result.Response?.NewEventData?.Type == NotableEventType.Farewell)
    session = new ChatSession();   // clear per-encounter history; long-term memory persists

// Flush memory to storage — call this from your game's SAVE event:
await store.CommitAsync();
```

That is the entire integration surface. Everything below is detail.

---

## The reference integration: ConsoleRunner

`NpcMemoryService.ConsoleRunner` is the fastest way to understand the SDK: it is a
small interactive program that runs the exact loop above against a live model. Its
`Program.cs` is meant to be read as documentation — `BuildSession()` shows the full
dependency wiring, `RunChatTurn()` shows a turn, and `RunCompression()` shows memory
compaction.

**Run it:**

```bash
# 1. Set your key (the runner reads this environment variable)
setx OPENROUTER_API_KEY "sk-or-..."      # Windows; reopen the shell afterwards
#   export OPENROUTER_API_KEY="sk-or-..." # macOS/Linux

# 2. Run
dotnet run --project NpcMemoryService.ConsoleRunner
```

It loads (or creates) the NPC *Raganvad* and drops you into a chat. Anything you
type is a message to him; a few words are commands instead:

| Command | Effect |
|---|---|
| `save` | Persist memory to the `npc-data` folder (`CommitAsync`). |
| `memory` | Show the compact per-conversation digest. |
| `events` | List the NPC's long-term notable events + background context. |
| `compress` | Run LLM-driven memory compression (needs ≥12 events). |
| `day +N` / `day N` | Advance game time (so the next encounter is framed as a reunion). |
| `debug` | Toggle a panel showing the parsed `[EVENT]` / `[REPUTATION]` / `[ACTION]` / token sections. |
| `quit` | Exit **without** saving. |

Turn `debug` on, have a short conversation, then type `events` and `save` — you will
see memory accumulate, persist to JSON, and survive a restart.

---

## The response protocol

The model replies in tolerant, bracketed sections; `SectionResponseParser` decodes
them into a typed `ParsedResponse`. Missing sections degrade to `null`, malformed
sections are skipped, and siblings survive — a flaky model never breaks a turn.

| Section | Parsed into | Meaning |
|---|---|---|
| `[DIALOGUE]` | `Dialogue` | What the NPC says (the only guaranteed section). |
| `[NARRATION]` | `Narration` | Second-person scene narration of physical action. |
| `[EVENT]` | `NewEventData` | A significant moment to store in long-term memory. |
| `[REPUTATION]` | `Reputation` | A change in the NPC's opinion of the player. |
| `[ACTION]` | `Actions` | Zero or more commands to your game engine. |
| `[QUEST]` / `[QUEST_COMPLETE]` / `[QUEST_ABANDON]` | `QuestGiven` / `QuestCompleted` / `QuestAbandoned` | Informal quests offered, completed, or abandoned. |
| `[WITNESS_REACTION]` | `WitnessReactions` | Reactions from other characters present. |

`NpcProfile.ApplyConversationResult` applies memory, reputation, and the event in one
call; the other sections (actions, quests, witnesses) are yours to interpret.

---

## Game actions (the narrative-to-mechanical bridge)

`[ACTION]` is how the LLM asks your game to *do* something. The SDK is
**vocabulary-agnostic**:

- You publish the available actions via `PromptBuilder.ActionVocabulary`
  (`GameActionDefinition` entries describing each action and its parameters in plain
  language). The model only emits actions you told it exist.
- `GameAction.Type` is a free string and `GameAction.Parameters` is a free-form
  dictionary, so you extend your vocabulary without changing the SDK.
- Multiple actions per turn are allowed. Unknown actions degrade gracefully — log
  and ignore them; the turn still succeeds.

The SDK gives you the directive; **your consumer implements the verb** (maps
`action.Type` to your engine's API).

---

## Persistence

Persistence sits behind one interface, `INpcMemoryStore`, with two hooks:
`InitializeAsync` (load) and `CommitAsync` (save). The working set lives in memory
between commits, so a crash mid-conversation leaves no partial state.

**Two ways to persist:**

1. **`JsonFileNpcMemoryStore` (bundled).** One JSON file per NPC, atomic temp-file +
   rename on commit. Works in any host, on any platform, out of the box. One bad file
   costs at most one profile (filesystem-level isolation).

2. **Your own `INpcMemoryStore`.** Want the memory embedded inside your game's native
   save instead? Implement the interface and point `CommitAsync` at your save event.
   The SDK never touches your save format — this is what keeps the core
   engine-agnostic.

If your store packs **all profiles into a single blob** (a native save slot, a
config string, a DB column), use `ResilientProfileBundle` so one corrupt profile
can't brick the whole blob:

```csharp
// Save
string json  = ResilientProfileBundle.Serialize(profiles,
                   (id, name, ex) => Log($"dropped '{id}' ({name}): {ex.Message}"));
byte[] blob  = Encoding.UTF8.GetBytes(json);   // wrap however your host wants
WriteToHostSave(blob);

// Load
string json  = Encoding.UTF8.GetString(ReadFromHostSave());
var profiles = ResilientProfileBundle.Deserialize(json,
                   (id, ex) => Log($"skipped '{id}': {ex.Message}"));
```

`Serialize` proves each profile round-trips before including it; `Deserialize`
materializes each entry individually and skips the bad ones. (See
`Calradia Remembers/.../Storage/BannerlordNpcMemoryStore.cs` for a real
single-blob store: it wraps this helper in a UTF-8 `byte[]` over TaleWorlds'
`IDataStore`.)

---

## Providers

The SDK ships an **OpenRouter** adapter (`OpenRouterClient`), which gives access to
dozens of models — xAI Grok, Anthropic Claude, OpenAI GPT, Meta Llama, and more —
behind one API key. Set the model with any OpenRouter model id (e.g.
`x-ai/grok-4.20`, `anthropic/claude-sonnet-4`, `openai/gpt-4o`).

To add another provider (a local Ollama, a direct vendor API), implement
`ILlmClient` — a single `CompleteAsync(LlmRequest, CancellationToken)` method that
translates our internal protocol to and from the provider's wire format. Nothing
else in the system changes. Implementations never throw; they return an unsuccessful
`LlmResponse` instead.

---

## Memory compression

As an NPC's history grows, `LlmMemoryCompressor.CompressAsync` asks the model which
events are superseded and folds them into a short background summary. A safety net
**always** preserves the first meeting, the most recent events, and narratively
critical moments (betrayals, intimacy, confrontations, agreements, farewells) even
if the model misjudges. Trigger it on a token threshold, or manually (see the
ConsoleRunner's `compress` command).

---

## License & support

See the bundled documentation. Questions and integration feedback are welcome.
Built for, and proven by, *Calradia Remembers*.

// Code written by Gabriel Mailhot, 23/06/2026.

#region

using System;
using System.Collections.Generic;
using NpcMemoryService.Core.Services;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   An NPC's identity and accumulated memory of the player.
   ///   Persisted per save game via INpcMemoryStore.
   /// </summary>
   public sealed class NpcProfile
   {
      // Every collection below is backed by a field whose init accessor coalesces null away, rather than the
      // shorter "{ get; init; } = new()". The initializer alone is not enough: a property initializer only
      // runs before deserialization, and an explicit null in the stored JSON then OVERWRITES it through the
      // init setter. The profile handed back would carry a null list that no caller expects, and the mod
      // dereferences these in dozens of places (a courier respawn walking SentLetters, a quest pass walking
      // ActiveQuests). Absorbing it here kills the whole class of NullReferenceException at the one place it
      // can be prevented; guarding each call site instead is whack-a-mole against a save file.
      private readonly List<InformalQuest> _activeQuests = new();

      /// <summary>
      ///   Tasks this NPC has asked the player to accomplish. Holds quests in every
      ///   lifecycle state — outstanding, satisfied-awaiting-reward, and recently
      ///   terminated — so the giver can reference both pending work and how the
      ///   player has discharged past obligations. Each quest carries its own evidence,
      ///   so nothing accumulates into a shared log. Persisted across sessions.
      /// </summary>
      public List<InformalQuest> ActiveQuests
      {
         get => _activeQuests;
         init => _activeQuests = value ?? new List<InformalQuest>();
      }

      /// <summary>
      ///   Optional player-authored backstory: roleplay flavor only (color, not behavior — conduct
      ///   stays driven by traits). Synced from the host's character-overrides file before each
      ///   prompt, so it is never authoritative state and never goes stale in the save.
      /// </summary>
      public string? AuthoredBackstory { get; set; }

      /// <summary>
      ///   Optional player-authored CONVICTION: what this character holds to be true, and therefore what they
      ///   pursue. Distinct from <see cref="AuthoredBackstory" />, which the prompt deliberately confines to
      ///   voice and temperament ("HOW you speak and WHO you are, NOT what you decide"): a conviction is a
      ///   MOTIVE, and it may be false. It grants no powers, imports no lore, and never reaches past the bridge
      ///   (it may make an NPC want to plot; it can never make a plot exist). Synced from the host's
      ///   character-overrides file before each prompt, so it is never authoritative state in the save.
      /// </summary>
      public string? AuthoredConviction { get; set; }

      /// <summary>
      ///   Background narrative context produced when older events are compressed away.
      ///   Preserves the gist of dropped events as a short prose paragraph.
      /// </summary>
      public string? BackgroundContext { get; set; }

      public required string Clan { get; init; }

      /// <summary>
      ///   The clan's collective standing with the player, mirrored from the host
      ///   game's clan-level relation just before each prompt build. Null when the
      ///   consumer does not supply it (e.g. the console runner) — in that case the
      ///   prompt falls back to showing only the personal opinion.
      ///   Transient: not a durable part of this NPC's identity.
      /// </summary>
      public int? ClanRelationWithPlayer { get; set; }

      private readonly Dictionary<string, int> _courtActionCooldowns = new();

      /// <summary>
      ///   Court-action cooldown clocks (romance audit M-B6): marker -> the in-game day that action last fired.
      ///   A STRUCTURED store the LLM memory compaction cannot rewrite, unlike the old approach of scanning the
      ///   free-text event summaries for a marker (which vanished on compaction, silently resetting the cooldown
      ///   so the action could be spammed). Additive and save-safe: an older save loads this empty, and
      ///   <c>CourtActionResolver</c> falls back to the legacy event scan until a fresh action stamps it here.
      /// </summary>
      public Dictionary<string, int> CourtActionCooldowns
      {
         get => _courtActionCooldowns;
         init => _courtActionCooldowns = value ?? new Dictionary<string, int>();
      }

      private readonly List<DiscoveredTrait> _discoveredTraits = new();

      /// <summary>
      ///   Personal traits and preferences this NPC has revealed to the player
      ///   through conversation. Empty until the player has had meaningful exchanges.
      ///   Grows over time as the NPC opens up; each entry is deduplicated by
      ///   <see cref="DiscoveredTrait.Key" /> so the same fact is never recorded twice.
      ///   Displayed in the encyclopedia discovery section.
      /// </summary>
      public List<DiscoveredTrait> DiscoveredTraits
      {
         get => _discoveredTraits;
         init => _discoveredTraits = value ?? new List<DiscoveredTrait>();
      }

      private readonly List<NotableEvent> _events = new();

      /// <summary>
      ///   Significant past events with natural-language summaries.
      ///   This is the primary long-term memory surfaced to the LLM.
      /// </summary>
      public List<NotableEvent> Events
      {
         get => _events;
         init => _events = value ?? new List<NotableEvent>();
      }

      public required string Faction { get; init; }

      /// <summary>
      ///   Companion HAPPINESS — their satisfaction IN the player's service (0..100), distinct from the
      ///   stance axes (their regard FOR the player). Only meaningful for the player's own companions.
      ///   Defaults to 60 (the neutral baseline), so a freshly-met or older-save profile reads as content.
      /// </summary>
      public int Happiness { get; set; } = 60;

      /// <summary>
      ///   Companion WAR-WEARINESS — the accumulated toll of war (0..100), a SEPARATE axis from happiness: it
      ///   climbs with wounds and captures rather than drifting like a mood, and a spent veteran wants an
      ///   honorable retirement. Only meaningful for the player's own companions. Defaults to 0 (untouched by war),
      ///   so a freshly-met or older-save profile reads as fresh.
      /// </summary>
      public int WarWeariness { get; set; } = 0;

      /// <summary>
      ///   True once a war-weary LANDED companion (a governor / party leader) has asked to step back from the
      ///   FIELD to tend their fief and the player granted it: they stay in the clan but should not be marched to
      ///   war. The host reasserts this if the player re-adds them to a party. Defaults false.
      /// </summary>
      public bool SteppedBackFromWar { get; set; } = false;

      public required string Id { get; init; }

      /// <summary>
      ///   Dynastic succession: when the player died and an heir took over, the name of the deceased
      ///   predecessor whose history this profile now records. Null in normal play. When set, the
      ///   prompt frames the recorded events as INHERITED — the NPC deals with the heir, not the dead.
      /// </summary>
      public string? InheritedFromName { get; set; }

      /// <summary>The kinship word for <see cref="InheritedFromName" /> as seen from the heir (e.g. "father").</summary>
      public string? InheritedKinship { get; set; }

      /// <summary>
      ///   Negotiation Phase 3: set once a cynical man has had the female player in a leveraged
      ///   transaction (intimacy traded for a favour). He remembers it and may press the advantage in
      ///   later talks. Adult-gated and only ever set for the exploiter archetype; false for everyone else.
      /// </summary>
      public bool IntimacyLeverageHeld { get; set; }

      /// <summary>
      ///   A brigand captor has LEARNED the player's name because the prisoner gave it up in a scene (a
      ///   brigand has no lords' register, so this is the only way he could know it). Persisted, so a
      ///   recurring nemesis keeps knowing it across captures. Additive and save-safe: old saves load false.
      ///   Ratified with Gabriel 2026-07-19. Meaningless for a lord captor (who knows names anyway) and never
      ///   persisted for an ephemeral synthesized bandit; it earns its keep on a nemesis profile.
      /// </summary>
      public bool CaptorLearnedPlayerName { get; set; }

      /// <summary>
      ///   This NPC's sex, mirrored from the live hero. Stated plainly in the prompt's identity
      ///   line so the LLM never has to guess pronouns (a female NPC was once narrated as "his").
      ///   Refreshed each session, so profiles created before this field are corrected on load.
      /// </summary>
      public bool IsFemale { get; set; }

      /// <summary>
      ///   The hero's age in years, refreshed each conversation. 0 when unknown, e.g. a profile
      ///   from a save written before this field existed.
      /// </summary>
      public int Age { get; set; }

      /// <summary>
      ///   Campaign-time hour (<c>CampaignTime.Now.ToHours</c>) of the last
      ///   POSITIVE relation gain granted through a relation-changing action.
      ///   Used by the consumer to throttle relationship growth (at most one
      ///   routine gain per cooldown window). Null = no gain recorded yet.
      ///   Game-agnostic: the SDK only stores it; the consumer defines the policy.
      /// </summary>
      public double? LastRelationGainHour { get; set; }

      /// <summary>
      ///   Regard audit C1: the in-game hour of the last GRANTED intimacy relation bonus (the +3, cooldown-
      ///   bypassing gain). Its own dedicated cooldown, separate from <see cref="LastRelationGainHour" />, is what
      ///   stops a model that tags every reply "intimacy" from farming +3 per turn. Null = none granted yet.
      ///   Additive; absent on old saves (reads as null = no cooldown pending).
      /// </summary>
      public double? LastIntimacyGainHour { get; set; }

      /// <summary>
      ///   Duels: the game day on which the player and this NPC last crossed blades. The consumer's cooldown
      ///   policy reads it to refuse a fresh challenge too soon after the last one, so a duel stays a grave
      ///   matter instead of a daily treadmill. Null = these two have never dueled, which is never on cooldown.
      ///   Additive; absent on old saves (reads as null = never dueled).
      ///   Game-agnostic: the SDK only stores it; the consumer defines the cooldown window.
      /// </summary>
      public int? LastDuelDay { get; set; }

      /// <summary>
      ///   Compact per-conversation summaries. Useful for diagnostics.
      ///   Not currently injected into the prompt — see <see cref="Events" />.
      ///   Null means the NPC has never met the player.
      /// </summary>
      public string? MemoryDigest { get; set; }

      public required string Name { get; init; }

      public string? Personality { get; set; }

      private readonly List<PlayerLetter> _receivedPlayerLetters = new();

      /// <summary>
      ///   Letters the player has sent to this NPC. In transit until
      ///   <see cref="PlayerLetter.DeliveryDay" />; injected into the NPC's system
      ///   prompt while delivered but unread, then marked read after the first
      ///   dialogue response that follows delivery.
      ///   Persisted across sessions via the store's JSON serializer.
      /// </summary>
      public List<PlayerLetter> ReceivedPlayerLetters
      {
         get => _receivedPlayerLetters;
         init => _receivedPlayerLetters = value ?? new List<PlayerLetter>();
      }

      /// <summary>
      ///   Formatted description of the NPC's key in-game relationships:
      ///   liege, friends, enemies, family. Built from live game state and
      ///   refreshed on every session launch — not a stable identity field.
      ///   Null until the first session launch after profile creation.
      /// </summary>
      public string? Relationships { get; set; }

      /// <summary>
      ///   This NPC's OWN personal opinion of the player — independent of the
      ///   clan's collective standing. Per-NPC, persisted, moved only through the
      ///   gated relation action. Clamped to [-100, 100]. Negative = hostile.
      ///   May diverge from <see cref="ClanRelationWithPlayer" /> (e.g. a secret
      ///   fondness despite the clan's enmity).
      /// </summary>
      public int ReputationWithPlayer { get; set; }

      /// <summary>
      ///   Optional romantic profile. Null if the consumer disabled romantic
      ///   features at profile creation, or if this NPC was created before
      ///   the feature existed. Persisted across sessions once created.
      /// </summary>
      public RomanticProfile? Romantic { get; set; }

      private readonly List<PendingLetter> _sentLetters = new();

      /// <summary>
      ///   Letters this NPC has sent (or is about to send) to the player. Holds every
      ///   letter in all states — in transit, delivered, and replied — so the full
      ///   correspondence history is available for prompt injection and anti-spam checks.
      ///   Persisted across sessions via the store's JSON serializer.
      /// </summary>
      public List<PendingLetter> SentLetters
      {
         get => _sentLetters;
         init => _sentLetters = value ?? new List<PendingLetter>();
      }

      private readonly List<ScheduledLetter> _scheduledLetters = new();

      /// <summary>
      ///   Courier audit 2.5: letters this NPC has COMMITTED to send later but has not yet written (currently
      ///   only the child-support demand, due some days after a birth). The intention is persisted here, and the
      ///   content is generated at the scheduled day with bounded retry, so an LLM outage at the moment of the
      ///   birth no longer loses the pension (and its quest) forever, and the old "stamp SentOnDay in the future"
      ///   hack is gone. Empty on saves made before this existed (graceful, additive).
      /// </summary>
      public List<ScheduledLetter> ScheduledLetters
      {
         get => _scheduledLetters;
         init => _scheduledLetters = value ?? new List<ScheduledLetter>();
      }

      private readonly List<TroopLoan> _troopLoans = new();

      /// <summary>
      ///   Soldiers this NPC has lent the player for a task, and which return when that task ends (see
      ///   <see cref="TroopLoan" />). Additive and save-safe: an older save loads this empty, which reads
      ///   correctly as "this lord has nothing out on loan" rather than needing a migration.
      /// </summary>
      public List<TroopLoan> TroopLoans
      {
         get => _troopLoans;
         init => _troopLoans = value ?? new List<TroopLoan>();
      }

      /// <summary>
      ///   Name of this NPC's current spouse, or null if the NPC is single or widowed.
      ///   Refreshed from live game state on every session launch so it stays accurate
      ///   as Bannerlord events (death, remarriage) alter marital status.
      ///   Drives the intimacy-consent rules injected by <see cref="PromptBuilder" />.
      /// </summary>
      public string? SpouseName { get; set; }

      /// <summary>Stance axis — intimidation by the player (0..+100); fades over time.</summary>
      public int StanceFear { get; set; }

      /// <summary>
      ///   Campaign-time hour of the last accepted spoken stance-round, so spoken influence on the
      ///   stance axes can be rate-limited. Null = none yet. Game-agnostic: the SDK only stores it.
      /// </summary>
      public double? StanceLastWordHour { get; set; }

      /// <summary>
      ///   Game-day this NPC last acted on their stance toward the player unbidden (a hostile or favourable
      ///   letter), so "stance with teeth" rate-limits how often one lord reaches out. 0 = never. Game-agnostic.
      /// </summary>
      public int StanceLastConsequenceDay { get; set; }

      /// <summary>
      ///   Game-day this NPC armed an attempt on the player's life (a town ambush), or 0 when none is laid.
      ///   Set when the stance hardens to murderous intent; cleared once the ambush springs or is called off.
      ///   Game-agnostic: the SDK only stores it.
      /// </summary>
      public int StanceAmbushArmedDay { get; set; }

      /// <summary>Stance axis — esteem for the player's worth and prowess (−100..+100).</summary>
      public int StanceRespect { get; set; }

      /// <summary>
      ///   Stance axis — the NPC's faith in the player's word and deals (−100..+100). Part of the
      ///   multi-axis posture toward the player; Affection is mirrored from
      ///   <see cref="ReputationWithPlayer" />. Moved weakly by words and strongly by proven deeds
      ///   (the consumer's StanceGate). Defaults to 0 (neutral) on profiles from older saves.
      /// </summary>
      public int StanceTrust { get; set; }

      /// <summary>
      ///   Short personality archetype derived from the NPC's traits.
      ///   E.g., "Pitiless", "Decent And Kind", "The Charming Manipulator".
      /// </summary>
      public string? Trait { get; set; }

      /// <summary>
      ///   Applies memory, reputation, and event (if any) from a parsed response
      ///   in a single call. The current game day is required to timestamp events.
      /// </summary>
      /// <remarks>
      ///   Obsolete: this overlapped with <see cref="ProfileMutator.Apply" /> (which also
      ///   clamps reputation, guards duplicate FirstMeeting events, and advances the romantic
      ///   arc) via a second, divergent path. Delegates to it now so both paths behave
      ///   identically; still persists the memory digest, which <see cref="ProfileMutator" />
      ///   deliberately does not (the memory section is descriptive-only there).
      /// </remarks>
      [Obsolete("Use ProfileMutator.Apply(profile, response, gameDay) directly instead.")]
      public void ApplyConversationResult(ParsedResponse response, int gameDay)
      {
         if (response.Memory != null) ApplyMemoryUpdate(response.Memory);
         ProfileMutator.Apply(this, response, gameDay);
      }

      // ── Tell Don't Ask ────────────────────────────────────────────────────

      public void ApplyMemoryUpdate(ConversationMemory memory)
      {
         string entry = $"[{memory.Topic}] sentiment:{memory.Sentiment}" +
                        (memory.Decision != null
                           ? $" decision:{memory.Decision}"
                           : string.Empty);
         MemoryDigest = string.IsNullOrEmpty(MemoryDigest)
            ? entry
            : MemoryDigest + "\n" + entry;
      }

      /// <summary>Stamps a parsed event with the current game day and records it.</summary>
      [Obsolete("Use ProfileMutator.ApplyNotableEvent(profile, type, summary, gameDay) instead " +
                "(adds the duplicate-FirstMeeting guard).")]
      public void ApplyNotableEvent(ParsedEventData data, int gameDay)
         => ProfileMutator.ApplyNotableEvent(this, data.Type, data.Summary, gameDay);

      [Obsolete("Use ProfileMutator.ApplyReputationDelta(profile, delta) instead (adds the [-100, 100] clamp).")]
      public void ApplyReputationDelta(ReputationDelta delta)
      {
         if (!delta.ClanDelta.HasValue) return;
         ProfileMutator.ApplyReputationDelta(this, delta.ClanDelta.Value);
      }
   }
}
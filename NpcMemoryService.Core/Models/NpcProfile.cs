// Code written by Gabriel Mailhot, 26/05/2026.

#region

using System;
using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   An NPC's identity and accumulated memory of the player.
    ///   Persisted per save game via INpcMemoryStore.
    /// </summary>
    public sealed class NpcProfile
    {
        /// <summary>
        ///   Background narrative context produced when older events are compressed away.
        ///   Preserves the gist of dropped events as a short prose paragraph.
        /// </summary>
        public string? BackgroundContext { get; set; }

        public required string Clan { get; init; }

        /// <summary>
        ///   Significant past events with natural-language summaries.
        ///   This is the primary long-term memory surfaced to the LLM.
        /// </summary>
        public List<NotableEvent> Events { get; init; } = new List<NotableEvent>();

        public required string Faction { get; init; }
        public required string Id { get; init; }

        /// <summary>
        ///   Compact per-conversation summaries. Useful for diagnostics.
        ///   Not currently injected into the prompt — see <see cref="Events" />.
        ///   Null means the NPC has never met the player.
        /// </summary>
        public string? MemoryDigest { get; set; }

        public required string Name { get; init; }
        public string? Personality { get; set; }

        /// <summary>
        ///   This NPC's OWN personal opinion of the player — independent of the
        ///   clan's collective standing. Per-NPC, persisted, moved only through the
        ///   gated relation action. Clamped to [-100, 100]. Negative = hostile.
        ///   May diverge from <see cref="ClanRelationWithPlayer"/> (e.g. a secret
        ///   fondness despite the clan's enmity).
        /// </summary>
        public int ReputationWithPlayer { get; set; }

        /// <summary>
        ///   The clan's collective standing with the player, mirrored from the host
        ///   game's clan-level relation just before each prompt build. Null when the
        ///   consumer does not supply it (e.g. the console runner) — in that case the
        ///   prompt falls back to showing only the personal opinion.
        ///   Transient: not a durable part of this NPC's identity.
        /// </summary>
        public int? ClanRelationWithPlayer { get; set; }

        /// <summary>
        ///   Campaign-time hour (<c>CampaignTime.Now.ToHours</c>) of the last
        ///   POSITIVE relation gain granted through a relation-changing action.
        ///   Used by the consumer to throttle relationship growth (at most one
        ///   routine gain per cooldown window). Null = no gain recorded yet.
        ///   Game-agnostic: the SDK only stores it; the consumer defines the policy.
        /// </summary>
        public double? LastRelationGainHour { get; set; }

        /// <summary>
        ///   Formatted description of the NPC's key in-game relationships:
        ///   liege, friends, enemies, family. Built from live game state and
        ///   refreshed on every session launch — not a stable identity field.
        ///   Null until the first session launch after profile creation.
        /// </summary>
        public string? Relationships { get; set; }

        /// <summary>
        ///   Name of this NPC's current spouse, or null if the NPC is single or widowed.
        ///   Refreshed from live game state on every session launch so it stays accurate
        ///   as Bannerlord events (death, remarriage) alter marital status.
        ///   Drives the intimacy-consent rules injected by <see cref="PromptBuilder" />.
        /// </summary>
        public string? SpouseName { get; set; }

        /// <summary>
        ///   Personal traits and preferences this NPC has revealed to the player
        ///   through conversation. Empty until the player has had meaningful exchanges.
        ///   Grows over time as the NPC opens up; each entry is deduplicated by
        ///   <see cref="DiscoveredTrait.Key" /> so the same fact is never recorded twice.
        ///   Displayed in the encyclopedia discovery section.
        /// </summary>
        public List<DiscoveredTrait> DiscoveredTraits { get; init; } = new List<DiscoveredTrait>();

        /// <summary>
        ///   Optional romantic profile. Null if the consumer disabled romantic
        ///   features at profile creation, or if this NPC was created before
        ///   the feature existed. Persisted across sessions once created.
        /// </summary>
        public RomanticProfile? Romantic { get; set; }

        /// <summary>
        ///   Tasks this NPC has asked the player to accomplish. Holds quests in every
        ///   lifecycle state — outstanding, satisfied-awaiting-reward, and recently
        ///   terminated — so the giver can reference both pending work and how the
        ///   player has discharged past obligations. Each quest carries its own evidence,
        ///   so nothing accumulates into a shared log. Persisted across sessions.
        /// </summary>
        public List<InformalQuest> ActiveQuests { get; init; } = new List<InformalQuest>();

        /// <summary>
        ///   Short personality archetype derived from the NPC's traits.
        ///   E.g., "Pitiless", "Decent And Kind", "The Charming Manipulator".
        /// </summary>
        public string? Trait { get; set; }

        /// <summary>
        ///   Applies memory, reputation, and event (if any) from a parsed response
        ///   in a single call. The current game day is required to timestamp events.
        /// </summary>
        public void ApplyConversationResult(ParsedResponse response, int gameDay)
        {
            if (response.Memory != null) ApplyMemoryUpdate(response.Memory);
            if (response.Reputation != null) ApplyReputationDelta(response.Reputation);
            if (response.NewEventData != null) ApplyNotableEvent(response.NewEventData, gameDay);
        }

        // ── Tell Don't Ask ────────────────────────────────────────────────────

        public void ApplyMemoryUpdate(ConversationMemory memory)
        {
            var entry = $"[{memory.Topic}] sentiment:{memory.Sentiment}" +
                        (memory.Decision != null
                            ? $" decision:{memory.Decision}"
                            : string.Empty);
            MemoryDigest = string.IsNullOrEmpty(MemoryDigest)
                ? entry
                : MemoryDigest + "\n" + entry;
        }

        /// <summary>Stamps a parsed event with the current game day and records it.</summary>
        public void ApplyNotableEvent(ParsedEventData data, int gameDay)
        {
            Events.Add(new NotableEvent(gameDay, data.Type, data.Summary));
        }

        public void ApplyReputationDelta(ReputationDelta delta)
        {
            if (!delta.ClanDelta.HasValue) return;
            var updated = ReputationWithPlayer + delta.ClanDelta.Value;
            ReputationWithPlayer = Math.Max(-100, Math.Min(100, updated));
        }
    }
}
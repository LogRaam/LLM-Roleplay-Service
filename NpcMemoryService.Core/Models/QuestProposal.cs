// Code written by Gabriel Mailhot, 01/06/2026.
// Sprint 11: informal quests — parsed [QUEST] block.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   A quest the LLM proposed in a <c>[QUEST]</c> block, as parsed from the raw
    ///   response. The fields are still in the LLM's own words: target names are plain
    ///   strings, not resolved game ids. The consumer turns this into a persisted
    ///   <see cref="InformalQuest" /> by resolving the named target to a real settlement,
    ///   hero, or faction, and computing the deadline from <see cref="DeadlineDays" />.
    ///   A proposal that fails to resolve is dropped, never persisted half-formed.
    /// </summary>
    public sealed class QuestProposal
    {
        /// <summary>The deed type the LLM named (e.g. "bandit_clear"), mapped to <see cref="QuestType" /> by the consumer/parser.</summary>
        public QuestType Type { get; init; }

        /// <summary>Player-facing description in the giver's voice.</summary>
        public string Description { get; init; } = "";

        /// <summary>Named settlement target, or null. Resolved to a real settlement by the consumer.</summary>
        public string? TargetSettlement { get; init; }

        /// <summary>Named hero target (lord, prisoner, letter recipient), or null.</summary>
        public string? TargetHero { get; init; }

        /// <summary>Named faction target, or null.</summary>
        public string? TargetFaction { get; init; }

        /// <summary>Number of days the player has, or null for an open-ended quest.</summary>
        public int? DeadlineDays { get; init; }

        /// <summary>Denars the giver promises on completion. Zero when none.</summary>
        public int RewardGold { get; init; }

        /// <summary>Personal-relation gain the giver promises on completion. Zero when none.</summary>
        public int RewardRelation { get; init; }

        /// <summary>
        ///   The non-monetary favor the giver offers on completion (the negotiation
        ///   framework's reward side), or <see cref="RewardGrant.None" /> for an ordinary
        ///   quest. The consumer validates it can be honored before persisting.
        /// </summary>
        public RewardGrant Reward { get; init; } = RewardGrant.None;

        /// <summary>
        ///   For a <see cref="QuestType.DeliverItems" /> deed whose reward is NOT a
        ///   computed-value grant: the denar value of goods the giver asks for, in the
        ///   LLM's words. Ignored (and recomputed game-side) when the reward's worth sets
        ///   the floor, e.g. paying for a companion's service in kind.
        /// </summary>
        public int RequiredValue { get; init; }

        /// <summary>
        ///   For a <see cref="RewardGrant.MarriageConsent" /> bargain: the name of the intended
        ///   spouse the blessing covers (the candidate the player asked after), as the LLM wrote
        ///   it. Resolved and validated game-side. Null for non-marriage bargains.
        /// </summary>
        public string? MarriageSpouse { get; init; }
    }
}

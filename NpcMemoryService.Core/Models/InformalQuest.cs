// Code written by Gabriel Mailhot, 01/06/2026.
// Sprint 11: informal quests — persisted quest record.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   A task an NPC has asked the player to accomplish, persisted on the giver's
    ///   <see cref="NpcProfile.ActiveQuests" /> until it terminates. The reward is
    ///   fixed when the quest is issued (not renegotiated at completion) so the player
    ///   cannot talk their way into a bigger payout after the fact.
    ///
    ///   Completion is event-driven, not roleplay-driven: the consumer's game hooks
    ///   stamp <see cref="SatisfiedOnDay" /> and <see cref="Evidence" /> only when the
    ///   matching real deed fires with the player involved. The prompt then surfaces
    ///   that evidence to the giver, who acknowledges it and triggers the reward.
    /// </summary>
    public sealed class InformalQuest
    {
        /// <summary>The kind of deed requested. Drives how the consumer matches events.</summary>
        public QuestType Type { get; set; }

        /// <summary>Player-facing description of the task, in the giver's voice.</summary>
        public string Description { get; set; } = "";

        /// <summary>
        ///   Name of the settlement the deed is anchored to (village to clear around,
        ///   town to besiege, etc.), or null when the quest type is not place-bound.
        /// </summary>
        public string? TargetSettlement { get; set; }

        /// <summary>
        ///   Stable id of the target hero — the enemy lord to defeat, the prisoner to
        ///   capture or free, the recipient of a letter — or null when not hero-bound.
        /// </summary>
        public string? TargetHeroId { get; set; }

        /// <summary>Display name of <see cref="TargetHeroId" />, kept for prompt and UI text.</summary>
        public string? TargetHeroName { get; set; }

        /// <summary>
        ///   Stable id of the target faction (the enemy whose parties, villages, or
        ///   caravans the player is asked to strike), or null when not faction-bound.
        ///   Also drives the validity check: if the giver's faction makes peace with
        ///   this one, the quest is cancelled.
        /// </summary>
        public string? TargetFactionId { get; set; }

        /// <summary>Display name of <see cref="TargetFactionId" />.</summary>
        public string? TargetFactionName { get; set; }

        /// <summary>Game day the quest was issued. Used as the floor for valid deeds.</summary>
        public int IssuedOnDay { get; set; }

        /// <summary>
        ///   Game day by which the deed must be done, or null when the giver set no
        ///   deadline. Missing the deadline expires the quest with a consequence.
        /// </summary>
        public int? DeadlineDay { get; set; }

        /// <summary>Denars paid to the player on completion. Zero when none promised.</summary>
        public int RewardGold { get; set; }

        /// <summary>Personal-relation gain granted on completion. Zero when none promised.</summary>
        public int RewardRelation { get; set; }

        /// <summary>Lifecycle state. See <see cref="QuestStatus" />.</summary>
        public QuestStatus Status { get; set; } = QuestStatus.Active;

        /// <summary>
        ///   Game day the deed was verified by a real game event, or null while the
        ///   task is still outstanding. A non-null value on an <see cref="QuestStatus.Active" />
        ///   quest means "done, awaiting the player's report to the giver".
        /// </summary>
        public int? SatisfiedOnDay { get; set; }

        /// <summary>
        ///   Short, factual proof of the deed, written by the consumer when the quest
        ///   is satisfied (e.g. "Defeated the bandits near Aldrok on day 142"). Injected
        ///   into the prompt so the giver can acknowledge a deed they could not witness.
        ///   Null until the quest is satisfied.
        /// </summary>
        public string? Evidence { get; set; }

        /// <summary>True while the quest is live and its deed not yet verified.</summary>
        public bool IsOutstanding => Status == QuestStatus.Active && SatisfiedOnDay == null;

        /// <summary>True when the deed is verified and the quest is awaiting its reward.</summary>
        public bool IsAwaitingReward => Status == QuestStatus.Active && SatisfiedOnDay != null;
    }
}

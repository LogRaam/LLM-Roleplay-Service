using System.Collections.Generic;

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// The LLM's response decomposed into its structured sections.
    /// Only <see cref="Dialogue"/> is guaranteed; other sections are
    /// emitted by the LLM only when meaningful.
    /// </summary>
    public sealed class ParsedResponse
    {
        public required string Dialogue { get; init; }

        /// <summary>
        ///   Free-form scene narration (a <c>[NARRATION]</c> block), in the second person,
        ///   describing physical actions and events directed at the player — including the
        ///   actions of witnesses/soldiers acting on the main speaker's orders. Distinct
        ///   from <see cref="Dialogue" /> (the speaker's own first-person voice). Used
        ///   chiefly in captive/CNC scenes. Null when no narration was emitted.
        /// </summary>
        public string? Narration { get; init; }

        public ConversationMemory? Memory { get; init; }
        public ParsedEventData? NewEventData { get; init; }
        public ReputationDelta? Reputation { get; init; }

        /// <summary>
        /// Game-world actions requested by the LLM (zero or more).
        /// Consumers (game mods) interpret and execute these.
        /// </summary>
        public IReadOnlyList<GameAction> Actions { get; init; } = new List<GameAction>();

        /// <summary>
        ///   A personal trait the NPC naturally revealed during this exchange.
        ///   Null when no revelation occurred. The <see cref="DiscoveredTrait.GameDay" />
        ///   is 0 here — the consumer stamps it with the real game day when persisting.
        /// </summary>
        public DiscoveredTrait? Discovery { get; init; }

        /// <summary>
        ///   A quest the NPC offered the player in this exchange (a <c>[QUEST]</c> block).
        ///   Null when none was offered. The consumer resolves and persists it.
        /// </summary>
        public QuestProposal? QuestGiven { get; init; }

        /// <summary>
        ///   The NPC's acknowledgement that one of their active quests is done
        ///   (a <c>[QUEST_COMPLETE]</c> block). Null when none was claimed. The consumer
        ///   pays the reward only if a matching, already-satisfied quest exists.
        /// </summary>
        public QuestCompletionClaim? QuestCompleted { get; init; }

        /// <summary>
        ///   The NPC's recognition that the player has given up an active quest
        ///   (a <c>[QUEST_ABANDON]</c> block). Null when none was abandoned. The consumer
        ///   marks the quest abandoned and applies the broken-word consequence.
        /// </summary>
        public QuestAbandonClaim? QuestAbandoned { get; init; }

        /// <summary>
        ///   Visible reactions from witnesses present during this exchange
        ///   (<c>[WITNESS_REACTION]</c> blocks). Empty list when no witnesses reacted.
        ///   Each reaction is displayed as a separate chat message with the witness's portrait.
        /// </summary>
        public IReadOnlyList<WitnessReaction> WitnessReactions { get; init; }
            = new List<WitnessReaction>();
    }
}

// Code written by Gabriel Mailhot, 02/06/2026.
// Sprint 11: informal quests — parsed [QUEST_ABANDON] block.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   The giver's recognition, in a <c>[QUEST_ABANDON]</c> block, that the player has
    ///   told them they will not finish an active quest. The consumer marks the matching
    ///   quest <see cref="QuestStatus.Abandoned" /> and applies the broken-word consequence.
    ///   This is an honest withdrawal initiated by the player in dialogue — distinct from a
    ///   quest the world quietly cancelled. The LLM emits it only when the player has clearly
    ///   said they are giving up the task, never on its own initiative.
    /// </summary>
    public sealed class QuestAbandonClaim
    {
        /// <summary>
        ///   The deed type being abandoned, or null when the giver did not specify
        ///   (the consumer then abandons the single outstanding quest, if exactly one).
        /// </summary>
        public QuestType? Type { get; init; }
    }
}

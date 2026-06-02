// Code written by Gabriel Mailhot, 01/06/2026.
// Sprint 11: informal quests — parsed [QUEST_COMPLETE] block.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   The giver's acknowledgement, in a <c>[QUEST_COMPLETE]</c> block, that one of
    ///   their active quests is done and its reward should be paid. The LLM is instructed
    ///   to emit this ONLY when the prompt shows verified evidence for that quest — so the
    ///   block is a narrative trigger, never the proof itself. The consumer matches it to
    ///   a satisfied active quest (by <see cref="Type" /> when several are open) and pays
    ///   the reward fixed at issue time; an unmatched or unsatisfied claim is ignored.
    /// </summary>
    public sealed class QuestCompletionClaim
    {
        /// <summary>
        ///   The deed type the giver is acknowledging, or null when they did not specify
        ///   (the consumer then completes the single satisfied quest, if exactly one).
        /// </summary>
        public QuestType? Type { get; init; }
    }
}

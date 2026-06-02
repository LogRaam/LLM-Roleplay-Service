// Code written by Gabriel Mailhot, 01/06/2026.
// Sprint 11: informal quests — quest lifecycle.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Lifecycle state of an <see cref="InformalQuest" />. A quest stays
    ///   <see cref="Active" /> until it terminates one of three ways:
    ///   <see cref="Completed" /> (deed verified and reward paid),
    ///   <see cref="Expired" /> (deadline passed without the deed being done — carries
    ///   a consequence), <see cref="Cancelled" /> (its preconditions vanished, e.g. the
    ///   target faction made peace — no fault of the player, no penalty), or
    ///   <see cref="Abandoned" /> (the player told the giver they will not do it — an
    ///   honest withdrawal that still costs some standing for the broken word).
    ///   Whether the deed itself is done is tracked separately by
    ///   <see cref="InformalQuest.SatisfiedOnDay" />, so an Active quest can already be
    ///   satisfied and awaiting the player's report back to the giver.
    /// </summary>
    public enum QuestStatus
    {
        Active,
        Completed,
        Expired,
        Cancelled,
        Abandoned
    }
}

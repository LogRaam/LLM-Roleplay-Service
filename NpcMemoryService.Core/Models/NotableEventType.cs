namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// Categorizes a notable event between the player and an NPC.
    /// <c>Other</c> is the fallback when the LLM emits an unrecognized type.
    /// </summary>
    public enum NotableEventType
    {
        FirstMeeting,
        Farewell,
        Conflict,
        Collaboration,
        Agreement,
        Flirt,
        Intimacy,
        Betrayal,
        Confrontation,
        Other,

        /// <summary>
        ///   A captive encounter in which this NPC held the player prisoner and used,
        ///   dominated, or abused them (Sprint 17). Recorded by the mod itself at the end
        ///   of a captive scene — not emitted by the LLM in an [EVENT] block. Added last to
        ///   preserve the serialized ordinal values of existing saves.
        /// </summary>
        Captivity
    }
}

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
        Captivity,

        /// <summary>
        ///   A grievance this NPC holds because of a romantic act by the player involving
        ///   someone they had a stake in (a spouse wronged, a rival suitor who lost the one
        ///   they wanted, a co-partner outranked). Recorded by the mod's jealousy system —
        ///   not emitted by the LLM — so the NPC stays cold and can reference it later.
        ///   Added last to preserve the serialized ordinal values of existing saves.
        /// </summary>
        Jealousy
    }
}

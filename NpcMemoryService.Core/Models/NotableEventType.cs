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
        Jealousy,

        /// <summary>
        ///   Word that reached this NPC, not something they lived with the player (tashmetu, 06/10/2026: a mod
        ///   writing a memory for each piece of news a noble hears). Shown apart from their history, never counted
        ///   against the lived memories a short prompt keeps, and never shielded from compression by being recent.
        ///   Added last to preserve the serialized ordinal values of existing saves.
        /// </summary>
        Hearsay,

        /// <summary>
        ///   The NPC's own life, apart from the player: their attachments, grudges, feuds and affairs with others
        ///   (tashmetu, 08/10/2026: Regard and Standing writes them). Shown apart from their history with the player,
        ///   never counted against its lived memories, never framed as a predecessor's history on a succession.
        ///   Added last to preserve the serialized ordinal values of existing saves.
        /// </summary>
        OwnLife
    }
}

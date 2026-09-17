// Code written by Gabriel Mailhot, 10/05/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   A notable event in the player–NPC interaction history.
    ///   Emitted by the LLM via the [EVENEMENT] section.
    /// </summary>
    public sealed record NotableEvent(
        int gameDay,
        NotableEventType type,
        string summary
    )
    {
        /// <summary>
        ///   Short player-facing label of the MECHANICAL deed the bridge actually executed alongside this
        ///   memory ("Gave gold", "Joined your party", "Regard rose"...), or null when the turn recorded only
        ///   a memory with no emitted action. The Memories panel highlights this in blue, so the tag marks a
        ///   real, verifiable turning point rather than the LLM's narrative classification. Additive and
        ///   optional: older saves deserialize it as null (no migration needed).
        /// </summary>
        public string? ActionTag { get; set; }

        /// <summary>
        ///   A memory this character keeps to THEMSELVES: it reaches their own prompt like any other, and is
        ///   never rendered to anybody else in the room.
        ///   <para>
        ///     Built for the companion briefing (2026-09-13). A companion's memories travel into the prompt of
        ///     whoever is speaking, as their witness recall - which is exactly right for "he was there when you
        ///     took the town" and catastrophic for "Arwa asked me to charm Ira", read out in Ira's own prompt
        ///     while she stands there. Until now the briefing dealt with that by DROPPING its memories
        ///     entirely, so nothing of the private word survived the conversation it was given in.
        ///   </para>
        ///   <para>
        ///     The rule is about the AUDIENCE, never about the content: it says who may read this, not that it
        ///     is shameful. Additive and optional, so older saves deserialize it as false - a memory is public
        ///     unless somebody decided otherwise, which is the safe default for a leak guard to have backwards.
        ///   </para>
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        ///   A stable identity for this memory, set when something intends to come BACK to it later — an
        ///   anchor a summarizer will enrich once the model has read the transcript. Null everywhere else,
        ///   and null on every event written before 16/09/2026.
        ///
        ///   <para>
        ///     Two defects made it necessary, and both come from the same cause: a memory was identified by
        ///     its VALUE, which is neither unique nor stable (audit, 15/09/2026).
        ///   </para>
        ///   <para>
        ///     TWO CONVERSATIONS ON ONE DAY. The anchor written at the close of a chat is always
        ///     "Spoke with {player}." on today's date, so a second conversation with the same person on the
        ///     same day produced a byte-identical event. The summary queue deduplicated the second job away,
        ///     and the first conversation's summary was then written onto the SECOND conversation's event by
        ///     a FindLastIndex. One memory misattributed, one conversation never enriched.
        ///   </para>
        ///   <para>
        ///     ENRICHMENT VERSUS COMPRESSION. A summarizer replaces an anchor IN PLACE while the compressor
        ///     is working from a snapshot taken before that; the compressor then writes its snapshot back and
        ///     the enrichment is gone, with the job already marked done and never retried. An identity lets
        ///     the write-back carry forward whatever the anchor has become.
        ///   </para>
        ///   <para>
        ///     Additive and optional, the same contract as ActionTag and IsPrivate above: older saves
        ///     deserialize it as null and every path falls back to matching by value, which is exactly the
        ///     behaviour they have today.
        ///   </para>
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        ///   A copy of this memory carrying a fresh identity. Used where something is written now and meant
        ///   to be found again later; everything else may stay anonymous.
        /// </summary>
        public NotableEvent Identified()
            => this with {Id = System.Guid.NewGuid().ToString("N")};
    }
}
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
    }
}
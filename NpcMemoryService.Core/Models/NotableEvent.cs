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
    }
}
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
    );
}
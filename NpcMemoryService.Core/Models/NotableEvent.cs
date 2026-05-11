namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// A notable event in the player–NPC interaction history.
    /// Emitted by the LLM via the [EVENEMENT] section.
    /// </summary>
    public sealed record NotableEvent(
        int GameDay,
        NotableEventType Type,
        string Summary);
}
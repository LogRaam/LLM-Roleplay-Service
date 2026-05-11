namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// Event data as extracted from the LLM's [EVENEMENT] section.
    /// The service composes this with the current game day to produce
    /// a fully-formed <see cref="NotableEvent"/> for storage.
    /// </summary>
    public sealed record ParsedEventData(
        NotableEventType Type,
        string Summary);
}

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// Ambient world context injected into every NPC system prompt.
    /// Kept intentionally short to preserve context window budget.
    /// </summary>
    public sealed class WorldState
    {
        public int     CurrentDay       { get; init; }
        public string? ActiveConflicts  { get; init; }
        public string? Rumors           { get; init; }
    }
}

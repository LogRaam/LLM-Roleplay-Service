namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// Ambient world context injected into every NPC system prompt.
    /// Kept intentionally short to preserve context window budget.
    /// </summary>
    public sealed class WorldState
    {
        public int     CurrentDay       { get; init; }
        public string? Season           { get; init; }
        public string? ActiveConflicts  { get; init; }
        public string? Rumors           { get; init; }

        /// <summary>
        ///   The current part of the day (e.g. "morning", "afternoon", "evening", "night"),
        ///   so a scene's lighting and ambiance match vanilla time. Null when unknown.
        /// </summary>
        public string? TimeOfDay        { get; init; }
    }
}

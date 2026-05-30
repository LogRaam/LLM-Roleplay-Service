using System.Collections.Generic;

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>
    /// One action that the LLM may emit in an [ACTION] section.
    /// Each consumer (e.g., the Bannerlord mod) defines its own vocabulary.
    /// </summary>
    public sealed class GameActionDefinition
    {
        public required string Type        { get; init; }
        public required string Description { get; init; }

        /// <summary>
        /// Optional list of parameter names this action accepts.
        /// Communicated to the LLM so it can fill them appropriately.
        /// </summary>
        public IReadOnlyList<string> Parameters { get; init; } = new List<string>();
    }
}

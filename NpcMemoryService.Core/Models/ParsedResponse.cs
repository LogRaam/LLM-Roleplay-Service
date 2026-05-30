using System.Collections.Generic;

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// The LLM's response decomposed into its structured sections.
    /// Only <see cref="Dialogue"/> is guaranteed; other sections are
    /// emitted by the LLM only when meaningful.
    /// </summary>
    public sealed class ParsedResponse
    {
        public required string Dialogue { get; init; }
        public ConversationMemory? Memory { get; init; }
        public ParsedEventData? NewEventData { get; init; }
        public ReputationDelta? Reputation { get; init; }

        /// <summary>
        /// Game-world actions requested by the LLM (zero or more).
        /// Consumers (game mods) interpret and execute these.
        /// </summary>
        public IReadOnlyList<GameAction> Actions { get; init; } = new List<GameAction>();
    }
}

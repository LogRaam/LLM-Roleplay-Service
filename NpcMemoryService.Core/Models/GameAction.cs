using System.Collections.Generic;

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// A directive emitted by the LLM via the [ACTION] section, requesting
    /// the consumer (game mod, etc.) to effect a change in the game world.
    /// The SDK is action-vocabulary agnostic: <see cref="Type"/> is a free
    /// string and consumers define what they support.
    /// </summary>
    public sealed class GameAction
    {
        /// <summary>
        /// Action verb (e.g., "imprison", "give_money", "recruit"). Case-insensitive.
        /// The consumer interprets this and either executes or ignores it.
        /// </summary>
        public required string Type { get; init; }

        /// <summary>
        /// Optional natural-language context explaining the intent. Useful for
        /// logging, debugging, and as a fallback if the action requires nuanced
        /// interpretation.
        /// </summary>
        public string? Context { get; init; }

        /// <summary>
        /// Free-form parameters specific to the action type. Examples:
        /// for "give_money": { "amount": "500" }
        /// for "give_item":  { "item": "longsword", "quantity": "1" }
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters { get; init; }
            = new Dictionary<string, string>();
    }
}

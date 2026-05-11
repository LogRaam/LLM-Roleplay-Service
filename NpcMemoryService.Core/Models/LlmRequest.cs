using System.Collections.Generic;

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// A completion request in our internal protocol.
    /// Providers map this to their own wire format.
    /// </summary>
    public sealed class LlmRequest
    {
        /// <summary>System instructions: NPC profile, memory, world state, format rules.</summary>
        public required string SystemPrompt { get; init; }

        /// <summary>Conversation history, alternating User / Assistant turns.</summary>
        public required IReadOnlyList<LlmMessage> Messages { get; init; }

        public LlmParameters Parameters { get; init; } = new LlmParameters();
    }
}

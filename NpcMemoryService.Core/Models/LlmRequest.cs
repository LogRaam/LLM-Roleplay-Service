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

        /// <summary>
        ///   The STABLE prefix of <see cref="SystemPrompt"/> (must be a literal prefix) — the part that does
        ///   not change turn-to-turn within a conversation (identity, persona, instructions). When set and
        ///   caching is on, the cache breakpoint goes here, so only this prefix is cached and the dynamic
        ///   per-turn tail (current encounter, rumours, names) is sent fresh. Null = cache the whole prompt.
        /// </summary>
        public string? StableSystemPrompt { get; init; }

        /// <summary>Conversation history, alternating User / Assistant turns.</summary>
        public required IReadOnlyList<LlmMessage> Messages { get; init; }

        public LlmParameters Parameters { get; init; } = new LlmParameters();
    }
}

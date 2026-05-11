namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// Provider-agnostic generation settings.
    /// Each provider maps these to its own parameter names.
    /// </summary>
    public sealed class LlmParameters
    {
        public int   MaxTokens  { get; init; } = 1000;

        /// <summary>
        /// Creativity level from 0.0 (deterministic) to 1.0 (very creative).
        /// Maps to <c>temperature</c> or equivalent on each provider.
        /// </summary>
        public float Creativity { get; init; } = 0.7f;
    }
}

namespace NpcMemoryService.Core.Models
{
    /// <summary>Token consumption reported by the provider. Useful for diagnostics.</summary>
    public sealed class LlmUsage
    {
        public int PromptTokens          { get; init; }
        public int CompletionTokens      { get; init; }

        /// <summary>
        /// Of <see cref="PromptTokens"/>, how many were served from the provider's cache.
        /// Null when the provider does not report cache statistics.
        /// </summary>
        public int? CachedPromptTokens   { get; init; }

        public LlmUsage(int promptTokens, int completionTokens)
        {
            PromptTokens     = promptTokens;
            CompletionTokens = completionTokens;
        }
    }

    /// <summary>
    /// A completion response in our internal protocol.
    /// On failure, <see cref="Content"/> is empty and <see cref="ErrorMessage"/> is set.
    /// </summary>
    public sealed class LlmResponse
    {
        public required string  Content      { get; init; }
        public required bool    IsSuccess    { get; init; }
        public string?          ErrorMessage { get; init; }
        public LlmUsage?        Usage        { get; init; }

        /// <summary>
        ///   The provider's reason for ending generation ("stop", "length", "content_filter", …),
        ///   or null when not reported. "length" means the reply was cut off by the token limit —
        ///   some models (e.g. certain DeepSeek deployments) truncate mid-sentence this way.
        /// </summary>
        public string?          FinishReason { get; init; }
    }
}

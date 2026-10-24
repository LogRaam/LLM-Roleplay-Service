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

        /// <summary>
        /// What the provider billed for this call, in US dollars, when it says so (OpenRouter's usage accounting).
        /// Null when the provider does not report it: a local server, NanoGPT, an older response.
        /// </summary>
        public double? Cost              { get; init; }

        /// <summary>
        /// Of <see cref="CompletionTokens"/>, how many the model spent thinking: billed, never shown to the player.
        /// Null when the provider does not report it.
        /// </summary>
        public int? ReasoningTokens      { get; init; }

        /// <summary>Of <see cref="PromptTokens"/>, how many were written to the provider's cache on this call. Null when not reported.</summary>
        public int? CacheWriteTokens     { get; init; }

        /// <summary>
        /// Two attempts as one bill (a retry is paid for as well as the attempt it replaced). A figure either side
        /// does not report stays unknown only when neither reports it.
        /// </summary>
        public static LlmUsage? Sum(LlmUsage? a, LlmUsage? b)
        {
            if (a == null) return b;
            if (b == null) return a;

            return new LlmUsage(a.PromptTokens + b.PromptTokens, a.CompletionTokens + b.CompletionTokens)
            {
                CachedPromptTokens = Add(a.CachedPromptTokens, b.CachedPromptTokens),
                Cost = a.Cost == null && b.Cost == null ? null : (a.Cost ?? 0) + (b.Cost ?? 0),
                ReasoningTokens = Add(a.ReasoningTokens, b.ReasoningTokens),
                CacheWriteTokens = Add(a.CacheWriteTokens, b.CacheWriteTokens)
            };
        }

        private static int? Add(int? a, int? b) => a == null && b == null ? null : (a ?? 0) + (b ?? 0);

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

        /// <summary>
        ///   True when this reply came from the bounded second attempt (the first one was empty or
        ///   length-truncated). Surfaced so the host can log real retry frequency: usage token counts
        ///   alone cannot distinguish a retry from a single reply whose completion count includes
        ///   reasoning tokens.
        /// </summary>
        public bool             WasRetried   { get; init; }
    }
}

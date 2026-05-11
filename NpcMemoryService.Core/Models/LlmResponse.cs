namespace NpcMemoryService.Core.Models
{
    /// <summary>Token consumption reported by the provider. Useful for diagnostics.</summary>
    public sealed record LlmUsage(int PromptTokens, int CompletionTokens);

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
    }
}

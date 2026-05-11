namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
    public sealed class OpenRouterConfig
    {
        public required string ApiKey  { get; init; }
        public required string Model   { get; init; }
        public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1";
    }
}

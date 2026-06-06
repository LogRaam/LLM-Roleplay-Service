// Code written by Gabriel Mailhot, 11/05/2026.
// Updated 05/06/2026: live ApiKeyProvider so a host can change the key without
//                     rebuilding the client (read on every request).

#region

using System;

#endregion

namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
    public sealed class OpenRouterConfig
    {
        /// <summary>Static API key. Optional when <see cref="ApiKeyProvider" /> is set.</summary>
        public string ApiKey { get; init; }

        /// <summary>
        ///   Live key resolver, invoked on every request. Lets a host apply a key change
        ///   (e.g. from an in-game options menu) without rebuilding the client. When set,
        ///   it takes precedence over <see cref="ApiKey" />.
        /// </summary>
        public Func<string> ApiKeyProvider { get; init; }

        /// <summary>Static model id. Optional when <see cref="ModelProvider" /> is set.</summary>
        public string Model { get; init; }

        /// <summary>
        ///   Live model resolver, invoked on every request — lets a host change the model
        ///   from an options menu without rebuilding the client. Takes precedence over
        ///   <see cref="Model" />.
        /// </summary>
        public Func<string> ModelProvider { get; init; }

        public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1";

        /// <summary>Resolves the key at call time: the live provider first, then the static key.</summary>
        public string ResolveApiKey() => ApiKeyProvider?.Invoke() ?? ApiKey;

        /// <summary>Resolves the model at call time: the live provider first, then the static value.</summary>
        public string ResolveModel() => ModelProvider?.Invoke() ?? Model;
    }
}

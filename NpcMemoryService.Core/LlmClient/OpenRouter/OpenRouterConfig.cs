// Code written by Gabriel Mailhot, 11/05/2026.
// Updated 05/06/2026: live ApiKeyProvider so a host can change the key without
//                     rebuilding the client (read on every request).
// Updated 09/06/2026: live BaseUrlProvider + UseSystemPromptCachingProvider so a host
//                     can switch between OpenAI-compatible providers (OpenRouter,
//                     NanoGPT, ...) at runtime, including disabling the Anthropic-style
//                     cache_control extension for providers that do not accept it.

#region

using System;

#endregion

namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
    public sealed class OpenRouterConfig
    {
        /// <summary>Static API key. Optional when <see cref="ApiKeyProvider" /> is set.</summary>
        public string? ApiKey { get; init; }

        /// <summary>
        ///   Live key resolver, invoked on every request. Lets a host apply a key change
        ///   (e.g. from an in-game options menu) without rebuilding the client. When set,
        ///   it takes precedence over <see cref="ApiKey" />.
        /// </summary>
        public Func<string>? ApiKeyProvider { get; init; }

        /// <summary>Static model id. Optional when <see cref="ModelProvider" /> is set.</summary>
        public string? Model { get; init; }

        /// <summary>
        ///   Live model resolver, invoked on every request — lets a host change the model
        ///   from an options menu without rebuilding the client. Takes precedence over
        ///   <see cref="Model" />.
        /// </summary>
        public Func<string>? ModelProvider { get; init; }

        public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1";

        /// <summary>
        ///   Live base-URL resolver, invoked on every request — lets a host switch between
        ///   OpenAI-compatible providers (OpenRouter, NanoGPT, ...) from an options menu
        ///   without rebuilding the client. Takes precedence over <see cref="BaseUrl" />.
        /// </summary>
        public Func<string>? BaseUrlProvider { get; init; }

        /// <summary>
        ///   Live resolver for the model's reasoning effort, invoked on every request. Returns a
        ///   keyword the client maps onto OpenRouter's <c>reasoning</c> parameter:
        ///   <list type="bullet">
        ///     <item><c>off</c>/<c>none</c>/<c>disabled</c> → <c>reasoning.enabled = false</c>;</item>
        ///     <item><c>minimal</c>/<c>low</c>/<c>medium</c>/<c>high</c> → <c>reasoning.effort</c>;</item>
        ///     <item>null/empty/<c>default</c> → omit the field (model default).</item>
        ///   </list>
        ///   Lowering or disabling reasoning makes some models (notably Grok) markedly less
        ///   prone to moralizing refusals on consensual adult fiction.
        /// </summary>
        public Func<string?>? ReasoningProvider { get; init; }

        /// <summary>
        ///   Live resolver deciding whether the system prompt is sent as a content array
        ///   carrying the Anthropic-style <c>cache_control: ephemeral</c> breakpoint
        ///   (OpenRouter honors it; some aggregators reject the array form). When it
        ///   returns false, the system prompt is sent as a plain OpenAI string — the
        ///   maximally-portable form. Null = caching enabled (the historical default).
        /// </summary>
        public Func<bool>? UseSystemPromptCachingProvider { get; init; }

        /// <summary>
        ///   Live resolver for the per-request timeout in seconds, invoked on every request — lets a host
        ///   raise it for slow local models (or a long prompt that makes a reasoning model think past the
        ///   default) from an options menu without rebuilding the client. The client enforces it with a
        ///   linked <see cref="System.Threading.CancellationTokenSource" />, independent of the HttpClient's
        ///   own timeout (which the host should set high so THIS governs). Null/out-of-range falls back to
        ///   <see cref="DefaultTimeoutSeconds" />.
        /// </summary>
        public Func<int>? TimeoutSecondsProvider { get; init; }

        /// <summary>The fallback per-request timeout (seconds) when no provider is set — generous for large prompts.</summary>
        public const int DefaultTimeoutSeconds = 120;

        /// <summary>Resolves the key at call time: the live provider first, then the static key.</summary>
        public string? ResolveApiKey() => ApiKeyProvider?.Invoke() ?? ApiKey;

        /// <summary>
        ///   Resolves the per-request timeout in seconds, clamped to a sane window [15, 600] so a careless
        ///   host value can neither time out instantly nor hang for many minutes. The live provider wins.
        /// </summary>
        public int ResolveTimeoutSeconds()
        {
            int s = TimeoutSecondsProvider?.Invoke() ?? DefaultTimeoutSeconds;
            if (s <= 0) s = DefaultTimeoutSeconds;
            return s < 15 ? 15 : s > 600 ? 600 : s;
        }

        /// <summary>Resolves the model at call time: the live provider first, then the static value.</summary>
        public string? ResolveModel() => ModelProvider?.Invoke() ?? Model;

        /// <summary>Resolves the base URL at call time: the live provider first, then the static value.</summary>
        public string ResolveBaseUrl() => BaseUrlProvider?.Invoke() ?? BaseUrl;

        /// <summary>Resolves the reasoning keyword at call time. Null = omit (model default).</summary>
        public string? ResolveReasoning() => ReasoningProvider?.Invoke();

        /// <summary>
        ///   Resolves whether to emit the cacheable system-prompt content array.
        ///   Defaults to true (caching on) when no provider is set.
        /// </summary>
        public bool ResolveUseSystemPromptCaching() => UseSystemPromptCachingProvider?.Invoke() ?? true;
    }
}

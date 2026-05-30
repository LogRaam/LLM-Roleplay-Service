// Code written by Gabriel Mailhot, 11/05/2026.

#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
    /// <summary>
    ///   Maps our internal protocol to OpenRouter's OpenAI-compatible API.
    ///   The caller owns the <see cref="HttpClient" /> lifetime.
    ///   Prompt caching: the system message is sent as a content array with a
    ///   <c>cache_control</c> breakpoint, signaling that the system prompt is
    ///   a cacheable prefix. Providers that support caching (Anthropic, partially
    ///   OpenAI) honor this; others ignore it gracefully.
    /// </summary>
    public sealed class OpenRouterClient : ILlmClient
    {
        private readonly OpenRouterConfig _config;
        private readonly HttpClient _httpClient;

        public OpenRouterClient(HttpClient httpClient, OpenRouterConfig config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        #region private properties

        private string ChatCompletionsUrl =>
            _config.BaseUrl.TrimEnd(trimChars: '/') + "/chat/completions";

        #endregion

        public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
        {
            try
            {
                using HttpRequestMessage httpRequest = BuildHttpRequest(request);
                using HttpResponseMessage? httpResponse = await _httpClient.SendAsync(httpRequest, ct)
                                                                           .ConfigureAwait(false);

                var responseJson = await httpResponse.Content
                                                     .ReadAsStringAsync()
                                                     .ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode) return Failure($"HTTP {(int) httpResponse.StatusCode}: {responseJson}");

                return ParseResponse(responseJson);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        #region private

        private static LlmResponse Failure(string message) =>
            new LlmResponse {Content = string.Empty, IsSuccess = false, ErrorMessage = message};

        // ── Response parsing ──────────────────────────────────────────────────

        private static LlmResponse ParseResponse(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                var content = root
                              .GetProperty("choices")[0]
                              .GetProperty("message")
                              .GetProperty("content")
                              .GetString() ??
                              string.Empty;

                LlmUsage? usage = null;
                if (root.TryGetProperty("usage", out JsonElement usageEl))
                {
                    var prompt = usageEl.GetProperty("prompt_tokens").GetInt32();
                    var completion = usageEl.GetProperty("completion_tokens").GetInt32();

                    // Cached token counts when reported by the provider.
                    // OpenRouter normalizes these into the usage block.
                    var cachedRead = TryReadInt(usageEl, "cache_read_input_tokens") ?? TryReadInt(usageEl, "cached_tokens") ?? TryReadPromptDetailsCached(usageEl);

                    usage = new LlmUsage(prompt, completion) {
                        CachedPromptTokens = cachedRead
                    };
                }

                return new LlmResponse {Content = content, IsSuccess = true, Usage = usage};
            }
            catch (Exception ex)
            {
                return Failure($"Failed to parse response: {ex.Message}");
            }
        }

        private static int? TryReadInt(JsonElement parent, string property)
        {
            if (parent.TryGetProperty(property, out JsonElement el) && el.ValueKind == JsonValueKind.Number) return el.GetInt32();

            return null;
        }

        private static int? TryReadPromptDetailsCached(JsonElement usage)
        {
            // OpenAI nests cache stats under prompt_tokens_details.
            if (usage.TryGetProperty("prompt_tokens_details", out JsonElement details) && details.TryGetProperty("cached_tokens", out JsonElement cached) && cached.ValueKind == JsonValueKind.Number) return cached.GetInt32();

            return null;
        }

        // ── Request building ──────────────────────────────────────────────────

        private HttpRequestMessage BuildHttpRequest(LlmRequest request)
        {
            var json = JsonSerializer.Serialize(ToWireFormat(request));

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl);
            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.ApiKey);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return httpRequest;
        }

        /// <summary>
        ///   Translates our <see cref="LlmRequest" /> into OpenRouter's
        ///   OpenAI-compatible format, marking the system prompt as a
        ///   cacheable prefix via <c>cache_control: ephemeral</c>.
        /// </summary>
        private object ToWireFormat(LlmRequest request)
        {
            var messages = new List<object> {
                // System message as a content array enables the cache_control
                // breakpoint. Anthropic/OpenAI honor it; others ignore it.
                new {
                    role = "system",
                    content = new object[] {
                        new {
                            type = "text",
                            text = request.SystemPrompt,
                            cache_control = new {type = "ephemeral"}
                        }
                    }
                }
            };

            foreach (LlmMessage msg in request.Messages)
                messages.Add(new {
                    role = msg.Role == MessageRole.User
                        ? "user"
                        : "assistant",
                    content = msg.Content
                });

            return new {
                model = _config.Model,
                messages,
                temperature = (double) request.Parameters.Creativity,
                max_tokens = request.Parameters.MaxTokens
            };
        }

        #endregion
    }
}
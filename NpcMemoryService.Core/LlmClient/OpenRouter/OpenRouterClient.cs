using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
    /// <summary>
    /// Maps our internal protocol to OpenRouter's OpenAI-compatible API.
    /// The caller owns the <see cref="HttpClient"/> lifetime.
    /// </summary>
    public sealed class OpenRouterClient : ILlmClient
    {
        private readonly HttpClient        _httpClient;
        private readonly OpenRouterConfig  _config;

        private string ChatCompletionsUrl =>
            _config.BaseUrl.TrimEnd('/') + "/chat/completions";

        public OpenRouterClient(HttpClient httpClient, OpenRouterConfig config)
        {
            _httpClient = httpClient;
            _config     = config;
        }

        public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
        {
            try
            {
                using var httpRequest = BuildHttpRequest(request);
                using var httpResponse = await _httpClient.SendAsync(httpRequest, ct)
                    .ConfigureAwait(false);

                string responseJson = await httpResponse.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Failure($"HTTP {(int)httpResponse.StatusCode}: {responseJson}");
                }

                return ParseResponse(responseJson);
            }
            catch (OperationCanceledException)
            {
                throw;  // let cancellation propagate normally
            }
            catch (Exception ex)
            {
                return Failure(ex.Message);
            }
        }

        // ── Request building ──────────────────────────────────────────────────

        private HttpRequestMessage BuildHttpRequest(LlmRequest request)
        {
            string json = JsonSerializer.Serialize(ToWireFormat(request));

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl);
            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.ApiKey);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return httpRequest;
        }

        /// <summary>
        /// Translates our <see cref="LlmRequest"/> into the anonymous object
        /// that OpenRouter expects (OpenAI chat completions format).
        /// </summary>
        private object ToWireFormat(LlmRequest request)
        {
            var messages = new List<object>
            {
                new { role = "system", content = request.SystemPrompt }
            };

            foreach (LlmMessage msg in request.Messages)
            {
                messages.Add(new
                {
                    role    = msg.Role == MessageRole.User ? "user" : "assistant",
                    content = msg.Content
                });
            }

            return new
            {
                model       = _config.Model,
                messages,
                temperature = (double)request.Parameters.Creativity,
                max_tokens  = request.Parameters.MaxTokens
            };
        }

        // ── Response parsing ──────────────────────────────────────────────────

        private static LlmResponse ParseResponse(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                string content = root
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                LlmUsage? usage = null;
                if (root.TryGetProperty("usage", out JsonElement usageEl))
                {
                    usage = new LlmUsage(
                        PromptTokens:     usageEl.GetProperty("prompt_tokens").GetInt32(),
                        CompletionTokens: usageEl.GetProperty("completion_tokens").GetInt32()
                    );
                }

                return new LlmResponse { Content = content, IsSuccess = true, Usage = usage };
            }
            catch (Exception ex)
            {
                return Failure($"Failed to parse response: {ex.Message}");
            }
        }

        private static LlmResponse Failure(string message) =>
            new LlmResponse { Content = string.Empty, IsSuccess = false, ErrorMessage = message };
    }
}

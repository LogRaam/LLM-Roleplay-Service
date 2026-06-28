// Code written by Gabriel Mailhot, 23/06/2026.

#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

      private string ChatCompletionsUrl =>
         _config.ResolveBaseUrl().TrimEnd(trimChars: '/') + "/chat/completions";

      public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
      {
         LlmResponse response = await SendOnceAsync(request, ct).ConfigureAwait(false);

         // One retry when the provider cut the reply off by output length. Some models
         // (notably certain DeepSeek deployments) truncate a reply mid-sentence; a fresh
         // generation usually completes. Bounded to a single retry so a model that always
         // truncates cannot loop or double-bill indefinitely.
         if (response.IsSuccess && IsLengthTruncated(response.FinishReason))
         {
            LlmResponse retry = await SendOnceAsync(request, ct).ConfigureAwait(false);

            if (retry.IsSuccess) return retry; // prefer the retry even if also truncated — it's no worse
         }

         return response;
      }

      #region private

      /// <summary>
      ///   Maps a reasoning keyword to OpenRouter's <c>reasoning</c> object, or null to omit it
      ///   entirely (the model's default). <c>off/none/disabled</c> turns reasoning off;
      ///   <c>minimal/low/medium/high</c> sets the effort level.
      /// </summary>
      private static object? BuildReasoning(string? setting)
      {
         if (string.IsNullOrWhiteSpace(setting)) return null;
         string keyword = setting!.Trim().ToLowerInvariant();
         switch (keyword)
         {
            case "off":
            case "none":
            case "disabled":
            case "false":
               return new {enabled = false};
            case "minimal":
            case "low":
            case "medium":
            case "high":
               return new {effort = keyword};
            default: // "default" or anything unrecognized → let the model decide
               return null;
         }
      }

      private static LlmResponse Failure(string message) => new() {Content = string.Empty, IsSuccess = false, ErrorMessage = message};

      private static bool IsLengthTruncated(string? finishReason)
         => string.Equals(finishReason, "length", StringComparison.OrdinalIgnoreCase);

      // ── Response parsing ──────────────────────────────────────────────────

      private static LlmResponse ParseResponse(string json)
      {
         try
         {
            JObject root = JObject.Parse(json);

            // Preserve the original contract: a body without choices[0].message.content
            // is a failure, not a silent empty success — an unexpected API error body
            // would otherwise read as an empty reply.
            JToken? contentToken = root["choices"]?[0]?["message"]?["content"];

            if (contentToken == null || contentToken.Type == JTokenType.Null)
               return Failure("Response contained no message content.");

            string content = contentToken.Value<string>() ?? string.Empty;

            // "length" here means the reply was cut off by the token limit — surfaced so
            // the host can log it and the one-shot retry above can fire.
            var finishReason = root["choices"]?[0]?["finish_reason"]?.Value<string>();

            LlmUsage? usage = null;
            if (root["usage"] is JObject usageEl)
            {
               int prompt = usageEl["prompt_tokens"]?.Value<int>() ?? 0;
               int completion = usageEl["completion_tokens"]?.Value<int>() ?? 0;

               // Cached token counts when reported by the provider.
               // OpenRouter normalizes these into the usage block.
               int? cachedRead = ReadIntOrNull(usageEl["cache_read_input_tokens"]) ?? ReadIntOrNull(usageEl["cached_tokens"]) ?? ReadIntOrNull(usageEl["prompt_tokens_details"]?["cached_tokens"]);

               usage = new LlmUsage(prompt, completion) {
                  CachedPromptTokens = cachedRead
               };
            }

            return new LlmResponse {Content = content, IsSuccess = true, Usage = usage, FinishReason = finishReason};
         }
         catch (Exception ex)
         {
            return Failure($"Failed to parse response: {ex.Message}. The endpoint did not return a single JSON " +
                           $"object (a streaming or non-OpenAI response?). Body began with: {Snippet(json)}");
         }
      }

      /// <summary>A short, single-line excerpt of a raw body, for diagnosing a non-JSON response.</summary>
      private static string Snippet(string? body)
      {
         if (string.IsNullOrEmpty(body)) return "(empty)";

         string oneLine = body!.Replace("\r", " ").Replace("\n", " ").Trim();

         return oneLine.Length <= 160 ? oneLine : oneLine.Substring(0, 160) + "…";
      }

      private static int? ReadIntOrNull(JToken? token)
         => token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            ? token.Value<int>()
            : null;

      // ── Request building ──────────────────────────────────────────────────

      private HttpRequestMessage BuildHttpRequest(LlmRequest request)
      {
         string json = JsonConvert.SerializeObject(ToWireFormat(request));

         var httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl);
         httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _config.ResolveApiKey());
         httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

         return httpRequest;
      }

      /// <summary>
      ///   Translates our <see cref="LlmRequest" /> into the provider's
      ///   OpenAI-compatible format. When system-prompt caching is enabled
      ///   (OpenRouter), the system message is a content array marking a
      ///   cacheable prefix via <c>cache_control: ephemeral</c>. When disabled
      ///   (providers that reject the array form, e.g. NanoGPT), it is sent as a
      ///   plain OpenAI string — the maximally-portable form.
      /// </summary>
      /// <summary>
      ///   Builds the system message. With caching off, a plain OpenAI string (maximally portable). With
      ///   caching on and a stable prefix supplied, two text blocks — the stable prefix carrying the
      ///   <c>cache_control: ephemeral</c> breakpoint, then the per-turn dynamic tail sent fresh — so the
      ///   cache survives the encounter/day changing each turn. Without a split, the whole prompt is the
      ///   cached block (prior behaviour). Anthropic/OpenRouter honour the array form; others ignore it.
      /// </summary>
      private object BuildSystemMessage(LlmRequest request)
      {
         if (!_config.ResolveUseSystemPromptCaching())
            return new {role = "system", content = request.SystemPrompt};

         string? stable = request.StableSystemPrompt;

         if (!string.IsNullOrEmpty(stable) && request.SystemPrompt.Length > stable!.Length && request.SystemPrompt.StartsWith(stable, StringComparison.Ordinal))
            return new {
               role = "system",
               content = new object[] {
                  new {type = "text", text = stable, cache_control = new {type = "ephemeral"}},
                  new {type = "text", text = request.SystemPrompt.Substring(stable.Length)}
               }
            };

         return new {
            role = "system",
            content = new object[] {
               new {type = "text", text = request.SystemPrompt, cache_control = new {type = "ephemeral"}}
            }
         };
      }

      private async Task<LlmResponse> SendOnceAsync(LlmRequest request, CancellationToken ct)
      {
         try
         {
            using HttpRequestMessage httpRequest = BuildHttpRequest(request);
            using HttpResponseMessage? httpResponse = await _httpClient.SendAsync(httpRequest, ct)
                                                                       .ConfigureAwait(false);

            string? responseJson = await httpResponse.Content
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

      private object ToWireFormat(LlmRequest request)
      {
         var messages = new List<object> {BuildSystemMessage(request)};

         foreach (LlmMessage msg in request.Messages)
            messages.Add(new {
               role = msg.Role == MessageRole.User
                  ? "user"
                  : "assistant",
               content = msg.Content
            });

         var payload = new Dictionary<string, object> {
            ["model"] = _config.ResolveModel() ?? string.Empty,
            ["messages"] = messages,
            ["temperature"] = (double) request.Parameters.Creativity,
            ["max_tokens"] = request.Parameters.MaxTokens,
            // Explicitly non-streaming: we parse one JSON object, not an SSE "data: ..." chunk stream. Some
            // OpenAI-compatible providers (e.g. Chub) stream by default, which would arrive as unparseable text.
            ["stream"] = false
         };

         // OpenRouter reasoning control: lowering or disabling reasoning cuts moralizing
         // refusals on consensual adult fiction (see OpenRouterConfig.ReasoningProvider).
         object? reasoning = BuildReasoning(_config.ResolveReasoning());
         if (reasoning != null) payload["reasoning"] = reasoning;

         return payload;
      }

      #endregion
   }
}
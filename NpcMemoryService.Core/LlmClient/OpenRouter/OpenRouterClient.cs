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

         // One retry when the provider cut the reply off by output length, OR returned an empty
         // reply outright. Some models (notably certain DeepSeek deployments) truncate a reply
         // mid-sentence; a fresh generation usually completes. Reasoning models (MiMo, GLM, R1...)
         // can also return an empty content with finish_reason "stop" when the template closes on
         // reasoning alone; an empty chat reply is never legitimate, so it earns the same retry.
         // Bounded to a single retry so a model that always truncates cannot loop or double-bill.
         if (response.IsSuccess && (IsLengthTruncated(response.FinishReason) || string.IsNullOrWhiteSpace(response.Content)))
         {
            // An EMPTY reply means a reasoning model spent the whole completion budget thinking
            // (or closed on reasoning alone) and never wrote any text. Retry with double the
            // budget so there is room for prose after the thinking; a truncated-but-present
            // reply keeps the original budget (a fresh roll usually lands shorter).
            bool thinkingAteBudget = string.IsNullOrWhiteSpace(response.Content);
            LlmRequest retryRequest = thinkingAteBudget
               ? new LlmRequest {
                  Messages = request.Messages,
                  Parameters = new LlmParameters {
                     MaxTokens = request.Parameters.MaxTokens * 2,
                     Creativity = request.Parameters.Creativity
                  },
                  StableSystemPrompt = request.StableSystemPrompt,
                  SystemPrompt = request.SystemPrompt
               }
               : request;

            LlmResponse retry = await SendOnceAsync(retryRequest, ct).ConfigureAwait(false);

            if (retry.IsSuccess && !string.IsNullOrEmpty(retry.Content))
               // Prefer the retry even if also truncated — it's no worse. Stamped WasRetried so the
               // host can log real retry frequency (token counts alone cannot reveal it).
               return new LlmResponse {
                  Content = retry.Content,
                  IsSuccess = true,
                  Usage = retry.Usage,
                  FinishReason = retry.FinishReason,
                  WasRetried = true
               };

            if (!thinkingAteBudget) return response; // original had text; keep it over a failed retry

            return Failure("The model spent its entire reply budget on internal reasoning twice and produced " +
                           "no text (reasoning models such as MiMo or GLM think at length before writing). " +
                           "Lower the Reasoning Effort in Mod Options, or use a model that reasons less." +
                           (retry.ErrorMessage != null ? $" Last error: {retry.ErrorMessage}" : string.Empty));
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

      internal static LlmResponse ParseResponse(string json)
      {
         // Some endpoints stream the reply (SSE) even when we ask for a single object; rebuild it into the
         // standard envelope first so the rest of this method is unchanged. A non-streamed body passes through.
         if (ChatResponseTransformer.TryNormalizeStreamedResponse(json, out string normalized))
            json = normalized;

         try
         {
            JObject root = JObject.Parse(json);

            // Preserve the original contract: a body without choices[0].message.content
            // is a failure, not a silent empty success — an unexpected API error body
            // would otherwise read as an empty reply.
            JToken? message = root["choices"]?[0]?["message"];
            JToken? contentToken = message?["content"];

            if (contentToken == null || contentToken.Type == JTokenType.Null)
            {
               // A reasoning model (MiMo, GLM, R1...) can spend the entire completion budget
               // "thinking": the body then carries the reasoning text but a null content, with
               // finish_reason "length". Surface that as an EMPTY length-truncated success so
               // CompleteAsync's bigger-budget retry fires. Only a body with neither content
               // nor reasoning (an API error envelope) stays the original hard failure.
               string reasoningText = message?["reasoning"]?.Value<string>()
                                      ?? message?["reasoning_content"]?.Value<string>()
                                      ?? string.Empty;
               var cutReason = root["choices"]?[0]?["finish_reason"]?.Value<string>();

               if (reasoningText.Length > 0 && IsLengthTruncated(cutReason))
                  return new LlmResponse {Content = string.Empty, IsSuccess = true, FinishReason = cutReason};

               if (reasoningText.Length > 0)
                  return Failure($"The model produced only reasoning text and no reply (finish_reason: {cutReason ?? "unknown"}). " +
                                 "Lower the Reasoning Effort in Mod Options, or use a model that reasons less.");

               return Failure("Response contained no message content.");
            }

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
         // Enforce the timeout ourselves with a linked source, independent of HttpClient.Timeout (which the
         // host sets high so this governs). A long prompt on a slow/reasoning model is the usual cause of the
         // "A task was canceled" the player sees at the HttpClient default — now it is configurable + named.
         int timeoutSeconds = _config.ResolveTimeoutSeconds();
         using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
         timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

         try
         {
            using HttpRequestMessage httpRequest = BuildHttpRequest(request);
            using HttpResponseMessage? httpResponse = await _httpClient.SendAsync(httpRequest, timeoutCts.Token)
                                                                       .ConfigureAwait(false);

            string? responseJson = await httpResponse.Content
                                                     .ReadAsStringAsync()
                                                     .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode) return Failure($"HTTP {(int) httpResponse.StatusCode}: {responseJson}");

            return ParseResponse(responseJson);
         }
         catch (OperationCanceledException) when (ct.IsCancellationRequested)
         {
            throw; // the CALLER cancelled (the chat closed or was replaced) — propagate as before
         }
         catch (OperationCanceledException)
         {
            // OUR timeout fired (or HttpClient's) rather than a caller cancellation — turn it into a clear,
            // actionable failure instead of the opaque "A task was canceled".
            return Failure($"The model did not respond within {timeoutSeconds}s. Try a faster model, a shorter " +
                           "message, or raise 'LLM Response Timeout' in Mods -> Mod Options -> Calradia Remembers.");
         }
         catch (Exception ex)
         {
            return Failure(ex.Message);
         }
      }

      /// <summary>
      ///   Translates our <see cref="LlmRequest" /> into the provider's
      ///   OpenAI-compatible format. When system-prompt caching is enabled
      ///   (OpenRouter), the system message is a content array marking a
      ///   cacheable prefix via <c>cache_control: ephemeral</c>. When disabled
      ///   (providers that reject the array form, e.g. NanoGPT), it is sent as a
      ///   plain OpenAI string — the maximally-portable form.
      /// </summary>
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
            // Explicitly non-streaming: we parse one JSON object, not an SSE "data: ..." chunk stream. Some
            // OpenAI-compatible providers (e.g. Chub) stream by default, which would arrive as unparseable text.
            ["stream"] = false
         };

         // The OpenAI reasoning models (gpt-5*, o1/o3/o4*), reached directly, reject "max_tokens" (they want
         // "max_completion_tokens") and refuse a custom "temperature". The host's policy decides the shape;
         // aggregators (OpenRouter/NanoGPT) normalize, so for them this stays the classic max_tokens+temperature.
         ChatParameterOptions options = _config.ResolveParameterOptions();
         payload[options.UseMaxCompletionTokens ? "max_completion_tokens" : "max_tokens"] = request.Parameters.MaxTokens;
         if (options.IncludeTemperature) payload["temperature"] = (double) request.Parameters.Creativity;

         // OpenRouter reasoning control: lowering or disabling reasoning cuts moralizing
         // refusals on consensual adult fiction (see OpenRouterConfig.ReasoningProvider).
         object? reasoning = BuildReasoning(_config.ResolveReasoning());
         if (reasoning != null) payload["reasoning"] = reasoning;

         return payload;
      }

      #endregion
   }
}
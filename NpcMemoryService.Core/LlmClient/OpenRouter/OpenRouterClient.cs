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

         // One retry when the provider cut the reply off by output length, by its CONTENT FILTER, or
         // returned an empty reply outright. Some models (notably certain DeepSeek deployments) truncate
         // a reply mid-sentence; a moderated host can stop generation partway with finish_reason
         // "content_filter" (the reply arrives cut mid-act, which players read as the MOD censoring);
         // a fresh generation usually completes (sampling differs roll to roll). Reasoning models
         // (MiMo, GLM, R1...) can also return an empty content with finish_reason "stop" when the
         // template closes on reasoning alone; an empty chat reply is never legitimate, so it earns
         // the same retry. Bounded to a single retry so a model that always truncates cannot loop
         // or double-bill. AllowTruncationRetry=false skips this whole block regardless of the reply, so a
         // caller that wants to fail FAST on the first incomplete reply (the mod's PROSE call, whose own
         // fallback takes over from here) never pays for it.
         if (request.Parameters.AllowTruncationRetry && response.IsSuccess && (IsLengthTruncated(response.FinishReason) || IsContentFiltered(response.FinishReason) || string.IsNullOrWhiteSpace(response.Content)))
         {
            // A reply that hit the completion cap, EMPTY or cut mid-sentence, means the budget ran out before
            // the prose finished: a reasoning model spent it thinking (some, notably certain DeepSeek
            // deployments, reason INTERNALLY even when reasoning is set OFF, so the visible reply is a couple of
            // words or stops mid-act), or the prose genuinely ran long. A fresh roll at the SAME budget just
            // truncates again, so retry with DOUBLE the budget to leave room for the thinking AND the full reply.
            // A content-filter cut is not a budget problem, so it keeps the original budget (the filter trips at
            // a different point each roll).
            bool budgetExhausted = string.IsNullOrWhiteSpace(response.Content) || IsLengthTruncated(response.FinishReason);
            LlmRequest retryRequest = budgetExhausted
               ? new LlmRequest {
                  Messages = request.Messages,
                  Parameters = new LlmParameters {
                     MaxTokens = request.Parameters.MaxTokens * 2,
                     Creativity = request.Parameters.Creativity,
                     // Carry every OTHER generation setting across untouched: only the budget is being
                     // changed here. Rebuilding the object silently dropped the anti-repetition penalty on
                     // exactly the retries most likely to ramble.
                     PresencePenalty = request.Parameters.PresencePenalty
                  },
                  StableSystemPrompt = request.StableSystemPrompt,
                  SystemPrompt = request.SystemPrompt,
                  // Carry the per-request model override across the retry too, or the bigger-budget retry
                  // would silently fall back to the resolved model on exactly the extractor calls that set it.
                  ModelOverride = request.ModelOverride
               }
               : request;

            LlmResponse retry = await SendOnceAsync(retryRequest, ct).ConfigureAwait(false);

            if (retry.IsSuccess && !string.IsNullOrEmpty(retry.Content))
            {
               // Prefer the retry even if also truncated, it's no worse. EXCEPT after a content-filter
               // cut: the filter trips at a different point each roll, so keep whichever roll carried
               // FURTHER before being stopped. Stamped WasRetried so the host can log real retry
               // frequency (token counts alone cannot reveal it).
               bool originalCarriedFurther = IsContentFiltered(response.FinishReason)
                                             && response.Content != null
                                             && response.Content.Length > retry.Content.Length;

               return new LlmResponse {
                  Content = originalCarriedFurther ? response.Content! : retry.Content,
                  IsSuccess = true,
                  Usage = retry.Usage,
                  FinishReason = originalCarriedFurther ? response.FinishReason : retry.FinishReason,
                  WasRetried = true
               };
            }

            // Retry failed: keep any text the ORIGINAL carried (a truncated beat still beats an error); only a
            // truly empty original falls through to the hard failure.
            if (!string.IsNullOrWhiteSpace(response.Content)) return response;

            return Failure("The model spent its entire reply budget on internal reasoning twice and produced " +
                           "no text (reasoning models such as MiMo, GLM, or DeepSeek think at length before writing). " +
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

      /// <summary>
      ///   Maps the same reasoning keyword to the TOP-LEVEL OpenAI-style <c>reasoning_effort</c> STRING (what a
      ///   local OpenAI-compatible server such as Ollama honors; it ignores OpenRouter's <c>reasoning</c> object,
      ///   which let a reasoning model like Qwen3.5 Heretic burn the whole reply budget thinking). <c>off/none/
      ///   disabled/false</c> → <c>"none"</c>; <c>minimal/low/medium/high</c> → that word; anything else (incl.
      ///   <c>default</c>) → null to omit the field entirely, so an untouched setup sends nothing.
      /// </summary>
      internal static string? BuildReasoningEffort(string? setting)
      {
         if (string.IsNullOrWhiteSpace(setting)) return null;
         switch (setting!.Trim().ToLowerInvariant())
         {
            case "off":
            case "none":
            case "disabled":
            case "false":
               return "none";
            case "minimal":
            case "low":
            case "medium":
            case "high":
               return setting!.Trim().ToLowerInvariant();
            default:
               return null;
         }
      }

      private static LlmResponse Failure(string message) => new() {Content = string.Empty, IsSuccess = false, ErrorMessage = message};

      private static bool IsLengthTruncated(string? finishReason)
         => string.Equals(finishReason, "length", StringComparison.OrdinalIgnoreCase);

      /// <summary>
      ///   True when the provider stopped generation because ITS content filter tripped: the reply arrives
      ///   cut mid-sentence with a "content_filter" finish reason (providers vary the exact spelling, so
      ///   the check is separator-insensitive; "moderation" is the other reported variant).
      /// </summary>
      internal static bool IsContentFiltered(string? finishReason)
      {
         if (string.IsNullOrEmpty(finishReason)) return false;

         string normalized = finishReason!.Replace("-", "").Replace("_", "").Trim();

         return string.Equals(normalized, "contentfilter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "moderation", StringComparison.OrdinalIgnoreCase);
      }

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

            // Reasoning models can also leak their chain-of-thought INLINE in the content as
            // <think>...</think> (nemotron and friends) instead of the separate field handled above;
            // strip it here so every consumer (chat replies, memory lines, captive scenes) is covered
            // at once. A trace cut off before its closing tag strips to EMPTY, which routes into
            // CompleteAsync's bigger-budget retry exactly like the separate-field case.
            string content = ReasoningTraceStripper.Strip(contentToken.Value<string>());

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
      internal object ToWireFormat(LlmRequest request)
      {
         var messages = new List<object> {BuildSystemMessage(request)};

         foreach (LlmMessage msg in request.Messages)
            messages.Add(new {
               role = msg.Role == MessageRole.User
                  ? "user"
                  : "assistant",
               content = msg.Content
            });

         // A non-blank per-request override wins for this one call; otherwise the client's normally-resolved
         // model. This is what lets a single extractor call target a cheaper model with no global state change.
         string model = string.IsNullOrWhiteSpace(request.ModelOverride)
            ? _config.ResolveModel() ?? string.Empty
            : request.ModelOverride!.Trim();

         var payload = new Dictionary<string, object> {
            ["model"] = model,
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

         // Audit M13: the one anti-repetition lever that also reaches weak models, which ignore the prompt's
         // "never repeat a gesture or phrase". Sent only when the host asked for it AND the endpoint accepts
         // custom sampling at all (the same signal temperature rides on: the OpenAI reasoning models reject
         // both). At zero the field is omitted entirely, so every existing provider keeps its exact payload.
         if (options.IncludeTemperature && request.Parameters.PresencePenalty > 0f)
            payload["presence_penalty"] = (double) request.Parameters.PresencePenalty;

         // Reasoning control: lowering or disabling reasoning cuts moralizing refusals on consensual adult
         // fiction, and stops a local reasoning model from spending the whole reply budget thinking. The keyword
         // is the same; only the WIRE SHAPE differs by endpoint. OpenRouter reads its own "reasoning" object; a
         // local OpenAI-compatible server (Ollama) reads the top-level "reasoning_effort" string and ignores the
         // object. Emit exactly ONE of the two so neither backend receives a field it silently drops.
         string? reasoningKeyword = _config.ResolveReasoning();
         if (_config.ResolveReasoningAsEffortString())
         {
            string? effort = BuildReasoningEffort(reasoningKeyword);
            if (effort != null) payload["reasoning_effort"] = effort;
         }
         else
         {
            object? reasoning = BuildReasoning(reasoningKeyword);
            if (reasoning != null) payload["reasoning"] = reasoning;
         }

         // OpenRouter provider routing: pin the request to specific providers, in the player's chosen order.
         // Several providers moderate their own OUTPUT and cut generation the moment profanity appears, which
         // arrives as a reply chopped mid-sentence. Omitted entirely when nothing is pinned, so every other
         // OpenAI-compatible endpoint keeps receiving exactly the payload it received before.
         string[] providerSlugs = _config.ResolveProviderSlugs();
         if (providerSlugs.Length > 0)
            payload["provider"] = new Dictionary<string, object> {
               ["order"] = providerSlugs,
               ["allow_fallbacks"] = _config.ResolveAllowProviderFallbacks()
            };

         return payload;
      }

      #endregion
   }
}
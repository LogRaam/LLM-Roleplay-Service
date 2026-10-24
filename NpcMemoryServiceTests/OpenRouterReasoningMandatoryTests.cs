// Code written by Gabriel Mailhot, 06/10/2026.
// A model whose reasoning cannot be turned off. Thoragoros1 (Nexus, 06/10/2026), Gemini 3.7 Flash on 2.7.1: every
// interpreter call failed with HTTP 400 "Reasoning is mandatory for this endpoint and cannot be disabled", so no deed
// of any reply was recorded. The interpreter and every housekeeping call send reasoning OFF on purpose (it keeps them
// fast and within budget), and that model refuses the request outright. On that refusal the call is made once more at
// the lowest effort ("low"), and a model known to refuse (the Gemini 3 family) is sent "low" from its first call. Not
// the model's default: cost bench 24/10/2026, Gemini 3.8 Flash left to its default thought past the interpreter's
// budget and was asked twice per reading. If this breaks, a whole family of models records nothing the characters do,
// or pays double for every deed it reads.

#region

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>Refuses any request that turns reasoning off, as that endpoint does, and answers the others.</summary>
   internal sealed class ReasoningMandatoryHandler : HttpMessageHandler
   {
      public List<string> Bodies { get; } = new();

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         string body = await request.Content!.ReadAsStringAsync().ConfigureAwait(false);
         Bodies.Add(body);
         if (body.Contains("\"enabled\":false"))
            return new HttpResponseMessage(HttpStatusCode.BadRequest) {
               Content = new StringContent("{\"error\":{\"message\":\"Reasoning is mandatory for this endpoint and cannot be disabled.\",\"code\":400}}")
            };

         return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"[ACTION] type: none [/ACTION]\"},\"finish_reason\":\"stop\"}]}")
         };
      }
   }

   /// <summary>GLM on an OpenAI-compatible endpoint: refuses reasoning_effort "none" in Chinese, accepts low/high/max.</summary>
   internal sealed class AlwaysThinksHandler : HttpMessageHandler
   {
      public List<string> Bodies { get; } = new();

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         string body = await request.Content!.ReadAsStringAsync().ConfigureAwait(false);
         Bodies.Add(body);
         if (!body.Contains("\"reasoning_effort\":\"low\""))
            return new HttpResponseMessage(HttpStatusCode.BadRequest) {
               Content = new StringContent("{\"error\":{\"code\":\"1210\",\"message\":\"\u8be5\u6a21\u578b\u59cb\u7ec8\u601d\u8003\"}}")
            };

         return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"a summary\"},\"finish_reason\":\"stop\"}]}")
         };
      }
   }

   [TestFixture]
   public class OpenRouterReasoningMandatoryTests
   {
      private static OpenRouterClient GlmClient(AlwaysThinksHandler handler)
         => new(new HttpClient(handler), new OpenRouterConfig {
            Model = "glm-5.3-flash", ReasoningProvider = () => "low", ReasoningAsEffortStringProvider = () => true
         });

      // STAKE (LapitaPolata, 19/10/2026, glm-5.3-flash): the refusal came in Chinese, the English match missed it, and
      // every memory compression and summary failed. Refused, the call goes again at "low", and lands.
      [Test]
      public async Task GIVEN_a_model_that_refuses_reasoning_off_in_its_own_language_WHEN_a_housekeeping_call_asks_for_it_off_THEN_it_goes_again_at_low()
      {
         var handler = new AlwaysThinksHandler();

         LlmResponse response = await GlmClient(handler).CompleteAsync(Interpreter());

         response.IsSuccess.Should().BeTrue();
         handler.Bodies.Should().HaveCount(2);
         handler.Bodies[1].Should().Contain("\"reasoning_effort\":\"low\"");
      }

      // STAKE: once refused, the endpoint is not asked to turn reasoning off again: every later background call would
      // otherwise pay a failed round trip first.
      [Test]
      public async Task GIVEN_an_endpoint_that_refused_once_WHEN_the_next_housekeeping_call_goes_THEN_it_goes_at_low_at_once()
      {
         var handler = new AlwaysThinksHandler();
         OpenRouterClient client = GlmClient(handler);

         await client.CompleteAsync(Interpreter());
         await client.CompleteAsync(Interpreter());

         handler.Bodies.Should().HaveCount(3);
         handler.Bodies[2].Should().Contain("\"reasoning_effort\":\"low\"");
      }

      private static LlmRequest Interpreter() => new() {
         SystemPrompt = "system",
         Messages = new[] {new LlmMessage(MessageRole.User, "read this reply")},
         Parameters = new LlmParameters {MaxTokens = 400, ReasoningOverride = "off"}
      };

      // Thoragoros1's case, on a model nobody listed: the refusal is answered by asking again at "low", and the reply lands.
      [Test]
      public async Task GIVEN_an_unlisted_model_whose_reasoning_cannot_be_disabled_WHEN_a_call_asks_for_it_off_THEN_it_is_asked_again_at_low_and_succeeds()
      {
         var handler = new ReasoningMandatoryHandler();
         var client = new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {Model = "vendor/always-thinks"});

         LlmResponse response = await client.CompleteAsync(Interpreter());

         response.IsSuccess.Should().BeTrue();
         handler.Bodies.Should().HaveCount(2);
         handler.Bodies[1].Should().Contain("\"effort\":\"low\"", "left to its default the model thinks past the budget");
      }

      // STAKE (cost bench 24/10/2026): Gemini 3.8 Flash as interpreter, left to its default after the refusal, thought
      // about 1,000 tokens per reading, overran the 900 budget and was asked twice: 20 s and three times Grok's price per
      // reading. A Gemini 3 model is known to refuse, so it is asked once, at "low", from its very first call.
      [Test]
      public async Task GIVEN_a_gemini_3_model_WHEN_a_call_asks_for_reasoning_off_THEN_it_is_asked_once_at_low()
      {
         var handler = new ReasoningMandatoryHandler();
         var client = new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {Model = "google/gemini-3.8-flash"});

         LlmResponse response = await client.CompleteAsync(Interpreter());

         response.IsSuccess.Should().BeTrue();
         handler.Bodies.Should().HaveCount(1, "no refusal is paid for first");
         handler.Bodies[0].Should().Contain("\"effort\":\"low\"");
      }

      // STAKE: a prose call keeps the player's own dial on Gemini; only a call that asked for reasoning OFF is moved.
      [Test]
      public async Task GIVEN_a_gemini_3_model_WHEN_a_call_carries_the_players_dial_THEN_the_dial_is_sent_unchanged()
      {
         var handler = new ReasoningMandatoryHandler();
         var client = new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {Model = "google/gemini-3.8-flash", ReasoningProvider = () => "medium"});

         await client.CompleteAsync(new LlmRequest {
            SystemPrompt = "system", Messages = new[] {new LlmMessage(MessageRole.User, "hello")}, Parameters = new LlmParameters {MaxTokens = 400}
         });

         handler.Bodies.Should().ContainSingle().Which.Should().Contain("\"effort\":\"medium\"");
      }

      // Every other model is asked once, as before: the second ask is only for that refusal.
      [Test]
      public async Task GIVEN_a_model_that_accepts_reasoning_off_WHEN_called_THEN_it_is_asked_once()
      {
         var handler = new ReasoningMandatoryHandler();
         var client = new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {Model = "test/model"});

         var request = new LlmRequest {
            SystemPrompt = "system", Messages = new[] {new LlmMessage(MessageRole.User, "hello")}, Parameters = new LlmParameters {MaxTokens = 400}
         };
         await client.CompleteAsync(request);

         handler.Bodies.Should().HaveCount(1);
      }
   }
}

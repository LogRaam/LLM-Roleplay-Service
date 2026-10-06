// Code written by Gabriel Mailhot, 06/10/2026.
// A model whose reasoning cannot be turned off. Thoragoros1 (Nexus, 06/10/2026), Gemini 3.7 Flash on 2.7.1: every
// interpreter call failed with HTTP 400 "Reasoning is mandatory for this endpoint and cannot be disabled", so no deed
// of any reply was recorded. The interpreter and every housekeeping call send reasoning OFF on purpose (it keeps them
// fast and within budget), and that model refuses the request outright. On that refusal the call is made once more
// without any reasoning field, leaving the model its own default. If this breaks, a whole family of models records
// nothing the characters do.

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

   [TestFixture]
   public class OpenRouterReasoningMandatoryTests
   {
      private static LlmRequest Interpreter() => new() {
         SystemPrompt = "system",
         Messages = new[] {new LlmMessage(MessageRole.User, "read this reply")},
         Parameters = new LlmParameters {MaxTokens = 400, ReasoningOverride = "off"}
      };

      // Thoragoros1's case: the refusal is answered by asking again with the model's own default, and the reply lands.
      [Test]
      public async Task GIVEN_a_model_whose_reasoning_cannot_be_disabled_WHEN_a_call_asks_for_it_off_THEN_it_is_asked_again_without_and_succeeds()
      {
         var handler = new ReasoningMandatoryHandler();
         var client = new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {Model = "google/gemini-3.7-flash"});

         LlmResponse response = await client.CompleteAsync(Interpreter());

         response.IsSuccess.Should().BeTrue();
         handler.Bodies.Should().HaveCount(2);
         handler.Bodies[1].Should().NotContain("\"reasoning\"", "the second ask leaves reasoning to the model");
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

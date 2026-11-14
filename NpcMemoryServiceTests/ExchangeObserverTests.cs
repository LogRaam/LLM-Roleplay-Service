// Code written by Gabriel Mailhot, 14/11/2026.
// What this pins: an exchange observer sees each finished call, the request as asked and the reply as it came back, and
// can never break the call. Gabriel (14/11/2026) wants a player's "report a mistake" to carry the prompt and the raw reply
// of the conversation, so a report is diagnosed the first time instead of after three requests for the right log. If this
// breaks, reports arrive without the very context they exist to carry, or a faulty observer costs a player their reply.

#region

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ExchangeObserverTests
   {
      private const string Reply = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Well met, traveller.\"},\"finish_reason\":\"stop\"}]}";

      // THE REQUEST: the report carries what the character was given and what the model answered.
      [Test]
      public async Task GIVEN_an_exchange_observer_WHEN_a_call_finishes_THEN_it_sees_the_prompt_and_the_reply()
      {
         var seen = new Recorder();
         OpenRouterClient client = Client();
         client.ExchangeObserver = seen;

         await client.CompleteAsync(Request());

         seen.Request!.SystemPrompt.Should().Be("You are Lady Arwa.");
         seen.Response!.Content.Should().Be("Well met, traveller.");
         seen.Model.Should().Be("test/model");
      }

      // STAKE: a broken observer never costs the player the reply.
      [Test]
      public async Task GIVEN_an_observer_that_throws_WHEN_a_call_finishes_THEN_the_reply_still_arrives()
      {
         OpenRouterClient client = Client();
         client.ExchangeObserver = new Thrower();

         LlmResponse response = await client.CompleteAsync(Request());

         response.Content.Should().Be("Well met, traveller.");
      }

      private static OpenRouterClient Client()
         => new(new HttpClient(new OneReply()), new OpenRouterConfig {Model = "test/model", IsOpenRouterProvider = () => false});

      private static LlmRequest Request()
         => new() {SystemPrompt = "You are Lady Arwa.", Messages = new[] {new LlmMessage(MessageRole.User, "Hello.")}, Parameters = new LlmParameters {MaxTokens = 100}};

      private sealed class Recorder : ILlmExchangeObserver
      {
         public LlmRequest? Request { get; private set; }
         public LlmResponse? Response { get; private set; }
         public string? Model { get; private set; }

         public void OnExchange(LlmRequest request, LlmResponse response, string model)
         {
            Request = request;
            Response = response;
            Model = model;
         }
      }

      private sealed class Thrower : ILlmExchangeObserver
      {
         public void OnExchange(LlmRequest request, LlmResponse response, string model) => throw new InvalidOperationException("observer fault");
      }

      private sealed class OneReply : HttpMessageHandler
      {
         protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent(Reply)});
      }
   }
}

// Code written by Gabriel Mailhot, 15/08/2026.
// Pins LlmParameters.AllowTruncationRetry: OpenRouterClient.CompleteAsync's one-shot retry on a truncated/empty/
// content-filtered reply (see that method's own XML doc) exists to give the model a second, bigger-budget roll.
// The mod's PROSE FALLBACK feature (Prose + Interpreter composition, CalradiaRemembers.Logic.
// TruncationFallbackPolicy) needs the OPPOSITE for its prose call: fail fast on the FIRST truncated reply so the
// mod's own fallback (redo the turn, once, on a reliable explicit model) can take over immediately, instead of
// paying for OpenRouterClient's slow double-budget retry first. AllowTruncationRetry=false is how a single
// request opts out of that retry. No fake-transport harness existed for OpenRouterClient's full CompleteAsync
// path yet (the other OpenRouterClient tests only exercise ToWireFormat/ParseResponse, no network), so this file
// adds a minimal counting HttpMessageHandler to prove the flag's effect on the real retry call count.

#region

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
   /// <summary>
   ///   A fake transport that always answers with a fixed "length"-truncated reply and counts how many times it
   ///   was invoked, so a test can prove whether CompleteAsync's retry fired without any real network call.
   /// </summary>
   internal sealed class TruncatedReplyHandler : HttpMessageHandler
   {
      public int CallCount { get; private set; }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         CallCount++;
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"He leans in and\"}," +
                             "\"finish_reason\":\"length\"}]}";

         var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(body)
         };

         return Task.FromResult(response);
      }
   }

   [TestFixture]
   public class OpenRouterTruncationRetryTests
   {
      private static LlmRequest BuildRequest(bool allowTruncationRetry)
         => new() {
            SystemPrompt = "system",
            Messages = new[] {new LlmMessage(MessageRole.User, "hello")},
            Parameters = new LlmParameters {MaxTokens = 400, AllowTruncationRetry = allowTruncationRetry}
         };

      // The whole point of the flag: the mod's PROSE call sets this false so a truncated reply fails FAST (one
      // HTTP call) instead of paying for the client's own bigger-budget retry, letting the mod's OWN fallback (a
      // redo on a reliable explicit model) take over immediately. If the retry fired anyway, the prose turn would
      // pay for TWO slow calls before the mod's fallback even started a third.
      [Test]
      public async Task GIVEN_AllowTruncationRetry_false_WHEN_the_reply_is_truncated_THEN_the_client_does_not_retry()
      {
         var handler = new TruncatedReplyHandler();
         var client = new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {Model = "test/model"});

         LlmResponse response = await client.CompleteAsync(BuildRequest(false));

         handler.CallCount.Should().Be(1);
         response.WasRetried.Should().BeFalse();
         response.FinishReason.Should().Be("length");
      }

      // The default (true) is the load-bearing guarantee that every OTHER caller (Integrated chat, the action
      // interpreter, the summarizers) keeps its existing safety net unchanged: a truncated reply still gets the
      // bigger-budget retry exactly as it did before this feature.
      [Test]
      public async Task GIVEN_AllowTruncationRetry_true_WHEN_the_reply_is_truncated_THEN_the_client_still_retries()
      {
         var handler = new TruncatedReplyHandler();
         var client = new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {Model = "test/model"});

         LlmResponse response = await client.CompleteAsync(BuildRequest(true));

         handler.CallCount.Should().Be(2);
         response.WasRetried.Should().BeTrue();
      }
   }
}

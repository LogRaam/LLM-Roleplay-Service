// Code written by Gabriel Mailhot, 24/10/2026.
// What a call cost, as the provider billed it. fearente (Nexus, 24/10/2026): "I've already spent around $3.19 without
// particularly heavy usage" in a week the mod's page priced at about $1, with no way to see which calls were the mod's
// or where the money went. These pin the three things the bill depends on: the client asks OpenRouter for the cost and
// names the mod on its activity page (and never sends either to another endpoint); it reads the cost and the hidden
// reasoning tokens back; and a retry is paid for as well as the attempt it replaced, so a total never undercounts.

#region

using System.Collections.Generic;
using System.Linq;
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
   public class OpenRouterUsageAccountingTests
   {
      private const string Truncated = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"He leans in and\"},\"finish_reason\":\"length\"}]," +
                                       "\"usage\":{\"prompt_tokens\":1000,\"completion_tokens\":400,\"cost\":0.004," +
                                       "\"completion_tokens_details\":{\"reasoning_tokens\":350}}}";

      private const string Complete = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"He leans in and smiles.\"},\"finish_reason\":\"stop\"}]," +
                                      "\"usage\":{\"prompt_tokens\":1000,\"completion_tokens\":600,\"cost\":0.006," +
                                      "\"prompt_tokens_details\":{\"cached_tokens\":800,\"cache_write_tokens\":0}," +
                                      "\"completion_tokens_details\":{\"reasoning_tokens\":500}}}";

      // A model that thinks bills its thinking: fearente's Gemini spent tokens he never saw. The cost and the hidden
      // reasoning must come back with the answer, or no measurement can explain a bill.
      [Test]
      public async Task GIVEN_openrouter_bills_a_call_WHEN_the_answer_arrives_THEN_its_cost_and_hidden_reasoning_are_read()
      {
         var handler = new ScriptedHandler(Complete);
         OpenRouterClient client = Client(handler, openRouter: true);

         LlmResponse response = await client.CompleteAsync(Request());

         response.Usage!.Cost.Should().BeApproximately(0.006, 1e-9);
         response.Usage.ReasoningTokens.Should().Be(500);
         response.Usage.CachedPromptTokens.Should().Be(800);
      }

      // A truncated reply retried with a bigger budget is billed twice. Reporting the retry alone made every total
      // the mod could compute lower than the invoice.
      [Test]
      public async Task GIVEN_a_reply_cut_short_and_retried_WHEN_the_bill_is_read_THEN_both_attempts_are_counted()
      {
         var handler = new ScriptedHandler(Truncated, Complete);
         OpenRouterClient client = Client(handler, openRouter: true);
         var seen = new AttemptLog();
         client.UsageObserver = seen;

         LlmResponse response = await client.CompleteAsync(Request());

         response.Usage!.Cost.Should().BeApproximately(0.010, 1e-9);
         response.Usage.PromptTokens.Should().Be(2000);
         response.Usage.ReasoningTokens.Should().Be(850);
         seen.Attempts.Should().HaveCount(2);
         seen.Attempts.All(a => a == "test/model").Should().BeTrue();
      }

      // On OpenRouter the player's activity page names the mod beside each call, and each answer carries its price.
      [Test]
      public async Task GIVEN_a_call_to_openrouter_WHEN_it_is_sent_THEN_it_asks_for_the_bill_and_names_the_mod()
      {
         var handler = new ScriptedHandler(Complete);

         await Client(handler, openRouter: true).CompleteAsync(Request());

         handler.Bodies.Single().Should().Contain("\"usage\":{\"include\":true}");
         handler.Titles.Single().Should().Be("Calradia Remembers");
         handler.Referers.Single().Should().Be("https://www.nexusmods.com/mountandblade2bannerlord/mods/11658");
      }

      // A local server or another aggregator may refuse a field it does not know: nothing new reaches it.
      [Test]
      public async Task GIVEN_a_call_to_another_endpoint_WHEN_it_is_sent_THEN_its_payload_and_headers_are_unchanged()
      {
         var handler = new ScriptedHandler(Complete);

         await Client(handler, openRouter: false).CompleteAsync(Request());

         handler.Bodies.Single().Should().NotContain("\"usage\"");
         handler.Titles.Single().Should().BeNull();
         handler.Referers.Single().Should().BeNull();
      }

      private static OpenRouterClient Client(HttpMessageHandler handler, bool openRouter)
         => new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {
            Model = "test/model", IsOpenRouterProvider = () => openRouter, AppTitle = "Calradia Remembers",
            AppUrl = "https://www.nexusmods.com/mountandblade2bannerlord/mods/11658"
         });

      private static LlmRequest Request()
         => new() {
            SystemPrompt = "system",
            Messages = new[] {new LlmMessage(MessageRole.User, "hello")},
            Parameters = new LlmParameters {MaxTokens = 400}
         };

      private sealed class AttemptLog : ILlmUsageObserver
      {
         public List<string?> Attempts { get; } = new List<string?>();

         public void OnAttempt(string? model, LlmResponse response) => Attempts.Add(model);
      }

      private sealed class ScriptedHandler : HttpMessageHandler
      {
         private readonly Queue<string> _replies;

         public ScriptedHandler(params string[] replies) => _replies = new Queue<string>(replies);

         public List<string> Bodies { get; } = new List<string>();
         public List<string?> Titles { get; } = new List<string?>();
         public List<string?> Referers { get; } = new List<string?>();

         protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
         {
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            Titles.Add(request.Headers.TryGetValues("X-Title", out IEnumerable<string>? t) ? t.Single() : null);
            Referers.Add(request.Headers.TryGetValues("HTTP-Referer", out IEnumerable<string>? r) ? r.Single() : null);

            return new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent(_replies.Dequeue())};
         }
      }
   }
}

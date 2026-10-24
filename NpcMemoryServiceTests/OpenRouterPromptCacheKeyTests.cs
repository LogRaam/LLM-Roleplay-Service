// Code written by Gabriel Mailhot, 24/10/2026.
// Which server holds a prompt's cache. The cost bench (24/10/2026) found Grok re-reading the full 16,000-token prompt at
// full price on the first reply after the scene opener, and on the first reply after a long talk was folded, although
// the prompt's stable prefix had not changed by one character: Grok keeps a cache on one server and finds it again only
// when the request says where. A probe the same day: the first reply after the opener read 128 cached tokens in 3 tries
// out of 3 ($0.020); with a prompt_cache_key, 16,256 in 3 out of 3 ($0.0035). These pin that the key is sent, that it
// follows the stable prefix and nothing else, and that it never goes to an endpoint other than OpenRouter. If this
// breaks, every Grok conversation pays its opening reply about six times over.

#region

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class OpenRouterPromptCacheKeyTests
   {
      private const string Stable = "You are Lucon, Emperor of Calradia.\n";

      // STAKE: the opener and the first reply share the prefix but not the messages; the same key is what sends the
      // reply to the server that cached the opener.
      [Test]
      public async Task GIVEN_two_turns_with_the_same_stable_prefix_WHEN_sent_THEN_they_carry_the_same_key()
      {
         var handler = new RecordingHandler();
         OpenRouterClient client = Client(handler, openRouter: true);

         await client.CompleteAsync(Request(Stable, "CURRENT ENCOUNTER: day 3", "[set the scene]"));
         await client.CompleteAsync(Request(Stable, "CURRENT ENCOUNTER: day 4", "Well met."));

         string?[] keys = handler.Bodies.Select(KeyOf).ToArray();
         keys[0].Should().NotBeNullOrEmpty();
         keys[1].Should().Be(keys[0]);
      }

      // STAKE: two characters on one model must not share a key, or one would be routed to the other's server for nothing.
      [Test]
      public async Task GIVEN_two_characters_WHEN_sent_THEN_their_keys_differ()
      {
         var handler = new RecordingHandler();
         OpenRouterClient client = Client(handler, openRouter: true);

         await client.CompleteAsync(Request(Stable, "CURRENT ENCOUNTER: day 3", "hello"));
         await client.CompleteAsync(Request("You are Rhagaea, Empress of the south.\n", "CURRENT ENCOUNTER: day 3", "hello"));

         KeyOf(handler.Bodies[1]).Should().NotBe(KeyOf(handler.Bodies[0]));
      }

      // STAKE: a prompt without a stable prefix has nothing worth routing to; a key built from the whole prompt would
      // change every turn and say nothing.
      [Test]
      public async Task GIVEN_a_request_without_a_stable_prefix_WHEN_sent_THEN_it_carries_no_key()
      {
         var handler = new RecordingHandler();

         await Client(handler, openRouter: true).CompleteAsync(new LlmRequest {
            SystemPrompt = "system", Messages = new[] {new LlmMessage(MessageRole.User, "hello")}, Parameters = new LlmParameters {MaxTokens = 400}
         });

         KeyOf(handler.Bodies.Single()).Should().BeNull();
      }

      // STAKE: another OpenAI-compatible server (a local Ollama, NanoGPT) may refuse a field it does not know.
      [Test]
      public async Task GIVEN_an_endpoint_other_than_openrouter_WHEN_sent_THEN_it_carries_no_key()
      {
         var handler = new RecordingHandler();

         await Client(handler, openRouter: false).CompleteAsync(Request(Stable, "CURRENT ENCOUNTER: day 3", "hello"));

         KeyOf(handler.Bodies.Single()).Should().BeNull();
      }

      // STAKE: the key names the prompt without carrying it; a prompt's text never travels twice.
      [Test]
      public void GIVEN_a_stable_prefix_WHEN_named_THEN_the_name_is_short_and_carries_none_of_its_text()
      {
         string? key = OpenRouterClient.PromptCacheKey(Stable);

         key.Should().MatchRegex("^cr-[0-9a-f]{16}$");
      }

      private static string? KeyOf(string body) => (string?) JObject.Parse(body)["prompt_cache_key"];

      private static OpenRouterClient Client(HttpMessageHandler handler, bool openRouter)
         => new OpenRouterClient(new HttpClient(handler), new OpenRouterConfig {Model = "x-ai/grok-4.20", IsOpenRouterProvider = () => openRouter});

      private static LlmRequest Request(string stable, string tail, string message)
         => new() {
            SystemPrompt = stable + tail,
            StableSystemPrompt = stable,
            Messages = new[] {new LlmMessage(MessageRole.User, message)},
            Parameters = new LlmParameters {MaxTokens = 400}
         };

      private sealed class RecordingHandler : HttpMessageHandler
      {
         public List<string> Bodies { get; } = new List<string>();

         protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
         {
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(HttpStatusCode.OK) {
               Content = new StringContent("{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Well met.\"},\"finish_reason\":\"stop\"}]}")
            };
         }
      }
   }
}

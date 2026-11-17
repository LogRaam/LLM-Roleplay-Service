// Code written by Gabriel Mailhot, 17/11/2026.
// VonLemberg (Discord, 17/11/2026, RFC "Independent LLM Routes"): another mod sends a request through the player's CR
// connection but aimed at a model of its own (LlmRequest.ModelOverride, exposed as CrLlmRequest.Model). The player's
// provider pin was chosen for the player's model; sent along with another model it can name providers that do not host
// it, and with fallbacks off (the default) every such call fails. If this breaks one way, the other mod's calls fail for a
// reason nobody can see; the other way, the player's own conversations lose the pin they set against moderated providers.
// Only a request that asks (OmitProviderPin, set by CrLlm) drops the pin: the mod's own override of the model (the prose
// fallback, ProseFallbackComposer) is still the player's conversation and keeps it.

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
   public class OpenRouterModelOverridePinTests
   {
      private const string PlayerModel = "z-ai/glm-5.2";

      // STAKE: a diplomacy call aimed at another model fails on providers that never hosted it.
      [Test]
      public async Task GIVEN_a_pin_WHEN_another_mods_request_names_its_own_model_THEN_that_model_is_sent_without_the_pin()
      {
         JObject body = await Send(PlayerModel.Replace("glm-5.2", "glm-4.6"), omitPin: true);

         ((string?) body["model"]).Should().Be("z-ai/glm-4.6");
         body["provider"].Should().BeNull();
      }

      // STAKE: the player's own conversation loses the pin that keeps moderated providers away.
      [Test]
      public async Task GIVEN_a_pin_WHEN_a_request_names_no_model_THEN_the_pin_is_kept()
      {
         JObject body = await Send(null);

         ((string?) body["model"]).Should().Be(PlayerModel);
         body["provider"]!["order"]!.Values<string>().Should().Equal("deepinfra");
      }

      // STAKE: the prose fallback model is still the player's conversation: it keeps the pin against moderated providers.
      [Test]
      public async Task GIVEN_a_pin_WHEN_the_mod_itself_overrides_the_model_THEN_the_pin_is_kept()
      {
         JObject body = await Send("deepseek/deepseek-v4-flash");

         ((string?) body["model"]).Should().Be("deepseek/deepseek-v4-flash");
         body["provider"].Should().NotBeNull();
      }

      private static async Task<JObject> Send(string? model, bool omitPin = false)
      {
         var handler = new RecordingHandler();
         var client = new OpenRouterClient(new HttpClient(handler),
            new OpenRouterConfig {Model = PlayerModel, ProviderSlugsProvider = () => "deepinfra", IsOpenRouterProvider = () => true});

         await client.CompleteAsync(new LlmRequest {
            SystemPrompt = "system",
            Messages = new[] {new LlmMessage(MessageRole.User, "hello")},
            Parameters = new LlmParameters {MaxTokens = 400},
            ModelOverride = model,
            OmitProviderPin = omitPin
         });

         return JObject.Parse(handler.Bodies.Single());
      }

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

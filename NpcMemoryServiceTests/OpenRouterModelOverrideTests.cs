// Code written by Gabriel Mailhot, 13/08/2026.
// Pins how LlmRequest.ModelOverride reaches the OpenRouter wire body. This is what lets the PROSE+EXTRACT
// mode point ONE call (the extractor) at a cheaper model without touching any global model selection, so
// there is no async race with the shared model-mode setting. If the override stops being honoured, every
// extractor call would silently bill the scene's own (often expensive) model instead of the configured one;
// if a blank override stopped degrading to the resolved model, an unconfigured extractor would send an empty
// model id and the request would fail outright.

#region

using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class OpenRouterModelOverrideTests
   {
      private static OpenRouterClient BuildClient(string resolvedModel)
         => new(new HttpClient(), new OpenRouterConfig {Model = resolvedModel});

      private static LlmRequest BuildRequest(string? modelOverride)
         => new() {
            SystemPrompt = "extract the tags",
            Messages = new[] {new LlmMessage(MessageRole.User, "Output the tags now.")},
            ModelOverride = modelOverride
         };

      private static string ModelOf(object wireBody)
         => (string) ((Dictionary<string, object>) wireBody)["model"];

      // The whole point of the property: a set override is the model this one call targets, so the extractor
      // can run on a cheap model while the scene keeps its own expensive one.
      [Test]
      public void GIVEN_a_non_blank_model_override_WHEN_building_the_body_THEN_the_override_is_the_model_sent()
      {
         object body = BuildClient("scene/expensive-model").ToWireFormat(BuildRequest("extractor/cheap-model"));

         ModelOf(body).Should().Be("extractor/cheap-model");
      }

      // Blank ExtractorModel in MCM means "reuse the scene's model": a blank override must NOT blank out the
      // model id, it must fall back to the client's normally-resolved model exactly as before this feature.
      [Test]
      public void GIVEN_a_blank_model_override_WHEN_building_the_body_THEN_the_resolved_model_is_sent()
      {
         object body = BuildClient("scene/model").ToWireFormat(BuildRequest("   "));

         ModelOf(body).Should().Be("scene/model");
      }

      // The default (no override supplied) is the load-bearing guarantee that every existing, non-extractor
      // request keeps sending the exact model it did before: null must behave identically to blank.
      [Test]
      public void GIVEN_no_model_override_WHEN_building_the_body_THEN_the_resolved_model_is_sent_unchanged()
      {
         object body = BuildClient("scene/model").ToWireFormat(BuildRequest(null));

         ModelOf(body).Should().Be("scene/model");
      }
   }
}

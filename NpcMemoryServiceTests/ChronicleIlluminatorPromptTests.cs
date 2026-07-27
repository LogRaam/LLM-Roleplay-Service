// Code written by Gabriel Mailhot, 27/07/2026.
// Player report (Nexus, Santiago755, 2026-07-26): with Reply Language forced to Spanish, chat, letters, and
// memory came out Spanish, but the illuminated chronicle stayed English. Root cause: the chronicle is a
// FOURTH LLM output and its prompt builder was never told the language. These tests pin that, when the host
// passes a language, the system prompt carries an unambiguous directive to write the whole chronicle in it,
// and that a blank language leaves the prompt in its default (English) voice, so the setting cannot silently
// impose a language the player never asked for.

#region

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ChronicleIlluminatorPromptTests
   {
      // The load-bearing fix: without an explicit language line, the chronicler writes in English regardless of
      // the player's forced Reply Language. This pins the directive so a Spanish player gets a Spanish chronicle.
      [Test]
      public async Task GIVEN_a_language_WHEN_the_chronicle_prompt_is_built_THEN_it_orders_the_whole_chronicle_in_that_language()
      {
         var capturingClient = new CapturingLlmClient();
         var illuminator = new ChronicleIlluminator(capturingClient);

         await illuminator.IlluminateAsync("Derthert", "In the spring of the first year: won a great battle.", "", null, "Spanish");

         capturingClient.LastRequest.Should().NotBeNull();
         capturingClient.LastRequest!.SystemPrompt.Should().Contain("Write the ENTIRE chronicle in Spanish");
      }

      // The other half of the contract: a blank language (the default, auto-detect) must NOT inject any language
      // order, or the chronicle would be pinned to some tongue the player never chose. English stays the default.
      [Test]
      public async Task GIVEN_no_language_WHEN_the_chronicle_prompt_is_built_THEN_no_language_order_is_injected()
      {
         var capturingClient = new CapturingLlmClient();
         var illuminator = new ChronicleIlluminator(capturingClient);

         await illuminator.IlluminateAsync("Derthert", "In the spring of the first year: won a great battle.", "", null, null);

         capturingClient.LastRequest.Should().NotBeNull();
         capturingClient.LastRequest!.SystemPrompt.Should().NotContain("Write the ENTIRE chronicle in");
      }

      #region private

      private sealed class CapturingLlmClient : ILlmClient
      {
         public LlmRequest? LastRequest { get; private set; }

         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
         {
            LastRequest = request;
            return Task.FromResult(new LlmResponse {Content = "Y en la primavera del primer ano, gano una gran batalla.", IsSuccess = true});
         }
      }

      #endregion
   }
}

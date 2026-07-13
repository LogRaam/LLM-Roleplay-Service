// Code written by Gabriel Mailhot, 13/07/2026.

#region

using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Pins OpenRouter provider routing: the player can force the request through providers they trust, because
   ///   several of OpenRouter's providers moderate their own OUTPUT and stop generating the moment profanity
   ///   appears, so the reply arrives cut mid-sentence and reads as the mod censoring them (player request).
   ///   The pin is strictly opt-in: with nothing pinned, the request body must be exactly what it always was,
   ///   so NanoGPT and local OpenAI-compatible servers are never sent a field they do not understand.
   /// </summary>
   [TestFixture]
   public class ProviderRoutingTests
   {
      [Test]
      public void GIVEN_no_pin_at_all_WHEN_resolved_THEN_no_providers_are_pinned_and_the_field_is_omitted()
      {
         ProviderRouting.ParseSlugs(null).Should().BeEmpty();
         ProviderRouting.ParseSlugs("").Should().BeEmpty();
         ProviderRouting.ParseSlugs("   ").Should().BeEmpty();
         ProviderRouting.ParseSlugs(" , ,, ").Should().BeEmpty(); // a player who typed only separators
      }

      [Test]
      public void GIVEN_a_config_with_no_pin_provider_WHEN_resolved_THEN_it_defaults_to_open_routing_without_fallback_flags()
      {
         var config = new OpenRouterConfig();

         config.ResolveProviderSlugs().Should().BeEmpty();
         config.ResolveAllowProviderFallbacks().Should().BeFalse(); // pinning, when it happens, forces by default
      }

      [Test]
      public void GIVEN_a_single_pinned_provider_WHEN_parsed_THEN_it_is_the_only_one_the_request_may_use()
      {
         ProviderRouting.ParseSlugs("deepinfra").Should().Equal("deepinfra");
      }

      [Test]
      public void GIVEN_several_providers_WHEN_parsed_THEN_the_players_written_order_is_preserved_as_the_preference()
      {
         ProviderRouting.ParseSlugs("deepinfra, together, novita")
            .Should().Equal("deepinfra", "together", "novita");
      }

      [Test]
      public void GIVEN_untidy_spacing_and_empty_entries_WHEN_parsed_THEN_they_are_cleaned_away()
      {
         ProviderRouting.ParseSlugs("  deepinfra ,, ,  together  ")
            .Should().Equal("deepinfra", "together");
      }

      [Test]
      public void GIVEN_the_same_provider_twice_WHEN_parsed_THEN_it_is_listed_once()
      {
         ProviderRouting.ParseSlugs("deepinfra, DeepInfra, together")
            .Should().Equal("deepinfra", "together");
      }

      [Test]
      public void GIVEN_a_live_pin_provider_WHEN_resolved_THEN_the_config_reads_it_on_every_request()
      {
         var pin = "together";
         var config = new OpenRouterConfig {
            ProviderSlugsProvider = () => pin,
            AllowProviderFallbacksProvider = () => true
         };

         config.ResolveProviderSlugs().Should().Equal("together");
         config.ResolveAllowProviderFallbacks().Should().BeTrue();

         pin = "deepinfra"; // the player edits the MCM field mid-session: it applies at once, no restart
         config.ResolveProviderSlugs().Should().Equal("deepinfra");
      }
   }
}

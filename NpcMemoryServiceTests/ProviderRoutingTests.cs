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
      // The opt-in guarantee itself: every shape of "nothing typed" (null, empty, blank, or a string
      // of stray commas) must yield an empty slug list, never throw, so a player who never touches
      // the field keeps OpenRouter's own routing exactly as it always was.
      [Test]
      public void GIVEN_no_pin_at_all_WHEN_resolved_THEN_no_providers_are_pinned_and_the_field_is_omitted()
      {
         ProviderRouting.ParseSlugs(null).Should().BeEmpty();
         ProviderRouting.ParseSlugs("").Should().BeEmpty();
         ProviderRouting.ParseSlugs("   ").Should().BeEmpty();
         ProviderRouting.ParseSlugs(" , ,, ").Should().BeEmpty(); // a player who typed only separators
      }

      // A config wired with no live resolver at all (the common case: most hosts never set one) must
      // default to the same "leave routing alone" behaviour, and AllowProviderFallbacks must default
      // to false so that a pin, WHEN one is later set, is a hard restriction by default: soft
      // fallbacks would let OpenRouter silently drift back to a moderated provider, defeating the
      // whole point of "force a specific provider" the player asked for.
      [Test]
      public void GIVEN_a_config_with_no_pin_provider_WHEN_resolved_THEN_it_defaults_to_open_routing_without_fallback_flags()
      {
         var config = new OpenRouterConfig();

         config.ResolveProviderSlugs().Should().BeEmpty();
         config.ResolveAllowProviderFallbacks().Should().BeFalse(); // pinning, when it happens, forces by default
      }

      // The baseline case a player actually hits: type the one provider they trust and get back
      // exactly that single slug, unmodified.
      [Test]
      public void GIVEN_a_single_pinned_provider_WHEN_parsed_THEN_it_is_the_only_one_the_request_may_use()
      {
         ProviderRouting.ParseSlugs("deepinfra").Should().Equal("deepinfra");
      }

      // The slug array becomes OpenRouter's provider.order verbatim, so the player's own typed order
      // IS their trust ranking. Silently reordering it (e.g. alphabetizing) would send requests to a
      // provider the player ranked lower first, defeating the reason they pinned anything at all.
      [Test]
      public void GIVEN_several_providers_WHEN_parsed_THEN_the_players_written_order_is_preserved_as_the_preference()
      {
         ProviderRouting.ParseSlugs("deepinfra, together, novita")
            .Should().Equal("deepinfra", "together", "novita");
      }

      // Players paste comma lists carelessly (stray spaces, doubled commas). An uncleaned blank
      // entry reaching OpenRouter's provider.order could be rejected as an invalid slug or simply
      // waste a routing slot, so the parser must clean it away before it ever leaves this method.
      [Test]
      public void GIVEN_untidy_spacing_and_empty_entries_WHEN_parsed_THEN_they_are_cleaned_away()
      {
         ProviderRouting.ParseSlugs("  deepinfra ,, ,  together  ")
            .Should().Equal("deepinfra", "together");
      }

      // De-duplication is case-insensitive: a player who typed "deepinfra" and later "DeepInfra"
      // meant the same provider twice, not two entries, and a duplicated slug in provider.order is
      // at best a wasted entry.
      [Test]
      public void GIVEN_the_same_provider_twice_WHEN_parsed_THEN_it_is_listed_once()
      {
         ProviderRouting.ParseSlugs("deepinfra, DeepInfra, together")
            .Should().Equal("deepinfra", "together");
      }

      // The pin is a live Func resolver, not a value snapshotted at startup: a player who edits the
      // "Pin Providers" MCM field mid-session must see the very next request honour the new pin, with
      // no restart, matching the mod's other live-config settings.
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

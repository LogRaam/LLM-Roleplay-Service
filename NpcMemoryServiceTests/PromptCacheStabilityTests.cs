// Code written by Gabriel Mailhot, 12/09/2026.
// What these pin: that the clock moving does not cost the player money.
//
// Gabriel asked whether the pack architecture could optimise the LLM prompt cache further. Measuring first
// found the opposite: the architecture had just made it WORSE. On a 37,267-character prompt, changing only
// the hour and the weather cost 10,018 characters of cacheable prefix - 27% - because here_and_now rendered
// up in the identity block, so every section below it was invalidated along with it. Providers with
// prefix-matching caches (xAI Grok, Anthropic, OpenAI) charge full price for all of it, and a local model
// re-processes it.
//
// PromptBuilder's own seam comment had named "time of day" as belonging BELOW the encounter marker since long
// before any of this. The packs simply had no way to say which of them was which, so the fix was to let them
// declare it (PackVolatility) rather than to remember where each one goes.
//
// These tests exist because that regression was invisible: it broke nothing, failed nothing, and would have
// shown up only as a bill. If they fail, a stable section has drifted below a volatile one and the prefix is
// being thrown away every time the sun moves.

#region

using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PromptCacheStabilityTests
   {
      private static string Build(int hour, Weather weather, string place)
         => new PromptBuilder {
               ActionVocabulary = GameActionCatalog.All
                                                   .Select(s => new GameActionDefinition {
                                                      Type = s.Type, Description = s.Description
                                                   })
                                                   .ToList()
            }
            .BuildSystemPrompt(
               new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc", Age = 40},
               new WorldState {CurrentDay = 10},
               new EncounterContext {
                  Bearer = new KnowledgeBearer {IsLord = true, HasHouse = true},
                  HouseStanding = new HouseStandingFacts {
                     HouseName = "dey Meroc", Fiefs = new List<string> {"Sargot"}
                  },
                  HereAndNow = new HereAndNowFacts {
                     Season = Season.Winter, HourOfDay = hour, Weather = weather,
                     NearestPlace = place, RealmName = "Vlandia"
                  },

                  // A real conversation always has an encounter description, and therefore always emits the
                  // marker NpcChatService splits the cacheable prefix on. A test without one would measure a
                  // prompt no player is ever sent - which is the mistake the budget guard was making.
                  Scene = SceneType.Outdoor
               });

      private static string CommonPrefix(string a, string b)
      {
         var i = 0;
         while (i < a.Length && i < b.Length && a[i] == b[i]) i++;

         return a.Substring(0, i);
      }

      // THE REGRESSION, as a player's bill would have shown it. Everything about who this character is and what
      // his house holds must survive the clock moving; only the weather itself may fall outside.
      [Test]
      public void GIVEN_only_the_hour_and_the_sky_have_changed_WHEN_the_prompt_is_rebuilt_THEN_the_stable_part_is_unchanged()
      {
         string shared = CommonPrefix(Build(9, Weather.Clear, "Sargot"),
                                      Build(15, Weather.Snow, "Sargot"));

         shared.Should().Contain("YOU ARE TEST LORD");
         shared.Should().Contain("Your house holds Sargot");
      }

      // Moving across the map is the other thing that changes constantly, and it lives in the same pack.
      [Test]
      public void GIVEN_the_party_has_moved_WHEN_the_prompt_is_rebuilt_THEN_who_he_is_still_survives_it()
      {
         string shared = CommonPrefix(Build(9, Weather.Clear, "Sargot"),
                                      Build(9, Weather.Clear, "Pravend"));

         shared.Should().Contain("YOU ARE TEST LORD");
         shared.Should().Contain("Your house holds Sargot");
      }

      // And the direct statement of the rule, so a future reordering cannot quietly undo it: the volatile
      // knowledge sits BELOW everything stable, not merely somewhere the current test data tolerates.
      [Test]
      public void GIVEN_the_built_prompt_WHEN_read_THEN_the_weather_is_below_the_house_and_not_above_it()
      {
         string prompt = Build(2, Weather.Blizzard, "Sargot");

         prompt.Should().Contain("blizzard");
         prompt.IndexOf("blizzard").Should().BeGreaterThan(prompt.IndexOf("Your house holds Sargot"));
         prompt.IndexOf("blizzard").Should().BeGreaterThan(prompt.IndexOf("YOU ARE TEST LORD"));
      }

      // THE DECISIVE ONE, and the reason the fix is a fix rather than a shuffle: everything that changes must
      // fall BELOW the literal marker NpcChatService splits the cacheable prefix on. Above it, one changed
      // character costs the whole prefix; below it, nothing was ever cached anyway.
      [Test]
      public void GIVEN_the_prompt_WHEN_the_clock_moves_THEN_nothing_above_the_cache_split_marker_changes()
      {
         string morning = Build(9, Weather.Clear, "Sargot");
         string night = Build(2, Weather.Blizzard, "Sargot");

         int marker = morning.IndexOf(PromptBuilder.EncounterSectionHeading);
         marker.Should().BeGreaterThan(0, "a real encounter always emits the marker");

         CommonPrefix(morning, night).Length.Should().BeGreaterThanOrEqualTo(marker);
      }

      // The declaration that makes it work, asserted so nobody has to remember it. A pack is stable by default
      // because that is the cheap mistake: a stable pack wrongly marked volatile only moves down the prompt,
      // while a volatile one left in the prefix silently costs every later section its cache.
      [Test]
      public void GIVEN_the_registry_WHEN_read_THEN_exactly_the_packs_that_change_under_you_say_so()
      {
         NpcKnowledgeFactory.All.Where(p => p.Volatility == PackVolatility.PerEncounter)
                            .Select(p => p.Name)
                            .Should().BeEquivalentTo(new[] {"here_and_now", "realm_news"});
      }

      // The cost must not come back through the other door either: moving a pack below the marker must not
      // have dropped it. What was gained in cache would then have been paid for in knowledge.
      [Test]
      public void GIVEN_the_volatile_pack_moved_WHEN_the_prompt_is_built_THEN_the_character_is_still_told_all_of_it()
      {
         string prompt = Build(2, Weather.Blizzard, "Sargot");

         prompt.Should().Contain("winter night").And.Contain("blizzard").And.Contain("Sargot");
      }
   }
}

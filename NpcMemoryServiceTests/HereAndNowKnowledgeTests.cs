// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 3. What these pin: that a character knows what season it is, what time it
// is, and what the sky is doing - and speaks as though he were standing in it.
//
// Why it exists. Gabriel, asked what other categories of knowledge there might be: "la saison actuelle,
// sommes-nous le jour ou la nuit, ce sont des détails d'environnement qui comptent." The audit's first pass
// found the gap was total - EncounterContext returned ZERO matches for season, night, weather and terrain - so
// a character would wish you a fine morning at midnight in a blizzard and nothing anywhere would object.
//
// If these fail, either the weather has stopped reaching the room, or it has started taking up more of the
// prompt than the room deserves.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;
using System.Text;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class HereAndNowKnowledgeTests
   {
      private static readonly HereAndNowPack Pack = new();

      private static string Render(HereAndNowFacts now, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {HereAndNow = now}, lean);

         return sb.ToString();
      }

      // THE GAP, stated as the thing a player would notice: the hour and the season reach the character.
      [Test]
      public void GIVEN_a_winter_night_WHEN_rendered_THEN_he_is_standing_in_a_winter_night()
      {
         string said = Render(new HereAndNowFacts {Season = Season.Winter, HourOfDay = 1, IsNight = true});

         said.Should().Contain("winter").And.Contain("night");
      }

      // The part of day is read as a person places themselves, not as a clock: "a summer afternoon", never
      // "15:00". A character who quotes the hour is a character who has read a status bar.
      [Test]
      public void GIVEN_the_middle_of_a_summer_afternoon_WHEN_rendered_THEN_it_reads_as_a_person_would_say_it()
      {
         string said = Render(new HereAndNowFacts {Season = Season.Summer, HourOfDay = 15});

         said.Should().Contain("summer afternoon");
         said.Should().NotContain("15");
      }

      // The engine's own night flag wins over any threshold guessed from the hour, because the engine knows
      // when the sun is down in a way arithmetic here does not.
      [Test]
      public void GIVEN_the_engine_calls_it_night_WHEN_rendered_THEN_that_wins_over_the_hour()
      {
         Render(new HereAndNowFacts {Season = Season.Spring, HourOfDay = 17, IsNight = true})
            .Should().Contain("night");
      }

      // Weather is what makes the difference between knowing the date and standing outside.
      [Test]
      public void GIVEN_snow_or_a_storm_WHEN_rendered_THEN_the_sky_is_part_of_the_scene()
      {
         Render(new HereAndNowFacts {Season = Season.Winter, Weather = Weather.Snow}).Should().Contain("snow");
         Render(new HereAndNowFacts {Season = Season.Autumn, Weather = Weather.Storm}).Should().Contain("storm");
         Render(new HereAndNowFacts {Season = Season.Spring, Weather = Weather.Blizzard}).Should().Contain("blizzard");
      }

      // Clear weather earns NO clause. A sentence about the absence of rain is a sentence wasted, and this pack
      // is read in every single conversation in the game.
      [Test]
      public void GIVEN_a_clear_sky_WHEN_rendered_THEN_no_words_are_spent_saying_it_is_not_raining()
      {
         string said = Render(new HereAndNowFacts {Season = Season.Summer, HourOfDay = 10, Weather = Weather.Clear});

         said.Should().Contain("summer morning");
         said.ToLowerInvariant().Should().NotContain("clear").And.NotContain("rain");
      }

      // The instruction that earns the pack its keep: it must be the room, not the subject. Both halves matter,
      // and the second is the one that stops every conversation opening with a weather report.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_rendered_THEN_he_is_told_not_to_contradict_it_and_not_to_open_with_it()
      {
         string said = Render(new HereAndNowFacts {Season = Season.Winter, HourOfDay = 2, IsNight = true});

         said.Should().Contain("never contradict");
         said.Should().Contain("Do not open with the weather");
      }

      // And Lean keeps the fact while dropping the coaching, the same discipline every pack holds. This one is
      // in EVERY prompt, so its Compact form has to be a single short sentence.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_it_is_one_short_sentence_and_no_more()
      {
         string lean = Render(new HereAndNowFacts {Season = Season.Winter, HourOfDay = 2, IsNight = true}, true);

         lean.Should().Contain("winter night");
         lean.Trim().Should().NotContain("\n");
         lean.Length.Should().BeLessThan(60);
      }

      // ── where he is, which is the half that was missing on the open map ──

      // Gabriel, 2026-09-12: "Il faudrait que le NPC ait conscience de où il se trouve sur la carte Monde."
      // CurrentLocationNote and GeographyNote both go null the moment the party leaves a settlement, which is
      // most of the game, so out in the country nobody knew where they were standing.
      [Test]
      public void GIVEN_open_country_WHEN_rendered_THEN_he_knows_what_he_is_near_and_whose_land_it_is()
      {
         string said = Render(new HereAndNowFacts {
            Season = Season.Autumn, NearestPlace = "Pravend",
            BearingToPlace = "a short ride to the north-east", RealmName = "Vlandia"
         });

         said.Should().Contain("Pravend").And.Contain("north-east").And.Contain("Vlandia");
      }

      // And the restraint that keeps this from being a second voice on one fact: INSIDE a settlement,
      // CurrentLocationNote has said where they are since long before this pillar existed.
      [Test]
      public void GIVEN_he_is_inside_the_town_WHEN_rendered_THEN_this_pack_says_nothing_about_where_he_is()
      {
         string said = Render(new HereAndNowFacts {
            Season = Season.Autumn, NearestPlace = "Pravend", IsInsideThatPlace = true, RealmName = "Vlandia"
         });

         said.Should().NotContain("Pravend");
         said.Should().Contain("autumn"); // the hour and season still land
      }

      // The true wilds: no settlement near enough to name, but the land still belongs to someone.
      [Test]
      public void GIVEN_country_with_no_place_worth_naming_WHEN_rendered_THEN_at_least_the_realm_is_known()
      {
         Render(new HereAndNowFacts {Season = Season.Winter, RealmName = "Battania"})
            .Should().Contain("Battania");
      }

      // And nothing known about where means nothing claimed about where.
      [Test]
      public void GIVEN_nothing_known_of_the_place_WHEN_rendered_THEN_no_location_is_invented()
      {
         string said = Render(new HereAndNowFacts {Season = Season.Spring, HourOfDay = 9});

         said.Should().Contain("spring morning");
         said.ToLowerInvariant().Should().NotContain("open country");
      }

      // ── composition and completeness ─────────────────────────────────────

      // A beggar knows it is snowing as surely as a king does.
      [Test]
      public void GIVEN_anyone_at_all_WHEN_composing_THEN_they_are_standing_in_the_weather_too()
      {
         Pack.CarriedBy(new KnowledgeBearer()).Should().BeTrue();
         Pack.CarriedBy(new KnowledgeBearer {IsNotable = true}).Should().BeTrue();
         Pack.CarriedBy(null).Should().BeFalse();
      }

      // Unlike personal_bonds, an empty one here IS a fault: there is always a season and always an hour, so
      // nothing supplied means nobody read the clock. The live sweep should say so.
      [Test]
      public void GIVEN_nothing_supplied_WHEN_swept_THEN_it_counts_as_a_fault_because_there_is_always_an_hour()
      {
         Pack.RequiresContent.Should().BeTrue();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {HereAndNow = new HereAndNowFacts()}).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {HereAndNow = new HereAndNowFacts {Season = Season.Spring}})
             .Should().BeTrue();
      }

      // An unknown season says nothing at all rather than guessing one, because a wrong season is worse than
      // no season: it would have the character contradict what the player can see out of the window.
      [Test]
      public void GIVEN_an_unknown_season_WHEN_rendered_THEN_nothing_is_invented()
      {
         Render(new HereAndNowFacts {Season = Season.Unknown, HourOfDay = 9}).Should().BeEmpty();
      }
   }
}

// Code written by Gabriel Mailhot, 05/11/2026.
// The tournaments under way a character knows of. 3chmidt (Nexus, 05/11/2026): "lords know in what town currently
// tournaments happen, at least in the same kingdom or a neighbouring one". The host picks which; this pins that they reach
// the character as word he has had, per turn, and that nothing is said when none is held.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class TournamentsHeldPromptTests
   {
      private const string Heading = "TOURNAMENTS BEING HELD";

      private static string Build(EncounterContext context)
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"},
                                                  new WorldState {CurrentDay = 10}, context);

      // STAKE: a lord asked where the games are held can answer for his own realm.
      [Test]
      public void GIVEN_a_tournament_in_his_realm_WHEN_the_prompt_is_built_THEN_he_knows_of_it()
      {
         string prompt = Build(new EncounterContext {TournamentsHeld = new[] {"at Pravend, in your own realm, the prize a Noble Bow"}});

         prompt.Should().Contain(Heading);
         prompt.Should().Contain("- at Pravend, in your own realm, the prize a Noble Bow");
      }

      // STAKE: tournaments open and close by the day, so they are sent fresh, below the cache marker.
      [Test]
      public void GIVEN_tournaments_held_WHEN_the_prompt_is_built_THEN_they_are_below_the_cache_marker()
      {
         string prompt = Build(new EncounterContext {TournamentsHeld = new[] {"at Sargot, in Vlandia"}});

         prompt.IndexOf(Heading, System.StringComparison.Ordinal).Should().BeGreaterThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal));
      }

      // STAKE: none held, no heading for the model to fill.
      [Test]
      public void GIVEN_no_tournament_WHEN_the_prompt_is_built_THEN_none_is_spoken_of()
      {
         Build(new EncounterContext()).Should().NotContain(Heading);
      }
   }
}

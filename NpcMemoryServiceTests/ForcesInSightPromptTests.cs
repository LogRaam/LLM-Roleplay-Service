// Code written by Gabriel Mailhot, 05/11/2026.
// The forces in sight of a conversation on the open map. 3chmidt (Nexus, 05/11/2026): a friendly lord did not notice an
// enemy army of 1,800 five meters away and acted surprised when it was mentioned. The host hands the lines; this pins
// that they reach the character as something he has seen, every turn, and that nothing is said when nothing is in sight.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ForcesInSightPromptTests
   {
      private const string Heading = "FORCES IN SIGHT";

      private static string Build(EncounterContext context)
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"},
                                                  new WorldState {CurrentDay = 10}, context);

      // STAKE: 3chmidt's army at the door is seen, and the lord is told not to be surprised by it.
      [Test]
      public void GIVEN_an_enemy_army_in_sight_WHEN_the_prompt_is_built_THEN_the_character_has_seen_it()
      {
         string prompt = Build(new EncounterContext {ForcesInSight = new[] {"an enemy army of Vlandia under Lord Derthert, some 1,800 men"}});

         prompt.Should().Contain(Heading);
         prompt.Should().Contain("- an enemy army of Vlandia under Lord Derthert, some 1,800 men");
         prompt.Should().Contain("never be surprised to hear of them");
      }

      // STAKE: armies move between turns, so what is in sight is sent fresh: it must sit below the cache marker.
      [Test]
      public void GIVEN_forces_in_sight_WHEN_the_prompt_is_built_THEN_they_are_below_the_cache_marker()
      {
         string prompt = Build(new EncounterContext {ForcesInSight = new[] {"a band of looters, some 20 men"}});

         prompt.IndexOf(Heading, System.StringComparison.Ordinal).Should().BeGreaterThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal));
      }

      // STAKE: nothing in sight, no heading: the character is not handed an empty list to fill with invention.
      [Test]
      public void GIVEN_nothing_in_sight_WHEN_the_prompt_is_built_THEN_no_forces_are_spoken_of()
      {
         Build(new EncounterContext()).Should().NotContain(Heading);
         Build(new EncounterContext {ForcesInSight = new string[0]}).Should().NotContain(Heading);
      }
   }
}

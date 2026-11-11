// Code written by Gabriel Mailhot, 11/11/2026.
// What these pin: that a lord marching in an army is told who marches with him and how many. Schmidt (Discord,
// 11/11/2026): "Lords know which lords they invited to their army when they form a grand army, as well as the army size of
// the lords and the total number of men in the grand army." If these fail, a lord asked who rides with him invents names.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class ArmyRosterPromptTests
   {
      // STAKE: the report. The lords of his army and the total reach him, with the rule not to name others.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_a_lord_in_an_army_WHEN_the_prompt_is_built_THEN_he_knows_its_lords_and_its_total(LeanPromptLevel level)
      {
         string prompt = Build(new EncounterContext {
            LeanLevel = level,
            NpcArmyRoster = new[] {"You lead it, with some 200 men in your own party.", "Ingalther, some 150 men.", "In all, some 350 men in 2 parties."}
         });

         prompt.Should().Contain("THE ARMY YOU MARCH IN").And.Contain("- Ingalther, some 150 men.").And.Contain("- In all, some 350 men in 2 parties.")
            .And.Contain("never name others as part of this army");
      }

      // STAKE: a lord in no army is told nothing of one, so the model does not raise a host that is not there.
      [Test]
      public void GIVEN_a_lord_in_no_army_WHEN_the_prompt_is_built_THEN_no_army_is_told()
      {
         Build(new EncounterContext()).Should().NotContain("THE ARMY YOU MARCH IN");
      }

      #region Helpers

      private static string Build(EncounterContext context)
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"}, new WorldState {CurrentDay = 12}, context);

      #endregion
   }
}

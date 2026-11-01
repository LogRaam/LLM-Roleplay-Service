// Code written by Gabriel Mailhot, 01/11/2026.
// A legend the host chose for this character reaches the prompt (DepaHN, 27/10/2026). If it does not, nobody ever tells one; if it
// is written when none was chosen, the model has a heading with nothing under it to invent from.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LegendPromptTests
   {
      private const string Line = "A LEGEND YOU KNOW, \"The Holding of Rovalt\": in spring 1091, Derthert held Rovalt.";

      private static string Prompt(string legend)
      {
         var context = new EncounterContext {LegendKnown = legend};
         var npc = new NpcProfile {Id = "npc_test", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      // STAKE: the whole pillar: a legend no character has in mind is told by nobody.
      [Test]
      public void GIVEN_a_legend_for_this_character_WHEN_they_speak_THEN_it_is_in_their_mind()
      {
         Prompt(Line).Should().Contain(Line);
      }

      // STAKE: only what the mod decided may be told: no legend, no mention, nothing for the model to fill in.
      [Test]
      public void GIVEN_no_legend_WHEN_they_speak_THEN_none_is_mentioned()
      {
         Prompt(null).Should().NotContain("A LEGEND YOU KNOW");
      }
   }
}

// Code written by Gabriel Mailhot, 06/10/2026.
// What an ordinary villager knows of a written tale. fkasad (Nexus, 06/10/2026): "Walked in the village, talked to 3
// regular NPCs (random peasants). They have no idea about women disappearing / werewolf." The tale's locals were only
// the village's notables, because only heroes received the tale's lines. The host now hands the commoner's prompt the
// same lines a notable of that place gets (CommonsKnowledge.AuthoredQuestBlock); if it is dropped, the whole village
// knows of the beast except the people in its street.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CommonerAuthoredQuestTests
   {
      private const string Block = "A MATTER YOU ARE PART OF (The Wolf of the Wood):\n- You believe a werewolf haunts the wood.";

      private static string Prompt(string block)
         => new PromptBuilder().BuildCommonerSystemPrompt(
            new NpcProfile {Id = "commoner", Name = "a villager", Faction = "Vlandia", Clan = ""},
            new CommonsKnowledge {SettlementName = "Atrion", SettlementType = "village", AuthoredQuestBlock = block});

      // The villager in the street believes what the village believes.
      [Test]
      public void GIVEN_a_tale_the_village_knows_of_WHEN_a_villager_speaks_THEN_they_know_it_too()
      {
         Prompt(Block).Should().Contain("You believe a werewolf haunts the wood.");
      }

      // No tale here: the commoner's slim prompt is exactly as it was.
      [Test]
      public void GIVEN_no_tale_WHEN_a_villager_speaks_THEN_nothing_is_added()
      {
         Prompt(null).Should().NotContain("A MATTER YOU ARE PART OF");
      }
   }
}

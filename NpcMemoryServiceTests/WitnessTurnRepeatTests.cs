// Code written by Gabriel Mailhot, 20/10/2026.
// Lordfadooboo (Nexus, 20/10/2026): in a group conversation the character asked him a question, a bystander spoke, and the
// character answered the bystander and then asked him the same question again, sometimes a third time, before he could
// answer. The witness-turn directive told the character the player "has not spoken this turn" and, on the last round, to
// "turn the question to the player". If this is wrong, the player is asked the same thing again after every bystander line.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class WitnessTurnRepeatTests
   {
      private static string Prompt(bool last) => new PromptBuilder().BuildSystemPrompt(
         new NpcProfile {Id = "npc_test", Name = "Apolanea", Faction = "Empire", Clan = "Lonalion", IsFemale = true},
         new WorldState {CurrentDay = 10},
         new EncounterContext {
            Scene = SceneType.Settlement,
            IsWitnessExchangeTurn = true,
            IsLastWitnessExchange = last,
            Witnesses = new[] {new WitnessEntry {Name = "Ira", RelationToNpc = "the player's companion"}}
         });

      // STAKE: the reported case: a question already put to the player waits for the player, it is not asked again after
      // the bystander's line.
      [Test]
      public void GIVEN_a_witness_turn_WHEN_the_prompt_is_built_THEN_the_character_is_told_not_to_ask_the_player_again()
      {
         Prompt(last: false).Should().Contain("Anything you already asked the player is still waiting for their answer");
         Prompt(last: true).Should().Contain("Anything you already asked the player is still waiting for their answer");
      }

      // STAKE: the last round invited the very repeat that was reported.
      [Test]
      public void GIVEN_the_last_witness_turn_WHEN_the_prompt_is_built_THEN_it_no_longer_invites_a_question_to_the_player()
      {
         Prompt(last: true).Should().NotContain("turn the question to the").And.Contain("the player is about to speak");
      }
   }
}

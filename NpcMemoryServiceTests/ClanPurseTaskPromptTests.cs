// Code written by Gabriel Mailhot, 13/11/2026.
// What this pins: a member of the player's own clan is taught never to pay the player for a task.
//
// Aporo (Discord, 13/11/2026): "I don't need that 100 denars for bringing 20 grain when it's my money either way ...
// i told her before that i need no payment for that and still got it". Gabriel: "tout le clan". If this line is lost,
// a wife or a companion promises coin again that the host then refuses to pay, and the player reads a broken word.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ClanPurseTaskPromptTests
   {
      private const string Menu = "- deliver_letter (target_hero: Dame Mina): a message to carry in Lycaron";

      private static string Build(EncounterContext context)
      {
         PromptBuilder real = LeanRealPromptBudgetTests.RealBuilder();
         var builder = new PromptBuilder {
            ActionVocabulary = real.ActionVocabulary, WorldDescription = real.WorldDescription,
            BehaviorGuidelinesOverride = real.BehaviorGuidelinesOverride, AdultLevel = real.AdultLevel, EnableQuests = true
         };

         return builder.BuildSystemPrompt(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // THE REPORT: his wife paid him from what is his own purse.
      [Test]
      public void GIVEN_a_member_of_the_players_clan_WHEN_the_task_teaching_is_built_THEN_they_are_told_to_pay_no_coin()
      {
         Build(new EncounterContext {NpcInPlayerClan = true}).Should().Contain(PromptBuilder.ClanPurseLine);
      }

      // STAKE: a lord of another house pays for a task as before; only the player's own clan shares the purse.
      [Test]
      public void GIVEN_someone_of_another_clan_WHEN_the_task_teaching_is_built_THEN_the_purse_line_stays_out()
      {
         Build(new EncounterContext()).Should().NotContain(PromptBuilder.ClanPurseLine);
      }

      // STAKE: "i told her before that i need no payment for that and still got it".
      [Test]
      public void GIVEN_any_giver_WHEN_the_task_teaching_is_built_THEN_a_player_who_wants_no_payment_gets_none()
      {
         Build(new EncounterContext()).Should().Contain("0 when the player has said they want no payment");
      }

      // STAKE: on the Compact prompt, a clan member is not even shown a coin field to fill.
      [Test]
      public void GIVEN_a_clan_member_on_the_compact_prompt_WHEN_built_THEN_no_coin_field_is_taught()
      {
         string prompt = Build(new EncounterContext {LeanLevel = LeanPromptLevel.Lean, ViableQuestMenu = Menu, NpcInPlayerClan = true});

         prompt.Should().Contain(PromptBuilder.LeanQuestMenuHeader).And.NotContain("reward_gold: N");
      }
   }
}

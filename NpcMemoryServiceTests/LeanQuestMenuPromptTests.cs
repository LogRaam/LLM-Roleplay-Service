// Code written by Gabriel Mailhot, 12/11/2026.
// What this pins: the Compact (Lean) prompt can give a task, from the host's pre-checked menu only.
//
// superdeeplearningaibot (Discord, 12/11/2026): "the edicts that were recently introduced ... doesn't trigger any quests.
// The NPC seems to still read this however, just that it wont trigger an type of quest." The log showed the Compact
// prompt: its quest teaching was skipped whole, so no character could give a task, edict or not. If this fails, a
// player on a small model is never offered a task and cannot tell why; if it leaks, a captive or an enemy hands out
// errands, or the world-blind catalogue costs a small model its budget.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LeanQuestMenuPromptTests
   {
      private const string Menu = "- deliver_letter (target_hero: Dame Mina): a message to carry in Lycaron";

      private static string Build(EncounterContext context)
      {
         PromptBuilder builder = LeanRealPromptBudgetTests.RealBuilder();
         builder = new PromptBuilder {
            ActionVocabulary = builder.ActionVocabulary, WorldDescription = builder.WorldDescription,
            BehaviorGuidelinesOverride = builder.BehaviorGuidelinesOverride, AdultLevel = builder.AdultLevel, EnableQuests = true
         };

         return builder.BuildSystemPrompt(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // THE REPORT: a lord in Compact mode is given the pre-checked tasks and the block to write one.
      [Test]
      public void GIVEN_the_compact_prompt_and_a_checked_menu_WHEN_built_THEN_the_character_may_give_a_task_from_it()
      {
         string prompt = Build(new EncounterContext {LeanLevel = LeanPromptLevel.Lean, ViableQuestMenu = Menu});

         prompt.Should().Contain(PromptBuilder.LeanQuestMenuHeader).And.Contain(Menu).And.Contain("[QUEST]");
      }

      // STAKE: the world-blind catalogue is what let a model invent targets, and a small model has no room for it.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_built_THEN_the_generic_task_catalogue_stays_out()
      {
         string prompt = Build(new EncounterContext {LeanLevel = LeanPromptLevel.Lean, ViableQuestMenu = Menu});

         prompt.Should().NotContain("Task types you may offer");
      }

      // STAKE: with no checked task there is nothing real to give; teaching the block alone invites invented ones.
      [Test]
      public void GIVEN_the_compact_prompt_and_no_menu_WHEN_built_THEN_no_task_is_taught()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Lean}).Should().NotContain(PromptBuilder.LeanQuestMenuHeader);
      }

      // STAKE: an enemy lord does not hand the player errands (fkasad, 29/09/2026), in Compact mode either.
      [Test]
      public void GIVEN_the_compact_prompt_and_a_lord_at_war_with_the_player_WHEN_built_THEN_no_task_is_taught()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, ViableQuestMenu = Menu, WarStatus = DiplomaticStatus.AtWar};

         Build(context).Should().NotContain(PromptBuilder.LeanQuestMenuHeader);
      }

      // STAKE: an intimate (NSFW) scene must never mint a task from a vow (2026-08-23), in Compact mode either.
      [Test]
      public void GIVEN_the_compact_prompt_and_quests_suppressed_WHEN_built_THEN_no_task_is_taught()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, ViableQuestMenu = Menu, SuppressQuests = true};

         Build(context).Should().NotContain(PromptBuilder.LeanQuestMenuHeader);
      }
   }
}

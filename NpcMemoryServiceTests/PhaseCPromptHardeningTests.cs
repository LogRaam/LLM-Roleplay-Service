// Code written by Gabriel Mailhot, 17/07/2026.
// Phase C of the quest/action-parser audit: the parser was hardened first, and these tests pin the prompt-side
// half, teaching a WEAK model the exact bracketed shape it must reproduce for [ACTION]/[QUEST]/[QUEST_COMPLETE]
// blocks to actually parse, rather than leaving it to infer syntax from an abstract field-name template alone.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PhaseCPromptHardeningTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildWithActionVocabulary()
      {
         var builder = new PromptBuilder {
            ActionVocabulary = new List<GameActionDefinition> {
               new() {Type = "change_relation", Description = "Nudge your regard for the player.", Parameters = new List<string> {"delta"}}
            }
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10});
      }

      // GAME ACTIONS taught only the abstract field-name template ("type: action_name", "param_name: value"),
      // never a concrete filled block. A weak model asked to copy an abstract placeholder often reproduces the
      // placeholder text itself instead of substituting real values; a worked example closes that gap.
      [Test]
      public void GIVEN_an_action_vocabulary_WHEN_teaching_the_action_format_THEN_a_fully_filled_action_example_follows_the_template()
      {
         string prompt = BuildWithActionVocabulary();

         prompt.Should().Contain("type: change_relation");
         prompt.Should().Contain("delta: 1");
      }

      // Weak models were seen inventing their own section tags (lowercase, mismatched brackets, no closing
      // tag) because the format contract never states the lexical rule outright. Spelling it out once, plainly,
      // near the top of the FULL format contract, is the explicit rule a weak model can fall back on.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_the_uppercase_open_close_tag_rule_is_stated_explicitly()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("Section labels are UPPERCASE, each OPENED as [LABEL] and CLOSED as [/LABEL]");
      }

      // C5 feedback loop: when the previous reply's [QUEST] block could not be registered, the MODEL (not just
      // the player) must be told, or a weak model keeps speaking of a task the game never recorded. Injected only
      // the turn after the refusal.
      [Test]
      public void GIVEN_a_pending_format_feedback_note_WHEN_built_THEN_the_model_is_told_its_last_block_was_refused()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            LlmFormatFeedbackNote = "Your last task offer could not be registered (it came through garbled)."
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("NOTE ON YOUR LAST REPLY:");
         prompt.Should().Contain("could not be registered");
      }

      // The other side: an ordinary turn (no refusal last turn) must NOT carry the note, or every reply would
      // open with a spurious apology for a block the model never got wrong.
      [Test]
      public void GIVEN_no_feedback_note_WHEN_built_THEN_no_refusal_note_is_shown()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full};

         builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context)
                .Should().NotContain("NOTE ON YOUR LAST REPLY");
      }
   }
}

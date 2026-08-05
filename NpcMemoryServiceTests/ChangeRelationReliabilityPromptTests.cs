// Code written by Gabriel Mailhot, 08/05/2026.
// Cause 1 of a real player report (DeepSeek playthrough, 2026-08-05): actions were narrated in prose but never
// emitted as [ACTION] blocks, and change_relation was NOT emitted even once across the whole session, while the
// SAME dialogues emitted it fine on Grok. DeepSeek writes good prose but drops the structured output. These tests
// pin two additive reinforcements: a change_relation-reliability directive next to its teaching (Full only, the
// Lean budget has no room), and a compact ACTION CHECKLIST as the very last thing before the model generates.

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
   public class ChangeRelationReliabilityPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // GAME ACTIONS (and the reliability directive beside it) only renders when the host supplied an action
      // vocabulary, exactly like the live game does; an empty PromptBuilder skips the whole section.
      private static string Build(LeanPromptLevel level)
         => new PromptBuilder {
            ActionVocabulary = new List<GameActionDefinition> {
               new() {Type = "change_relation", Description = "Nudge your regard for the player.", Parameters = new List<string> {"delta"}}
            }
         }.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = level});

      // The core fix: a shift in regard (praise, insult, a kindness, an affront, a bold or foolish claim) must
      // be tied to the change_relation [ACTION] tag, parallel to the existing anti-empty-promise deed rule, so
      // a weak model cannot narrate warmer or colder feelings in prose and consider the relation moved.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_the_change_relation_reliability_directive_is_present()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("CHANGE_RELATION IS HOW THE GAME LEARNS YOUR REGARD SHIFTED");
         prompt.Should().Contain("praise, an insult, a");
         prompt.Should().Contain("kindness done to you, an affront, a bold or a foolish claim");
         prompt.Should().Contain("The game records the shift ONLY from that block");
      }

      // Placement: the directive sits beside the change_relation teaching in GAME ACTIONS, well before the
      // final FORMAT REMINDER restates it, not merged into or replacing either.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_the_directive_sits_after_the_change_relation_example_and_before_the_final_reminder()
      {
         string prompt = Build(LeanPromptLevel.Full);

         int exampleIndex = prompt.IndexOf("For example:", System.StringComparison.Ordinal);
         int directiveIndex = prompt.IndexOf(
            "CHANGE_RELATION IS HOW THE GAME LEARNS YOUR REGARD SHIFTED", System.StringComparison.Ordinal);
         int checklistIndex = prompt.IndexOf(
            "ACTION CHECKLIST, BEFORE YOU FINISH THIS REPLY", System.StringComparison.Ordinal);

         directiveIndex.Should().BeGreaterThan(exampleIndex);
         checklistIndex.Should().BeGreaterThan(directiveIndex);
      }

      // Budget guard (task 8's Lean policy): the Full-only directive must not leak into Lean, which has almost
      // no headroom left (LeanPromptPolicyTests pins the hard byte budget).
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_full_only_directive_is_absent()
      {
         string prompt = Build(LeanPromptLevel.Lean);

         prompt.Should().NotContain("CHANGE_RELATION IS HOW THE GAME LEARNS YOUR REGARD SHIFTED");
      }

      // Lean still gets a one-line nudge folded into its existing three-line FORMAT REMINDER, so even the
      // smallest model reads that a shift in regard is one of the changes it must not leave to prose alone.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_format_reminder_still_names_a_shift_in_regard()
      {
         Build(LeanPromptLevel.Lean).Should().Contain("a shift in regard");
      }

      // The final checklist: the highest-recency spot in the whole prompt (immediately before generation),
      // listing every must-emit case in a few tight lines, and stating plainly that prose alone is not enough.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_the_action_checklist_lists_every_must_emit_case()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("ACTION CHECKLIST, BEFORE YOU FINISH THIS REPLY");
         prompt.Should().Contain("-> change_relation");
         prompt.Should().Contain("its own [ACTION]");
         prompt.Should().Contain("-> end_conversation");
         prompt.Should().Contain("prose describing it is not enough");
      }

      // Recency: the checklist must be the LAST thing in the built-in prompt (after the literal bracket
      // skeleton in AppendFormatReminder), the single highest-leverage position for a weak model.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_the_action_checklist_is_the_last_thing_before_generation()
      {
         string prompt = Build(LeanPromptLevel.Full);

         int checklistIndex = prompt.IndexOf(
            "ACTION CHECKLIST, BEFORE YOU FINISH THIS REPLY", System.StringComparison.Ordinal);
         int skeletonIndex = prompt.IndexOf(
            "[DIALOGUE]your spoken words[/DIALOGUE]", System.StringComparison.Ordinal);

         checklistIndex.Should().BeGreaterThan(skeletonIndex);
         (prompt.Length - checklistIndex).Should().BeLessThan(400);
      }
   }
}

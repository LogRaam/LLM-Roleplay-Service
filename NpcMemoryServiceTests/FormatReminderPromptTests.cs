// Code written by Gabriel Mailhot, 12/07/2026.
// The full [SECTION] format contract sits ~10k tokens up in the cached prefix; a weaker model that follows
// structure loosely (player report: DeepSeek gives good prose but emits no [ACTION]/[EVENT], so no mechanics
// fire) is likelier to comply when the rule is restated forcefully at the very END of the prompt. These tests
// lock that a short FORMAT REMINDER is emitted last, in both Full and Lean, after the main format teaching.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class FormatReminderPromptTests
   {
      private const string Reminder = "FORMAT REMINDER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(LeanPromptLevel level)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = level});

      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_a_format_reminder_is_restated_after_the_main_contract()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain(Reminder);
         prompt.Should().Contain("[DIALOGUE]");

         // Recency intent: the reminder must come AFTER the main "RESPONSE FORMAT" teaching, near the end.
         prompt.IndexOf(Reminder, System.StringComparison.Ordinal)
               .Should().BeGreaterThan(prompt.IndexOf("RESPONSE FORMAT", System.StringComparison.Ordinal));
      }

      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_format_reminder_is_still_present()
      {
         // Lean is exactly the weak/small-model case that needs the reminder most.
         Build(LeanPromptLevel.Lean).Should().Contain(Reminder);
      }

      [Test]
      public void GIVEN_any_prompt_WHEN_built_THEN_the_reminder_names_the_consequence_of_skipping_a_block()
      {
         // The motivator is the consequence, not just the rule: skip the block and the game cannot act on it.
         Build(LeanPromptLevel.Full).Should().Contain("the game cannot see");
      }
   }
}

// Code written by Gabriel Mailhot, 01/07/2026.
// Documents the lean-prompt section selection: Full keeps everything, Lean drops the heavy flavour/context
// sections and shortens memory + the behaviour guidelines, so the prompt fits a small model's short context.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LeanPromptPolicyTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      [Test]
      public void GIVEN_the_full_level_WHEN_selecting_sections_THEN_every_heavy_section_is_kept()
      {
         foreach (PromptSection section in System.Enum.GetValues(typeof(PromptSection)))
            LeanPromptPolicy.Include(section, LeanPromptLevel.Full).Should().BeTrue();
      }

      [Test]
      public void GIVEN_the_lean_level_WHEN_selecting_sections_THEN_every_heavy_section_is_dropped()
      {
         foreach (PromptSection section in System.Enum.GetValues(typeof(PromptSection)))
            LeanPromptPolicy.Include(section, LeanPromptLevel.Lean).Should().BeFalse();
      }

      // These pin the specific call sites (task 8) that actually consult Include(section, ...), so the
      // section parameter is exercised for real rather than the level alone deciding everything.

      [TestCase(PromptSection.WitnessMachineryTeaching)]
      [TestCase(PromptSection.ProseCraftBlocklist)]
      [TestCase(PromptSection.DeliverPrisonerOffer)]
      public void GIVEN_a_task_8_lean_section_WHEN_selecting_THEN_full_keeps_it_and_lean_drops_it(PromptSection section)
      {
         LeanPromptPolicy.Include(section, LeanPromptLevel.Full).Should().BeTrue();
         LeanPromptPolicy.Include(section, LeanPromptLevel.Lean).Should().BeFalse();
      }

      [Test]
      public void GIVEN_the_full_level_WHEN_reading_the_memory_limit_THEN_all_events_are_kept()
      {
         LeanPromptPolicy.MemoryEventLimit(LeanPromptLevel.Full).Should().Be(int.MaxValue);
      }

      [Test]
      public void GIVEN_the_lean_level_WHEN_reading_the_memory_limit_THEN_only_a_small_recent_window_is_kept()
      {
         LeanPromptPolicy.MemoryEventLimit(LeanPromptLevel.Lean).Should().Be(LeanPromptPolicy.LeanMemoryEventLimit);
         LeanPromptPolicy.LeanMemoryEventLimit.Should().BeLessThan(int.MaxValue).And.BePositive();
      }

      [Test]
      public void GIVEN_a_level_WHEN_deciding_the_behaviour_guidelines_THEN_only_full_uses_the_authored_file()
      {
         LeanPromptPolicy.UseFullBehaviorGuidelines(LeanPromptLevel.Full).Should().BeTrue();
         LeanPromptPolicy.UseFullBehaviorGuidelines(LeanPromptLevel.Lean).Should().BeFalse();
      }

      /// <summary>
      ///   Size sanity test (task 8): a Lean prompt for a minimal profile must stay well under a small
      ///   local model's context window. Budget: ~1.6k tokens of instruction overhead, ~6.5k chars at the
      ///   usual ~4 chars/token. The always-on Lean contract crept ~600 chars across later increments (new
      ///   actions, rules); 2026-07-06 the lean behaviour guidelines were tightened to reclaim some, landing
      ///   at ~6.47k chars for a minimal profile (no history, no relationships, no encounter flavour). 2026-07-12
      ///   the compact end-of-prompt FORMAT REMINDER (three lines in Lean, so a weak model still emits [ACTION]/
      ///   [EVENT] blocks) added ~126 chars, landing at ~6.73k. 6850 leaves a little headroom for incidental
      ///   drift without being so loose it stops catching a regression that re-bloats Lean mode.
      /// </summary>
      [Test]
      public void GIVEN_a_lean_prompt_for_a_minimal_profile_WHEN_built_THEN_it_stays_under_the_token_budget()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Length.Should().BeLessThan(6850);
      }
   }
}

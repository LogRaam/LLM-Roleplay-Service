// Code written by Gabriel Mailhot, 07/09/2026.
// Companion-briefing pillar: EncounterContext.CompanionBrief is host-resolved per SPEAKER (keyed on the
// speaking hero's own id, see CalradiaRemembers.Mapping.EncounterContextBuilder), so PromptBuilder's only job
// is to render it verbatim when present and add nothing when absent: the field being null IS the mechanism
// that keeps the conversation's target from ever seeing what the player told a companion in a separate,
// private conversation they were never part of. Mirrors AppreciationNoteTests' shape. Unconditional (not
// Lean-gated): a player's own explicit standing instruction outweighs the flavour sections Lean drops.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CompanionBriefPromptTests
   {
      private const string Marker = "UNIQUE_COMPANION_BRIEF_TEST_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Companion",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The host has already resolved WHICH companion this call is for (by keying the store lookup on the
      // speaking hero's own id); PromptBuilder's only job is to place the text unchanged, in the companion's
      // own turn.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_a_companion_brief_is_supplied_THEN_it_is_rendered_verbatim()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionBrief = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      // Ratified requirement: on the LEAN prompt this line must SURVIVE, it is the player's own explicit
      // instruction, worth more than most of what Lean keeps. Not gated by LeanPromptPolicy.Include.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_a_companion_brief_is_supplied_THEN_it_still_survives()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, CompanionBrief = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      // The load-bearing property of the whole feature, at the PromptBuilder layer: a null field (which is
      // exactly what the target's own turn gets, since the host never resolves a brief for anyone but the
      // briefed companion) must add NOTHING to the prompt: there must be no trace, no empty section, nothing
      // for the target to ever see. Whitespace counts as "nothing to say" too, never a blank instruction.
      [Test]
      public void GIVEN_no_companion_brief_WHEN_building_a_prompt_THEN_nothing_extra_is_added()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionBrief = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Marker);
         promptNone.Should().NotContain("THE PLAYER'S PRIVATE WORD TO YOU");
         promptBlank.Should().NotContain("THE PLAYER'S PRIVATE WORD TO YOU");
      }

      // The companion must never announce the briefing happened (the whole design point of a SEPARATE prior
      // conversation is that there is nothing to hide, because the target was never part of it), but that
      // discretion still has to be TAUGHT, or a chatty model could blurt "the player told me to press you on
      // this" straight to the very person the private word was about.
      [Test]
      public void GIVEN_a_companion_brief_WHEN_building_a_prompt_THEN_the_model_is_told_not_to_reveal_the_briefing()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionBrief = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("Never announce that you were briefed");
      }
   }
}

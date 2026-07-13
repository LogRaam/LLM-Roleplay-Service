// Code written by Gabriel Mailhot, 04/07/2026.
// Divorce escalation: EncounterContext.SpouseEstrangementNote is host-composed (the consumer resolves it
// only for the player's OWN spouse whose estrangement is currently active) and rendered verbatim by
// PromptBuilder, right after PlayerEndOwnMarriageNote, since it carries the accept_divorce [ACTION] format
// itself. Dropped in Lean mode exactly like the sibling divorce notes.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SpouseEstrangementNoteTests
   {
      private const string Marker = "type: accept_divorce - UNIQUE_SPOUSE_ESTRANGEMENT_TEST_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Spouse",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // This note folds in the accept_divorce [ACTION] format itself (decline_divorce no longer applies once
      // an estrangement is under way). If it fails to reach the prompt, the estranged spouse loses the only
      // teaching that lets the player formally accept the split in dialogue.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_a_spouse_estrangement_note_is_supplied_THEN_it_is_rendered_verbatim()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, SpouseEstrangementNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      // Dropped in Lean exactly like the sibling divorce notes: a small model still gets the estrangement
      // voiced through the NPC's own history/letters, just not the extended [ACTION] contract it has no
      // headroom for.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_a_spouse_estrangement_note_is_supplied_THEN_it_is_omitted()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, SpouseEstrangementNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(Marker);
      }

      // Blank guard: this NPC's estrangement not being active must render nothing, so a spouse who is not
      // estranged never gets handed the accept_divorce action by mistake.
      [Test]
      public void GIVEN_no_spouse_estrangement_note_WHEN_building_a_full_prompt_THEN_nothing_extra_is_added()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, SpouseEstrangementNote = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Marker);
         promptBlank.Should().NotContain(Marker);
      }

      // Two independent host-composed notes, resolved by different rules (own spouse currently estranged vs
      // player choosing to end the marriage themselves). PromptBuilder does not police any relationship
      // between them, so this guards that supplying both together renders both, neither clobbering the other.
      [Test]
      public void GIVEN_both_the_estrangement_and_end_own_marriage_notes_supplied_WHEN_building_a_full_prompt_THEN_both_are_rendered_together()
      {
         var builder = new PromptBuilder();
         const string endOwnMarriageMarker = "type: end_own_marriage - UNIQUE_COEXIST_END_OWN_MARRIAGE_MARKER";
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            SpouseEstrangementNote = Marker,
            PlayerEndOwnMarriageNote = endOwnMarriageMarker
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
         prompt.Should().Contain(endOwnMarriageMarker);
      }
   }
}

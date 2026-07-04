// Code written by Gabriel Mailhot, 04/07/2026.
// Divorce, Phase 2a: EncounterContext.PlayerEndOwnMarriageNote is host-composed (the consumer resolves it
// only when the NPC is the player's own living spouse) and rendered verbatim by PromptBuilder, right after
// SpouseDivorceDemandNote, since it carries the end_own_marriage [ACTION] format itself. Dropped in Lean
// mode exactly like SpouseDivorceDemandNote (a small model does not need the extended verb).

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PlayerEndOwnMarriageNoteTests
   {
      private const string Marker = "type: end_own_marriage — UNIQUE_PLAYER_END_OWN_MARRIAGE_TEST_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Spouse",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      [Test]
      public void GIVEN_a_full_prompt_WHEN_a_player_end_own_marriage_note_is_supplied_THEN_it_is_rendered_verbatim()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerEndOwnMarriageNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      [Test]
      public void GIVEN_a_lean_prompt_WHEN_a_player_end_own_marriage_note_is_supplied_THEN_it_is_omitted()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, PlayerEndOwnMarriageNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(Marker);
      }

      [Test]
      public void GIVEN_no_player_end_own_marriage_note_WHEN_building_a_full_prompt_THEN_nothing_extra_is_added()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerEndOwnMarriageNote = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Marker);
         promptBlank.Should().NotContain(Marker);
      }

      [Test]
      public void GIVEN_both_notes_supplied_WHEN_building_a_full_prompt_THEN_both_are_rendered_together()
      {
         var builder = new PromptBuilder();
         const string spouseDemandMarker = "type: accept_divorce — UNIQUE_COEXIST_SPOUSE_DEMAND_MARKER";
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            SpouseDivorceDemandNote = spouseDemandMarker,
            PlayerEndOwnMarriageNote = Marker
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(spouseDemandMarker);
         prompt.Should().Contain(Marker);
      }
   }
}

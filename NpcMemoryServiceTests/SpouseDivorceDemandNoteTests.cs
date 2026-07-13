// Code written by Gabriel Mailhot, 04/07/2026.
// Divorce, Phase 2b: EncounterContext.SpouseDivorceDemandNote is host-composed (the consumer resolves it
// only for the player's OWN demanding spouse) and rendered verbatim by PromptBuilder, right after
// ExtraActionTeachings, since it carries the accept_divorce / decline_divorce [ACTION] format itself.
// Dropped in Lean mode exactly like ExtraActionTeachings (a small model does not need the extended verb).

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SpouseDivorceDemandNoteTests
   {
      private const string Marker = "type: accept_divorce — UNIQUE_SPOUSE_DIVORCE_TEST_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Spouse",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // This note folds in the accept_divorce / decline_divorce [ACTION] format itself (Divorce, Phase 2b).
      // If it fails to reach the prompt, the player loses the taught path to answer the demanding spouse in
      // dialogue at all.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_a_spouse_divorce_demand_note_is_supplied_THEN_it_is_rendered_verbatim()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, SpouseDivorceDemandNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      // Dropped in Lean like ExtraActionTeachings: a small model does not need the extended [ACTION]
      // contract, but the demand itself still surfaces through the NPC's own history and letters.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_a_spouse_divorce_demand_note_is_supplied_THEN_it_is_omitted()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, SpouseDivorceDemandNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(Marker);
      }

      // Blank guard: an NPC who is not the player's own demanding spouse must never be handed the
      // accept_divorce / decline_divorce actions.
      [Test]
      public void GIVEN_no_spouse_divorce_demand_note_WHEN_building_a_full_prompt_THEN_nothing_extra_is_added()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, SpouseDivorceDemandNote = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Marker);
         promptBlank.Should().NotContain(Marker);
      }
   }
}

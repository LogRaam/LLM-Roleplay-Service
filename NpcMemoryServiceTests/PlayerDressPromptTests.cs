// Code written by Gabriel Mailhot, 21/07/2026.
// Perception layer: EncounterContext.PlayerDress, set by the mod from the player's live equipment (see
// CalradiaRemembers.Logic.PlayerDressPolicy / CalradiaRemembers.Mapping.PlayerDressResolver), is rendered by
// PromptBuilder only for a REMARKABLE state (Naked/Rags). Civilian and Armoured are the ordinary states an
// NPC would not comment on unprompted, so they must render NOTHING: every prompt in the game would grow for
// free otherwise. This pins that render-worthiness gate from the SDK side, independent of the mod's own
// classification tests.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PlayerDressPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // Naked is the state the owner was explicit must be reachable and remarked on: a captor cannot later
      // strip a prisoner meaningfully if the NPC never notices the result.
      [Test]
      public void GIVEN_the_player_is_naked_WHEN_building_the_prompt_THEN_a_factual_note_is_injected()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerDress = PlayerDressState.Naked};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("nothing on at all");
      }

      // Rags is the other remarkable state (a quality read on what is worn, not merely "not armoured").
      [Test]
      public void GIVEN_the_player_wears_rags_WHEN_building_the_prompt_THEN_a_factual_note_is_injected()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerDress = PlayerDressState.Rags};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("ragged clothing");
      }

      // The ordinary case: a properly dressed player must add NOTHING to the prompt, or every single
      // conversation in the game grows for a fact almost never worth remarking on.
      [Test]
      public void GIVEN_the_player_is_ordinarily_dressed_as_a_civilian_WHEN_building_the_prompt_THEN_no_dress_note_is_injected()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerDress = PlayerDressState.Civilian};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain("nothing on at all");
         prompt.Should().NotContain("ragged clothing");
      }

      // Armoured is deliberately treated as unremarkable too (see PromptBuilder.AppendPlayerDressNote): most
      // travellers and every fighter reads as armoured or civilian, so noting battle harness on every turn
      // would be pure token cost for a state the scene/war-status sections already imply.
      [Test]
      public void GIVEN_the_player_is_armoured_WHEN_building_the_prompt_THEN_no_dress_note_is_injected()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerDress = PlayerDressState.Armoured};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain("nothing on at all");
         prompt.Should().NotContain("ragged clothing");
      }
   }
}

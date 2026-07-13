// Code written by Gabriel Mailhot, 12/07/2026.
// Player report: the player addressed a present companion (a witness), but the MAIN NPC answered and even
// voiced the witness's line inline ("Tezzeret's lips curve into a smirk. They will learn."). The tightened
// SCENE DISCIPLINE forbids putting any spoken words in another character's mouth inside your own text; a
// witness's words belong only in their own attributed [WITNESS_REACTION]. These tests pin that wording.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SceneDisciplinePromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Sturgia",
         Clan = "Vagiroving"
      };

      private static string Build(LeanPromptLevel level)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = level});

      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_scene_discipline_forbids_composing_another_character_s_words()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("SCENE DISCIPLINE");
         prompt.Should().Contain("never yours to compose");
      }

      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_a_witness_s_words_are_confined_to_their_own_block()
      {
         Build(LeanPromptLevel.Full).Should().Contain("belong in THEIR block");
      }

      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_full_scene_discipline_block_is_not_carried()
      {
         // Scene discipline is a Full-prompt section; the Lean format contract returns before it.
         Build(LeanPromptLevel.Lean).Should().NotContain("SCENE DISCIPLINE");
      }
   }
}

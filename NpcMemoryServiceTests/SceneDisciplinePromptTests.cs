// Code written by Gabriel Mailhot, 12/07/2026.
// Player report 1 (2026-07-12): the player addressed a present companion (a witness), but the MAIN NPC answered
// and even voiced the witness's line inline ("Tezzeret's lips curve into a smirk. They will learn."). The
// tightened SCENE DISCIPLINE forbids putting any spoken words in another character's mouth inside your own text.
// Player report 2 (2026-07-15): since that tightening, witnesses rarely interject or address each other at all.
// The block sits at the very END of the prompt (maximum recency), and its unconditional "You are the ONLY
// character speaking" wall muffled the witness liveliness the earlier WITNESSES PRESENT teaching invites. The
// block is therefore witness-AWARE now: strict wall without witnesses, anchor-plus-invitation with them, and the
// core never-compose-their-words rule identical in both. These tests pin all three faces.

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

      private static string BuildWithWitness()
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {
               LeanLevel = LeanPromptLevel.Full,
               Witnesses = new List<WitnessEntry> {
                  new() {Name = "Tezzeret", RelationToNpc = "a rival lord", Persona = "aloof schemer"}
               }
            });

      // The exact bug from the 2026-07-12 report: the main NPC voiced a present witness's line inline. This
      // pins the rule that forbids putting spoken words in another character's mouth. It must hold in BOTH
      // variants (with and without witnesses): loosening liveliness must never reopen that hole.
      [Test]
      public void GIVEN_a_prompt_with_or_without_witnesses_WHEN_built_THEN_composing_another_character_s_words_stays_forbidden()
      {
         Build(LeanPromptLevel.Full).Should().Contain("SCENE DISCIPLINE").And.Contain("never yours to compose");
         BuildWithWitness().Should().Contain("SCENE DISCIPLINE").And.Contain("never yours to compose");
      }

      // The witness-specific half of the same fix: a witness's own reply belongs only in their attributed
      // [WITNESS_REACTION] block, not woven into the main NPC's dialogue or narration.
      [Test]
      public void GIVEN_witnesses_present_WHEN_built_THEN_a_witness_s_words_are_confined_to_their_own_block()
      {
         BuildWithWitness().Should().Contain("belong in THEIR block");
      }

      // The 2026-07-15 regression: "You are the ONLY character speaking" was emitted even with witnesses in the
      // room, at recency, and the model obeyed the wall instead of the early liveliness teaching. With witnesses
      // present the wall must be gone, replaced by the anchor framing and an ACTIVE invitation to step in, so
      // the last thing the model reads now encourages the interjection instead of forbidding it in spirit.
      [Test]
      public void GIVEN_witnesses_present_WHEN_built_THEN_the_only_speaker_wall_is_replaced_by_an_invitation_to_step_in()
      {
         string prompt = BuildWithWitness();

         prompt.Should().NotContain("You are the ONLY character speaking");
         prompt.Should().Contain("scene's ANCHOR");
         prompt.Should().Contain("THE ROOM IS ALIVE");
      }

      // The other side of the witness-aware split: with NO witnesses, the strict wall is correct (there is
      // genuinely no one else to voice) and must stay word-for-word, or the 2026-07-12 bug creeps back through
      // the solo path.
      [Test]
      public void GIVEN_no_witnesses_WHEN_built_THEN_the_strict_only_speaker_wall_remains()
      {
         Build(LeanPromptLevel.Full).Should().Contain("You are the ONLY character speaking");
      }

      // Gabriel's ratified direction (2026-07-16): a witness the topic CONCERNS participates, and that includes
      // SPEAKING and ASKING QUESTIONS, not just gesturing. The old teaching capped them at "One sentence in
      // their voice, no more", which reads as a gag order; this pins the richer allowance and the anchor rule
      // that keeps it from becoming a hijack.
      [Test]
      public void GIVEN_witnesses_present_WHEN_built_THEN_a_concerned_witness_may_question_and_press_a_point_but_never_take_over()
      {
         string prompt = BuildWithWitness();

         prompt.Should().Contain("may ask the player or you");
         prompt.Should().Contain("never take over the conversation");
         prompt.Should().NotContain("One sentence in their voice");
      }

      // Confirms this fix rides the Full-prompt format contract rather than being a bolt-on: Lean returns
      // before this section is ever appended, so a Lean deployment does not carry its token cost.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_full_scene_discipline_block_is_not_carried()
      {
         // Scene discipline is a Full-prompt section; the Lean format contract returns before it.
         Build(LeanPromptLevel.Lean).Should().NotContain("SCENE DISCIPLINE");
      }
   }
}

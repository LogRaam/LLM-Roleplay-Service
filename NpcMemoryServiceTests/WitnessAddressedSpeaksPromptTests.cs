// Code written by Gabriel Mailhot, 27/08/2026.
// Player report (2026-08-27): in a scene with more than one character present, a witness answered a line spoken
// straight AT them with only a wordless *gesture*, so the main speaker read it as the cold shoulder and the
// conversation soured. The [WITNESS_REACTION] format allows a gesture alone, and models lean on atmospheric
// gesture-only prose, which is fine for a background beat but wrong when the witness is actually being addressed.
// These pin the two rules that fix it: a witness spoken TO must answer in WORDS, and a gesture-only reaction is
// only for a background beat. Both live in the detailed witness teaching, so they never touch the lean budget.

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
   public class WitnessAddressedSpeaksPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildWithWitness() =>
         new PromptBuilder().BuildSystemPrompt(
            Npc(),
            new WorldState {CurrentDay = 10},
            new EncounterContext {
               LeanLevel = LeanPromptLevel.Full,
               Witnesses = new List<WitnessEntry> {
                  new() {Name = "Shiera", RelationToNpc = "a lady of the court"}
               }
            });

      // The core fix. When someone speaks straight at a witness, a bare gesture in reply is the "silent
      // treatment" the player reported: the rule must tell the model an addressed witness answers in WORDS.
      [Test]
      public void GIVEN_witnesses_present_WHEN_building_the_prompt_THEN_an_addressed_witness_is_told_to_answer_in_words()
      {
         string prompt = BuildWithWitness();

         prompt.Should().Contain("WHEN A WITNESS IS SPOKEN TO, THEY ANSWER IN WORDS.");
         prompt.Should().Contain("witness MUST reply this turn with a SPOKEN line in quotes");
      }

      // The other half: gesture-only is legitimate for a background beat, so we do not ban it outright; we pin
      // it to that case, so a witness who actually engages produces words rather than yet another silent look.
      [Test]
      public void GIVEN_witnesses_present_WHEN_building_the_prompt_THEN_gesture_only_is_reserved_for_a_background_beat()
      {
         string prompt = BuildWithWitness();

         prompt.Should().Contain("A reaction that is ONLY a gesture is for a fleeting background beat");
      }

      // The rule belongs to the witness teaching: with no witnesses there is no witness section at all, so the
      // rule must be absent (and, being in the detailed block, it never reaches an ordinary two-person prompt).
      [Test]
      public void GIVEN_no_witnesses_WHEN_building_the_prompt_THEN_the_addressed_witness_rule_is_absent()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = LeanPromptLevel.Full});

         prompt.Should().NotContain("WHEN A WITNESS IS SPOKEN TO, THEY ANSWER IN WORDS.");
      }
   }
}

// Code written by Gabriel Mailhot, 27/08/2026.
// Player report (2026-08-27): in a scene with more than one character present, a witness answered a line spoken
// straight AT them with only a wordless *gesture*, so the main speaker read it as the cold shoulder and the
// conversation soured. The [WITNESS_REACTION] format allows a gesture alone, and models lean on atmospheric
// gesture-only prose, which is fine for a background beat but wrong when the witness is actually being addressed.
// These pin the two rules that fix it: a witness spoken TO must answer in WORDS, and a gesture-only reaction is
// only for a background beat. Both live in the detailed witness teaching, so they never touch the lean budget.
//
// Follow-up report (2026-08-28): the v2.3.1 fix above only covered a witness literally ADDRESSED. A witness who
// was merely DISCUSSED (named, made the subject of the exchange) still fell through the gap, gave a gesture-only
// reaction, and the main speaker escalated as though snubbed. Two more rules close that: the "answer in words"
// trigger is broadened past direct address (named/discussed/subject of the exchange also counts), and a separate
// directive tells the MAIN SPEAKER never to over-read a genuine bystander's silence as a snub in the first place.

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

         prompt.Should().Contain("WHEN A WITNESS IS PART OF WHAT IS BEING SAID, THEY ANSWER IN WORDS.");
         prompt.Should().Contain("witness MUST reply this turn with a SPOKEN line in quotes");
      }

      // The other half: gesture-only is legitimate for a background beat, so we do not ban it outright; we pin
      // it to that case, so a witness who actually engages produces words rather than yet another silent look.
      [Test]
      public void GIVEN_witnesses_present_WHEN_building_the_prompt_THEN_gesture_only_is_reserved_for_a_periphery_witness()
      {
         string prompt = BuildWithWitness();

         prompt.Should().Contain("Reserve a reaction that is ONLY a gesture for a witness genuinely on the PERIPHERY");
      }

      // The follow-up fix (2026-08-28): the rule must not stop at literal address. A witness who is merely
      // NAMED or DISCUSSED, without being spoken to directly, is exactly the gap the reported bug fell through
      // (the bystander was discussed, not addressed, and still gave a gesture-only reaction).
      [Test]
      public void GIVEN_witnesses_present_WHEN_building_the_prompt_THEN_a_discussed_witness_also_must_answer_in_words()
      {
         string prompt = BuildWithWitness();

         prompt.Should().Contain("This is not only");
         prompt.Should().Contain("when they are directly addressed: if you (the main speaker) or the player name a");
         prompt.Should().Contain("witness, discuss them, question them, or make them the subject of the current");
      }

      // The other half of the follow-up fix: even where a witness legitimately stays gesture-only (genuinely on
      // the periphery), the MAIN SPEAKER must not read that silence as an insult. This is the exact over-read
      // that derailed the reported scene, so it is asserted independently of the broadened trigger above.
      [Test]
      public void GIVEN_witnesses_present_WHEN_building_the_prompt_THEN_the_main_speaker_is_told_not_to_over_read_silence()
      {
         string prompt = BuildWithWitness();

         prompt.Should().Contain("DO NOT OVER-READ A BYSTANDER'S SILENCE.");
         prompt.Should().Contain("never treat that silence, quiet,");
         prompt.Should().Contain("or wordless gesture as a snub, a cold shoulder, an insult, or disrespect.");
      }

      // The rule belongs to the witness teaching: with no witnesses there is no witness section at all, so the
      // rule must be absent (and, being in the detailed block, it never reaches an ordinary two-person prompt).
      [Test]
      public void GIVEN_no_witnesses_WHEN_building_the_prompt_THEN_the_addressed_witness_rule_is_absent()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = LeanPromptLevel.Full});

         prompt.Should().NotContain("WHEN A WITNESS IS PART OF WHAT IS BEING SAID, THEY ANSWER IN WORDS.");
         prompt.Should().NotContain("DO NOT OVER-READ A BYSTANDER'S SILENCE.");
      }
   }
}

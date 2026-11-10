// Code written by Gabriel Mailhot, 02/11/2026.
// The main speaker must stay free to disagree with a witness. froggyluv (Nexus, 2/11/2026, v2.8.1): when the player's
// companion spoke up in a talk with a lord, the lord ALWAYS praised her, "no matter how sharp or combative" ("Your
// Shield Maiden has the right of it", "She's the smartest one here"). No line told him to praise; the witness teaching
// only showed an ally who "confirms", and the witness-has-spoken turn said "react to what they expressed" with nothing
// on the lord's own side. If these break, every companion's remark is met with applause whatever the lord thinks.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class WitnessMainSpeakerJudgesPromptTests
   {
      private static string Build(LeanPromptLevel level, bool witnessJustSpoke)
      {
         var npc = new NpcProfile {Id = "npc_test", Name = "Lord Olek", Faction = "Vlandia", Clan = "dey Meroc"};
         var context = new EncounterContext {
            LeanLevel = level,
            IsWitnessExchangeTurn = witnessJustSpoke,
            Witnesses = new[] {
               new WitnessEntry {Name = "Brienne", RelationToNpc = "the player's companion", IsPlayerCompanion = true, IsFemale = true}
            }
         };

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      // STAKE: the lord applauds a companion's insolence because nothing in the prompt says he may do otherwise.
      [Test]
      public void GIVEN_a_witness_in_the_room_WHEN_the_prompt_is_built_THEN_the_main_speaker_is_told_a_witness_is_not_right_for_standing_by_the_player()
      {
         string prompt = Build(LeanPromptLevel.Full, false);

         prompt.Should().Contain("IS NOT RIGHT FOR STANDING BESIDE THE PLAYER");
         prompt.Should().Contain("agree, disagree, rebuke, dismiss or ignore");
         prompt.Should().Contain("A sharp or insolent remark from someone of lower station is answered");
      }

      // STAKE: the witness's own written reaction presumes the lord will side with them.
      [Test]
      public void GIVEN_a_witness_in_the_room_WHEN_the_prompt_is_built_THEN_it_says_not_to_assume_the_speaker_takes_their_side()
      {
         Build(LeanPromptLevel.Full, false).Should().Contain("without assuming you");
      }

      // STAKE: the turn right after a companion speaks is where the praise actually lands.
      [Test]
      public void GIVEN_a_witness_has_just_spoken_WHEN_the_prompt_is_built_THEN_the_turn_says_the_speaker_need_not_agree_or_praise()
      {
         string prompt = Build(LeanPromptLevel.Full, true);

         prompt.Should().Contain("A WITNESS HAS JUST SPOKEN");
         prompt.Should().Contain("You are not bound to agree with the witness or to praise them");
         prompt.Should().Contain("Disagree, rebuke or dismiss a");
      }

      // STAKE: Lean (small models) drops the long teaching, and with it the counterweight, so praise comes back.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_one_line_counterweight_survives()
      {
         Build(LeanPromptLevel.Lean, false).Should().Contain("not as an ally of the player");
      }

      // froggyluv (Nexus, 04/11/2026): one companion swore to a broken promise "and generally the other companions will
      // jump in and all call me untrustworthy... things that never happened". The witnesses' reactions are written in the
      // same reply, so one invented wrong became a chorus.

      // STAKE: the companions in the room pile onto an accusation none of them could know is true.
      [Test]
      public void GIVEN_witnesses_in_the_room_WHEN_the_prompt_is_built_THEN_a_witness_never_vouches_for_a_wrong_they_did_not_see()
      {
         Build(LeanPromptLevel.Full, false).Should().Contain("A WITNESS VOUCHES ONLY FOR WHAT THEY KNOW");
         Build(LeanPromptLevel.Lean, false).Should().Contain("No witness vouches for a wrong they did not see.");
      }

      // STAKE: the turn after a companion accuses the player, the speaker takes the accusation up as a known fact.
      [Test]
      public void GIVEN_a_witness_has_just_spoken_WHEN_the_prompt_is_built_THEN_their_accusation_is_their_claim()
      {
         Build(LeanPromptLevel.Full, true).Should().Contain("A wrong the witness lays at the player's door is their claim");
      }
   }
}

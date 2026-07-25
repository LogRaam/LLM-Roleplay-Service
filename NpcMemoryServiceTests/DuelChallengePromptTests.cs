// Code written by Gabriel Mailhot, 22/07/2026.
// Duels pillar, the chat entry point. Until now nothing in a conversation could start a duel: the mission
// launcher, the outcome applier and five pure policies (DuelChallengePolicy, DuelAcceptancePolicy,
// DuelStakesPolicy, DuelOutcomePolicy, CourtshipDuelPolicy) all existed and were reachable only from the
// console. AppendDuelChallenge is the verb that connects them. Two things about it are load-bearing.
// First, the conditional teaching: the section renders ONLY when the host has already ruled the duel
// admissible AND found a venue, the same contract as RECRUITMENT, so the NPC can never agree to a field the
// game will not open (the empty-promise class of bug this codebase keeps closing). Second, the STOP AT THE
// CHALLENGE rule: whether the NPC takes the field is DuelAcceptancePolicy's call, weighed from their honour,
// valour, fear and grudges, so a model that answered "I accept" inside its own line would make that whole
// policy decorative and hand the decision back to the LLM.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class DuelChallengePromptTests
   {
      private const string DuelHeading = "A MATTER OF HONOUR MAY BE SETTLED WITH STEEL:";
      private const string DuelActionType = "type: challenge_duel";
      private const string StopRule = "STOP AT THE CHALLENGE.";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The feature itself: with the host's verdict in hand, the verb must reach the model, or a player who
      // calls a lord out gets a fine speech and no fight (the exact shape of the bug reported for hiring,
      // marriage and prisoner execution before it).
      [Test]
      public void GIVEN_the_host_has_ruled_a_duel_admissible_WHEN_built_THEN_the_challenge_verb_is_taught()
      {
         string prompt = Build(new EncounterContext {DuelChallengeAvailable = true});

         prompt.Should().Contain(DuelHeading);
         prompt.Should().Contain(DuelActionType);
      }

      // The other half of the conditional contract. DuelChallengePolicy refuses a duel for seven distinct
      // reasons (kin, a commoner, captivity, not face to face, cooldown, either party unable), and the host
      // collapses all of them into this one flag. If the section rendered anyway, every refusal would become
      // an NPC promising steel the game would then decline to stage.
      [Test]
      public void GIVEN_an_ordinary_conversation_WHEN_built_THEN_the_challenge_verb_is_absent()
      {
         string prompt = Build(new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Free});

         prompt.Should().NotContain(DuelHeading);
         prompt.Should().NotContain(DuelActionType);
      }

      // Player report (2026-07-25): a player negotiated a duel with a companion and the model, having no
      // challenge_duel action to emit (the target was not yet eligible), simply PLAYED THE FIGHT OUT in prose.
      // Withholding the verb (the test above) is not enough on its own: a fight is not a promise of a future
      // deed, it is an event the model can narrate as if it already happened. So the standing anti-empty-promise
      // rule must ALSO forbid narrating a duel when no challenge action is offered, or an ineligible target
      // still gets a fake fight. This pins that guard, which is present in every ordinary conversation.
      [Test]
      public void GIVEN_no_duel_is_available_WHEN_built_THEN_the_npc_is_forbidden_from_narrating_a_fight()
      {
         string prompt = Build(new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Free});

         prompt.Should().Contain("A FIGHT is the same");
         prompt.Should().Contain("unless the challenge action is offered above");
      }

      // Redundancy, deliberately: DuelChallengePolicy already returns InCaptivity, so the host should never
      // set the flag for a prisoner. The guard exists so a bug in the host cannot put a captor and their own
      // captive on a field of honour as equals, which is the one refusal that is a design statement rather
      // than a mechanical limit.
      [Test]
      public void GIVEN_the_player_is_this_npcs_prisoner_WHEN_the_flag_is_set_anyway_THEN_the_verb_is_still_withheld()
      {
         string prompt = Build(new EncounterContext {
            DuelChallengeAvailable = true,
            PlayerStatus = PlayerStatusVsNpc.Captive
         });

         prompt.Should().NotContain(DuelHeading);
      }

      // The line that keeps the decision in Logic. DuelAcceptancePolicy weighs a grudge at +40, honour and
      // valour per level, fear as a pull away and a Calculating nature as an exit: none of that means
      // anything if the model has already written its own answer into the challenge line.
      [Test]
      public void GIVEN_the_challenge_is_taught_WHEN_built_THEN_the_npc_is_told_not_to_answer_it_itself()
      {
         string prompt = Build(new EncounterContext {DuelChallengeAvailable = true});

         prompt.Should().Contain(StopRule);
      }

      // The duel mission knocks the loser unconscious and never kills (DuelMissionLauncher's own contract).
      // A model left to imagine the stakes writes a death, and the player then meets the "slain" lord alive
      // the next day: the fiction must state the mechanic it actually has.
      [Test]
      public void GIVEN_the_challenge_is_taught_WHEN_built_THEN_the_duel_is_declared_non_lethal()
      {
         string prompt = Build(new EncounterContext {DuelChallengeAvailable = true});

         prompt.Should().Contain("NOT to the death");
      }

      // The host owns the venue (DuelVenuePolicy: gates in a settlement, a field on open land, nothing at
      // sea), so a place named in the NPC's own voice is one the game will really open. Without the label
      // the place is simply left unstated rather than invented.
      [Test]
      public void GIVEN_a_venue_label_WHEN_built_THEN_the_npc_is_given_the_place_the_host_chose()
      {
         string prompt = Build(new EncounterContext {
            DuelChallengeAvailable = true,
            DuelVenueLabel = "at the gates of this town"
         });

         prompt.Should().Contain("at the gates of this town");
      }
   }
}

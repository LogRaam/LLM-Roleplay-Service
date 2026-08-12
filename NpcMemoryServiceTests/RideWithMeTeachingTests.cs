// Code written by Gabriel Mailhot, 11/08/2026.
// ride_with_me (1:1 bridge action): the idle-lord complement of follow_me. A lord who does NOT lead a field
// party of their own agrees to ride IN the player's party for a time, staying in their OWN clan (no marriage,
// no clan-join). These tests pin the SAME conditional-teaching contract follow_me carries (FollowMeTeachingTests):
// ride_with_me is taught only when EncounterContext.NpcCanRideWithPlayer is true, never otherwise (a model must
// not invent a retainer the bridge cannot honour), and its mirror part_ways only while NpcIsRidingWithPlayer is
// true. Above all they pin the carve-outs the host asked for: NEVER in a captive, council or round-table turn.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class RideWithMeTeachingTests
   {
      private const string RideWithMeHeading = "YOU MAY JOIN THEIR PARTY FOR A WHILE";
      private const string RideWithMeActionType = "type: ride_with_me";
      private const string PartWaysHeading = "YOU CURRENTLY RIDE IN THE PLAYER'S PARTY";
      private const string PartWaysActionType = "type: part_ways";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The core deed the verb exists for: an idle lord who can genuinely honour it must be taught the actual
      // action that puts them in the player's party, or "ride with me" stays an empty promise the model narrates
      // but nothing carries out.
      [Test]
      public void GIVEN_the_npc_can_ride_with_the_player_WHEN_built_THEN_ride_with_me_is_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcCanRideWithPlayer = true
         });

         prompt.Should().Contain(RideWithMeHeading);
         prompt.Should().Contain(RideWithMeActionType);
      }

      // The other half of the conditional-teaching contract, and the whole reason the feature is gated OFF by
      // default: when the host has not confirmed (and enabled) the retainer path, the model must never be handed
      // an action the bridge would only refuse.
      [Test]
      public void GIVEN_the_npc_cannot_ride_with_the_player_WHEN_built_THEN_ride_with_me_is_not_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcCanRideWithPlayer = false
         });

         prompt.Should().NotContain(RideWithMeHeading);
         prompt.Should().NotContain(RideWithMeActionType);
      }

      // Symmetric with follow_me's own captive carve-out: a captor holding the player prisoner is not inviting
      // them to ride along, even if the underlying eligibility fact happens to be true.
      [Test]
      public void GIVEN_the_player_is_a_captive_WHEN_built_THEN_ride_with_me_is_not_taught_even_if_eligible()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            NpcCanRideWithPlayer = true
         });

         prompt.Should().NotContain(RideWithMeHeading);
      }

      // The council/round-table carve-out the host required: a governance turn owns its own [RESOLUTION] channel,
      // so a personal "ride with me" offer must never leak into it, even when the lord is otherwise eligible.
      [Test]
      public void GIVEN_a_round_table_turn_WHEN_built_THEN_ride_with_me_is_not_taught_even_if_eligible()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcCanRideWithPlayer = true,
            IsRoundTableTurn = true
         });

         prompt.Should().NotContain(RideWithMeHeading);
      }

      // part_ways' own half of the contract: while a retainer is genuinely riding along, the player (or the lord
      // themselves) must be able to end it, or the arrangement could never be dissolved in conversation.
      [Test]
      public void GIVEN_the_npc_is_currently_riding_with_the_player_WHEN_built_THEN_part_ways_is_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcIsRidingWithPlayer = true
         });

         prompt.Should().Contain(PartWaysHeading);
         prompt.Should().Contain(PartWaysActionType);
      }

      // No active retainer, no parting to offer: teaching part_ways here would hand the model an action the
      // bridge has nothing to end.
      [Test]
      public void GIVEN_the_npc_is_not_riding_with_the_player_WHEN_built_THEN_part_ways_is_not_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcIsRidingWithPlayer = false
         });

         prompt.Should().NotContain(PartWaysHeading);
         prompt.Should().NotContain(PartWaysActionType);
      }
   }
}

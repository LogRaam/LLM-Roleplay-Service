// Code written by Gabriel Mailhot, 03/08/2026.
// The council resolution catalogue gained a second LIVE executor (appoint_governor, CouncilLift.SettleAppointGovernor)
// but the prompt only ever taught "type: quest", so the LLM never proposed a governorship even when the lift
// could have honoured it: an EXECUTABLE deed the table could never reach for. EncounterContext.CouncilOfferedResolutionKinds
// (populated by the mod's ResolutionOfferingResolver -> ResolutionEligibilityPolicy from live world facts) is the
// wire this closes: appoint_governor is taught ONLY when that list says the lift could actually honour it right
// now, so the model is never invited to promise a deed that fails the moment the council rises.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CouncilResolutionOfferingPromptTests
   {
      private const string AppointGovernorFormat = "type: appoint_governor";
      private const string AssignPartyRoleFormat = "type: assign_party_role";
      private const string RejoinPartyFormat = "type: rejoin_party";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The whole point of this wire: without appoint_governor in its own vocabulary the model has no way to
      // propose it, no matter how eligible the world is, exactly the dormant state this task closes.
      [Test]
      public void GIVEN_a_council_turn_with_appoint_governor_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "appoint_governor"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(AppointGovernorFormat);
         prompt.Should().Contain("GOVERN one of your fiefs");
         prompt.Should().Contain("only possible");
         prompt.Should().Contain("while the player still leads the clan");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no vacant fief, or no
      // available member) must not see appoint_governor at all: teaching it here would offer a governorship the
      // lift has already proven it cannot grant right now.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_appoint_governor_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(AppointGovernorFormat);
         // The existing quest teaching must survive untouched: this feature is additive, never a regression.
         prompt.Should().Contain("RECORDING WHAT THE TABLE DECIDES:");
         prompt.Should().Contain("type: quest");
      }

      // A null/empty offered-kinds list (every council caller before this feature, and the older whole-table
      // spike which never populates it) must fall back to exactly today's behaviour: quest taught, nothing else.
      [Test]
      public void GIVEN_a_council_turn_with_no_offered_kinds_set_WHEN_building_the_prompt_THEN_it_falls_back_to_quest_only()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(AppointGovernorFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a
      // governorship offer belongs only to a real council turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_appoint_governor_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "appoint_governor"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(AppointGovernorFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // The second executable menu motion (2026-08-03), mirroring appoint_governor's own wire: without
      // assign_party_role in its own vocabulary the model has no way to propose it, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_assign_party_role_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "assign_party_role"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(AssignPartyRoleFormat);
         prompt.Should().Contain("target_role:");
         prompt.Should().Contain("Scout, Engineer, Quartermaster, or Surgeon");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no seated member rides
      // in the player's party) must not see assign_party_role at all: teaching it would offer a role the lift
      // has already proven no one present could actually hold.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_assign_party_role_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(AssignPartyRoleFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a party
      // role offer belongs only to a real council turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_assign_party_role_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "assign_party_role"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(AssignPartyRoleFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // The THIRD executable menu motion (bug d, 2026-08-03), mirroring assign_party_role's own wire: without
      // rejoin_party in its own vocabulary the model has no way to propose bringing an away companion back,
      // whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_rejoin_party_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "rejoin_party"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(RejoinPartyFormat);
         prompt.Should().Contain("REJOIN it");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no seated companion is
      // out of the party) must not see rejoin_party at all: teaching it would offer a return the lift has
      // already proven nobody present needs.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_rejoin_party_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(RejoinPartyFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a rejoin
      // pledge belongs only to a real council turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_rejoin_party_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "rejoin_party"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(RejoinPartyFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }
   }
}

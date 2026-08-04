// Code written by Gabriel Mailhot, 03/08/2026.
// The council resolution catalogue gained a second LIVE executor (appoint_governor, CouncilLift.SettleAppointGovernor)
// but the prompt only ever taught "type: quest", so the LLM never proposed a governorship even when the lift
// could have honoured it: an EXECUTABLE deed the table could never reach for. EncounterContext.CouncilOfferedResolutionKinds
// (populated by the mod's ResolutionOfferingResolver -> ResolutionEligibilityPolicy from live world facts) is the
// wire this closes: appoint_governor is taught ONLY when that list says the lift could actually honour it right
// now, so the model is never invited to promise a deed that fails the moment the council rises.

#region

using System;
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
      private const string DispatchMissionFormat = "type: dispatch_mission";
      private const string GrantStipendFormat = "type: grant_stipend";
      private const string DeclareWarFormat = "type: declare_war";
      private const string MakePeaceFormat = "type: make_peace";
      private const string GiveGoldFormat = "type: give_gold";
      private const string GiveInfluenceFormat = "type: give_influence";
      private const string PledgeAgainstFormat = "type: pledge_against";
      private const string GrantFiefFormat = "type: grant_fief";
      private const string RevokeFiefFormat = "type: revoke_fief";
      private const string ExpelFromClanFormat = "type: expel_from_clan";
      private const string ArrangeMarriageFormat = "type: arrange_marriage";
      private const string TributeFormat = "type: tribute";

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

      // The FOURTH executable menu motion (2026-08-03), mirroring assign_party_role's own wire: without
      // dispatch_mission in its own vocabulary the model has no way to send a companion out on an existing
      // companion-mission errand, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_dispatch_mission_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "dispatch_mission"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(DispatchMissionFormat);
         prompt.Should().Contain("target_mission:");
         prompt.Should().Contain("GatherNews, Spy, Steal,");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no seated member rides in
      // the player's party) must not see dispatch_mission at all: teaching it would offer an errand the lift has
      // already proven nobody present could actually be sent on.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_dispatch_mission_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(DispatchMissionFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a
      // dispatch pledge belongs only to a real council turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_dispatch_mission_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "dispatch_mission"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(DispatchMissionFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // The FIFTH executable menu motion (2026-08-03), and the FIRST that touches persisted save state: without
      // grant_stipend in its own vocabulary the model has no way to propose funding a companion's purse, whatever
      // the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_grant_stipend_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "grant_stipend"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(GrantStipendFormat);
         prompt.Should().Contain("target_amount:");
         prompt.Should().Contain("50 and 500");
         prompt.Should().Contain("30 days");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (the player cannot afford
      // even the minimum tranche) must not see grant_stipend at all: teaching it would offer a purse the lift has
      // already proven the treasury cannot fund at all.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_grant_stipend_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(GrantStipendFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a stipend
      // offer belongs only to a real council turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_grant_stipend_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "grant_stipend"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(GrantStipendFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // The WarCouncil's first motion, and the first FACTION-CENTRIC kind (2026-08-03): without declare_war in
      // its own vocabulary the model has no way to propose a war declaration, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_declare_war_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "declare_war"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(DeclareWarFormat);
         prompt.Should().Contain("target_faction:");
         prompt.Should().Contain("Only the leader of a faction may declare war");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (the player does not lead
      // a faction that could declare war) must not see declare_war at all: teaching it would offer a decision
      // the lift has already proven the player has no standing to make.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_declare_war_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(DeclareWarFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation, and any non-council round-table turn, must NEVER see this teaching,
      // whatever the field happens to hold: a war declaration belongs only to a real WAR council turn.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_declare_war_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "declare_war"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(DeclareWarFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // The Parley's own motion, declare_war's symmetric counterpart (2026-08-03): without make_peace in its own
      // vocabulary the model has no way to propose ending the war, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_make_peace_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "make_peace"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(MakePeaceFormat);
         prompt.Should().Contain("Only the leader of a faction may make peace");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (the player does not lead
      // a faction at war with the parley's target) must not see make_peace at all: teaching it would offer a
      // decision the lift has already proven the player has no standing to make.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_make_peace_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(MakePeaceFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation, and any non-council round-table turn, must NEVER see this teaching,
      // whatever the field happens to hold: a peace offer belongs only to a real PARLEY turn.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_make_peace_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "make_peace"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(MakePeaceFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // Partie 1 (bringing the existing 1:1 resource verbs to the council, 2026-08-04): without give_gold in its
      // own vocabulary the model has no way to propose a seated lord's own gold pledge, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_give_gold_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "give_gold"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(GiveGoldFormat);
         prompt.Should().Contain("target_amount:");
         prompt.Should().Contain("PLEDGE GOLD TO YOU");
         prompt.Should().Contain("their own purse");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no seated member holds
      // any gold of their own) must not see give_gold at all: teaching it would offer a pledge the lift has
      // already proven nobody present could actually pay.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_give_gold_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(GiveGoldFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a gold
      // pledge from a seated lord belongs only to a real council turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_give_gold_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "give_gold"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(GiveGoldFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // Partie 1's other kind (bringing the existing 1:1 resource verbs to the council, 2026-08-04): without
      // give_influence in its own vocabulary the model has no way to propose a seated ally's own clan influence
      // pledge, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_give_influence_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "give_influence"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(GiveInfluenceFormat);
         prompt.Should().Contain("WAR COUNCIL");
         prompt.Should().Contain("CLAN'S INFLUENCE TO");
         prompt.Should().Contain("Do not name an");
      }

      // The STAKE this test pins: unlike give_gold's target_amount, the amount must NEVER be an LLM choice
      // (mirroring the 1:1 give_influence verb's own discipline, which the council amount policy reuses at the
      // lift). The taught block for this kind must never invite a target_amount field.
      [Test]
      public void GIVEN_give_influence_offered_WHEN_building_the_prompt_THEN_no_amount_field_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "give_influence"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         int blockStart = prompt.IndexOf(GiveInfluenceFormat, StringComparison.Ordinal);
         int blockEnd = prompt.IndexOf("[/RESOLUTION]", blockStart, StringComparison.Ordinal);
         string block = prompt.Substring(blockStart, blockEnd - blockStart);

         block.Should().NotContain("target_amount");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no seated ally's clan
      // could currently spare any influence) must not see give_influence at all: teaching it would offer a
      // pledge the lift has already proven nobody present could actually keep.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_give_influence_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(GiveInfluenceFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a clan
      // influence pledge from a seated ally belongs only to a real council turn, never to a private exchange
      // with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_give_influence_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "give_influence"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(GiveInfluenceFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // Partie 1's schemes kind (COUNCIL_ACTIONS.md, 2026-08-04), reusing the existing 1:1 pledge_against system
      // end to end: without pledge_against in its own vocabulary the model has no way to propose a seated
      // member's own scheme against a rival, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_pledge_against_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "pledge_against"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(PledgeAgainstFormat);
         prompt.Should().Contain("target_rival:");
         prompt.Should().Contain("MOVE AGAINST A RIVAL");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no seated member without
      // an outstanding pledge) must not see pledge_against at all: teaching it would offer a scheme the lift has
      // already proven nobody present could actually launch.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_pledge_against_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(PledgeAgainstFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a
      // council-decided scheme belongs only to a real council turn, never to a private exchange with one NPC
      // (the 1:1 pledge_against action already covers that case on its own vocabulary).
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_pledge_against_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "pledge_against"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(PledgeAgainstFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // Partie 2 (COUNCIL_ACTIONS.md, 2026-08-04), the FIRST governance kind to gain a real executor: without
      // grant_fief in its own vocabulary the model has no way to propose granting a crown fief, whatever the
      // world allows.
      [Test]
      public void GIVEN_a_council_turn_with_grant_fief_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "grant_fief"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(GrantFiefFormat);
         prompt.Should().Contain("target_settlement:");
         prompt.Should().Contain("RULES a kingdom");
         prompt.Should().Contain("GRANTED to a seated vassal");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no kingdom to rule, or
      // no giveable crown fief) must not see grant_fief at all: teaching it would offer a gift the lift has
      // already proven impossible.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_grant_fief_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(GrantFiefFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: granting
      // a fief belongs only to a real WAR COUNCIL turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_grant_fief_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "grant_fief"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(GrantFiefFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // Partie 2's other governance kind (COUNCIL_ACTIONS.md, 2026-08-04), grant_fief's grim mirror: without
      // revoke_fief in its own vocabulary the model has no way to threaten stripping a vassal's fief, whatever
      // the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_revoke_fief_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "revoke_fief"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(RevokeFiefFormat);
         prompt.Should().Contain("target_settlement:");
         prompt.Should().Contain("STRIPPED of a fief");
         prompt.Should().Contain("gravest punishments");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no kingdom to rule, or
      // no seated vassal holds any fief) must not see revoke_fief at all: teaching it would voice a threat the
      // lift has already proven hollow.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_revoke_fief_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(RevokeFiefFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: revoking
      // a vassal's fief belongs only to a real WAR COUNCIL turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_revoke_fief_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "revoke_fief"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(RevokeFiefFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // Partie 8 (COUNCIL_ACTIONS.md's Kimi review, 2026-08-04), the council's harshest INTERNAL sanction:
      // without expel_from_clan in its own vocabulary the model has no way to propose casting a companion out
      // of the clan, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_expel_from_clan_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "expel_from_clan"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(ExpelFromClanFormat);
         prompt.Should().Contain("CAST OUT OF THE CLAN");
         prompt.Should().Contain("Only the clan's own chief");
         prompt.Should().Contain("permanent");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no clan leadership, or no
      // expellable companion seated) must not see expel_from_clan at all: teaching it would voice a threat the
      // lift has already proven hollow.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_expel_from_clan_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(ExpelFromClanFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: casting a
      // companion out of the clan belongs only to a real council turn, never to a private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_expel_from_clan_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "expel_from_clan"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(ExpelFromClanFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // Partie 1 (COUNCIL_ACTIONS.md, "juste arrange_mariage qui est valide", 2026-08-04), REUSING the existing
      // 1:1 arrange_marriage system end to end: without arrange_marriage in its own vocabulary the model has no
      // way to propose a marriage alliance at the table, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_arrange_marriage_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "arrange_marriage"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(ArrangeMarriageFormat);
         prompt.Should().Contain("player_kin:");
         prompt.Should().Contain("target_kin:");
         prompt.Should().Contain("MARRIAGE ALLIANCE");
      }

      // The visible price the Kimi review demanded (COUNCIL_ACTIONS.md): the taught block must warn that the
      // match moves one kin's own allegiance into the other house, so the model never voices this as a costless
      // gift the way it would voice ordinary goodwill.
      [Test]
      public void GIVEN_arrange_marriage_offered_WHEN_building_the_prompt_THEN_the_clan_transfer_price_is_stated()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "arrange_marriage"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("move one of the two named kin into the other house");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no unwed kin on either
      // side) must not see arrange_marriage at all: teaching it would offer a match the lift has already proven
      // nobody present could actually make.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_arrange_marriage_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(ArrangeMarriageFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation must NEVER see this teaching, whatever the field happens to hold: a
      // council-arranged marriage belongs only to a real WAR COUNCIL turn (the 1:1 arrange_marriage action
      // already covers the private exchange on its own vocabulary).
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_arrange_marriage_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "arrange_marriage"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(ArrangeMarriageFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }

      // Partie 8 (COUNCIL_ACTIONS.md's Kimi review, "tribute", 2026-08-04), the Parley's own submission/buy-off
      // motion: without tribute in its own vocabulary the model has no way to propose the seated enemy leader's
      // own daily payment, whatever the world allows.
      [Test]
      public void GIVEN_a_council_turn_with_tribute_offered_WHEN_building_the_prompt_THEN_its_emission_format_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest", "tribute"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(TributeFormat);
         prompt.Should().Contain("target_amount:");
         prompt.Should().Contain("DAILY");
         prompt.Should().Contain("TRIBUTE");
         prompt.Should().Contain("50 and 2000");
         prompt.Should().Contain("their OWN coffers");
      }

      // A council whose world facts satisfy nothing beyond the universal quest pledge (no seated counterpart
      // leads a house distinct from the player's own) must not see tribute at all: teaching it would offer a
      // pledge the lift has already proven nobody present could actually owe.
      [Test]
      public void GIVEN_a_council_turn_with_only_quest_offered_WHEN_building_the_prompt_THEN_tribute_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            CouncilOfferedResolutionKinds = new[] {"quest"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(TributeFormat);
         prompt.Should().Contain("type: quest");
      }

      // An ordinary 1:1 conversation, and any non-council round-table turn, must NEVER see this teaching,
      // whatever the field happens to hold: a tribute pledge belongs only to a real PARLEY turn, never to a
      // private exchange with one NPC.
      [Test]
      public void GIVEN_an_ordinary_non_council_turn_WHEN_building_the_prompt_THEN_tribute_is_never_mentioned()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CouncilOfferedResolutionKinds = new[] {"quest", "tribute"}
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(TributeFormat);
         prompt.Should().NotContain("RECORDING WHAT THE TABLE DECIDES:");
      }
   }
}

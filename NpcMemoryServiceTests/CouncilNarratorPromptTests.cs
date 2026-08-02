// Code written by Gabriel Mailhot, 02/08/2026.
// Council narrator model: a real, player-led council no longer reuses the 1:1/whole-table framing (one NPC
// leads and answers, the others react as witnesses). EncounterContext.IsCouncilNarratorTurn swaps that framing
// for a NARRATOR one: the LLM describes the scene and gives the floor to whichever seated member(s) are
// genuinely moved to speak, attributed via [WITNESS_REACTION]; the profile backing the call (the anchor) is
// merely one more seated member and may use [DIALOGUE], never privileged. Every other council rule sharing the
// IsRoundTableTurn block (NO DEED IS SEALED, [RESOLUTION], the regard-shift ACTION) is unchanged and already
// covered by RoundTableTurnPromptTests/CouncilActorAttributionPromptTests; this file covers only what changes.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CouncilNarratorPromptTests
   {
      private const string NarratorHeader = "YOU ARE THE NARRATOR OF THIS COUNCIL, NOT ITS PRESIDING VOICE:";
      private const string OldWholeTableHeader = "THE COUNCIL PRESENT (each will speak in turn):";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The whole point of the reshape: no single NPC leads and answers while the rest merely react. Without
      // this header (and the old one gone), the model still reads "the council present, each will speak in
      // turn" and keeps treating the anchor as the presiding voice, exactly the model Gabriel asked to replace.
      [Test]
      public void GIVEN_a_council_narrator_turn_WHEN_building_the_prompt_THEN_the_narrator_framing_replaces_the_whole_table_one()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true, IsCouncilNarratorTurn = true};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(NarratorHeader);
         prompt.Should().NotContain(OldWholeTableHeader);
      }

      // The older whole-table/per-member spike (cr.council_test) and the unrelated ordinary round-table lap
      // both set IsRoundTableTurn without IsCouncilNarratorTurn; they must keep reading the original text
      // untouched, or every existing round-table caller silently changes behaviour underneath it.
      [Test]
      public void GIVEN_a_round_table_turn_without_the_narrator_flag_WHEN_building_the_prompt_THEN_the_whole_table_framing_is_unchanged()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(OldWholeTableHeader);
         prompt.Should().NotContain(NarratorHeader);
      }

      // Ratified 2026-07-24's council afterlife (NO DEED IS SEALED / [RESOLUTION] / the regard-shift ACTION)
      // must survive the reshape untouched: only HOW the reply is voiced changes, never what a council may
      // decide or record. Regression guard shared with RoundTableTurnPromptTests' equivalent assertions.
      [Test]
      public void GIVEN_a_council_narrator_turn_WHEN_building_the_prompt_THEN_the_existing_council_rules_still_apply()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true, IsCouncilNarratorTurn = true};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("NO DEED IS SEALED AT THIS TABLE");
         prompt.Should().Contain("RECORDING WHAT THE TABLE DECIDES:");
         prompt.Should().Contain("[RESOLUTION]");
      }

      // The anchor must never read as privileged: they are taught to use [DIALOGUE] like any other seated
      // member, optionally, not as the one who necessarily replies every turn.
      [Test]
      public void GIVEN_a_council_narrator_turn_WHEN_building_the_prompt_THEN_the_anchor_is_taught_as_an_unprivileged_seated_member()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true, IsCouncilNarratorTurn = true};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("You are not privileged");
         prompt.Should().Contain("[WITNESS_REACTION]");
      }

      // The exception ratified with Gabriel: addressing a member by name guarantees them a line this turn.
      // Without this directive the narrator could give the floor to whoever it likes and ignore a direct
      // question, which reads as the game not hearing the player.
      [Test]
      public void GIVEN_the_player_addressed_a_member_by_name_WHEN_building_the_prompt_THEN_that_member_must_answer()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            AddressedCouncilMemberName = "Ira"
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("THE PLAYER ADDRESSED IRA DIRECTLY THIS TURN:");
         prompt.Should().Contain("Ira must answer this turn");
      }

      // Absent an addressed member, the directive must not appear at all: an empty/blank name is exactly the
      // ordinary case (nobody was addressed by name this turn) and must render nothing.
      [Test]
      public void GIVEN_no_member_was_addressed_WHEN_building_the_prompt_THEN_the_must_answer_directive_is_absent()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true, IsCouncilNarratorTurn = true};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain("must answer this turn");
      }

      // AppendWitnesses' seat-list heading is shared machinery: it must stop asserting "each speaks in turn" on
      // a narrator turn, which would directly contradict the new "not everyone speaks every turn" framing above.
      [Test]
      public void GIVEN_a_council_narrator_turn_WHEN_building_the_prompt_THEN_the_seat_list_heading_matches_natural_pacing()
      {
         var witnesses = new[] {
            new WitnessEntry {Name = "Ira", HeroStringId = "hero_ira", RelationToNpc = "a companion in the player's service"}
         };
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true,
            Witnesses = witnesses
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("AT THIS TABLE WITH YOU (you among them; not everyone speaks every turn):");
      }
   }
}

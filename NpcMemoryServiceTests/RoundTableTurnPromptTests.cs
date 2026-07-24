// Code written by Gabriel Mailhot, 10/07/2026.
// Round-table group mode: EncounterContext.IsRoundTableTurn opens the floor to everyone present, so on
// that turn PromptBuilder injects a "ROUND TABLE:" block telling the NPC to speak at more length and that
// they may address the other people present, not only the player. False by default, so the block is absent.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class RoundTableTurnPromptTests
   {
      // Round-table audit D5 renamed this block. "ROUND TABLE:" sat beside a WITNESSES PRESENT section that
      // framed the same people as eavesdroppers to guard your tongue around, so the model received two
      // contradictory readings of the room. The heading now names them as participants.
      private const string Header = "THE COUNCIL PRESENT (each will speak in turn):";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // Without this block the NPC keeps its normal one-on-one habits: short replies, deferring back to the
      // player. The round-table mode needs the opposite, a full contribution that may address other people
      // present, or the group scene reads as a series of disconnected one-line answers.
      [Test]
      public void GIVEN_a_round_table_turn_WHEN_building_the_prompt_THEN_the_round_table_block_is_injected()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Header);
      }

      // False by default: an ordinary two-person conversation must never be told to "address the other people
      // present", which would invite the model to invent participants who are not actually there.
      [Test]
      public void GIVEN_no_round_table_turn_WHEN_building_the_prompt_THEN_the_round_table_block_is_absent()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(Header);
      }

      // Council audit C3 (ratified 2026-07-24): a DEED is never carried out at the table. This used to be a
      // flat "emit no [ACTION] at all", which contradicted a second block that invited an actor-attributed
      // [ACTION], and which the whole-table dispatch enforced for neither. The invariant that survives is
      // narrower and truthful: no gold/troops/deed is sealed here; a real commitment is a [RESOLUTION] settled
      // at the lift.
      [Test]
      public void GIVEN_a_round_table_turn_WHEN_building_the_prompt_THEN_no_deed_is_sealed_and_commitments_defer_to_a_resolution()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true});

         prompt.Should().Contain("NO DEED IS SEALED AT THIS TABLE");
         prompt.Should().Contain("no gold changes hands");
         prompt.Should().Contain("RECORDED below as a");
      }

      // The other half of the ratified split (2026-07-24): PERCEPTION is immediate. A lord angered at the
      // council IS angrier at once, and that must colour how he weighs every later request, so the prompt must
      // explicitly permit the regard change it once forbade. The bridge enforces the same split (only
      // change_relation runs on the spot at a council), so prompt and code now agree.
      [Test]
      public void GIVEN_a_round_table_turn_WHEN_building_the_prompt_THEN_a_shift_in_regard_is_permitted_immediately()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true});

         prompt.Should().Contain("YOUR REGARD IS REAL");
         prompt.Should().Contain("Register it as a");
         prompt.Should().Contain("change_relation");
      }

      // The council's mechanical afterlife (this task): a spoken pledge used to have no channel at all, which
      // is exactly what made the model either ignore the prohibition or go vague. [RESOLUTION] gives it a real,
      // concrete shape to record a commitment in (settled at the lift, not here), plus the withdrawal path the
      // owner asked for INSTEAD of a manual delete button (a change of mind belongs in the conversation itself).
      [Test]
      public void GIVEN_a_round_table_turn_WHEN_building_the_prompt_THEN_the_resolution_block_is_taught()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true});

         prompt.Should().Contain("RECORDING WHAT THE TABLE DECIDES:");
         prompt.Should().Contain("[RESOLUTION]");
         prompt.Should().Contain("actor: <the member's name exactly as listed above>");
         prompt.Should().Contain("type: withdraw");
      }

      // Council audit C2: target_settlement is REQUIRED to ground the only executable resolution kind (the lift
      // binds the quest to it), the parser read it, yet the teaching block never showed it, so the most natural
      // council pledge ("ride to X and clear the bandits") failed structurally at the lift. It must now be
      // taught, and named as required, or the one working kind stays unreachable except by the model's luck.
      [Test]
      public void GIVEN_a_round_table_turn_WHEN_building_the_prompt_THEN_the_resolution_teaches_the_required_target_settlement()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true});

         prompt.Should().Contain("target_settlement:");
         prompt.Should().Contain("The target_settlement is REQUIRED");
      }

      // The attributed-transcript convention is what lets several speakers share one room, and it was taught
      // nowhere: a weaker model could read "Name: ..." as the PLAYER quoting somebody, or answer on another
      // participant's behalf. Both break the illusion the council depends on.
      [Test]
      public void GIVEN_a_round_table_turn_WHEN_building_the_prompt_THEN_the_attributed_transcript_is_explained()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsRoundTableTurn = true});

         prompt.Should().Contain("what THAT person said aloud in this room");
         prompt.Should().Contain("Speak ONLY as yourself");
      }
   }
}

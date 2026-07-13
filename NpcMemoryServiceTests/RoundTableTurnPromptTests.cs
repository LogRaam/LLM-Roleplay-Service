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
      private const string Header = "ROUND TABLE:";

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
   }
}

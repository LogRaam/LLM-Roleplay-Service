// Code written by Gabriel Mailhot, 02/09/2026.
// The give_item teaching lets an NPC accept a gift the player hands over. A player reported that gifting took TWO
// NPC reactions, one to the offer and a second to the actual receipt, because the offer read as a proposal to
// deliberate rather than a completed handover. The guard added here tells the model that when the player hands an
// item over as a done act, it is RECEIVED this turn: react to holding it and emit the action in the same reply,
// never stalling the gift into a second turn. These tests pin that teaching and its captive exclusion.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GiveItemPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "test_hero",
         Name = "Derthert",
         Clan = "dey Meroc",
         Faction = "Vlandia"
      };

      private static string Build(EncounterContext ctx)
         => new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState(), ctx);

      // Without the teaching an NPC has no vocabulary to accept a gift, so an ordinary conversation must carry it.
      [Test]
      public void GIVEN_an_ordinary_conversation_WHEN_building_the_prompt_THEN_the_gift_action_is_taught()
      {
         string prompt = Build(new EncounterContext());

         prompt.Should().Contain("ITEM OFFER");
         prompt.Should().Contain("type: give_item");
      }

      // THE PLAYER REPORT: a gift cost TWO NPC reactions (one to the offer, one to the receipt). The guard must
      // teach that a completed handover lands in ONE turn: received now, the action emitted this reply, not stalled.
      [Test]
      public void GIVEN_the_player_hands_an_item_over_WHEN_building_the_prompt_THEN_it_is_taught_to_land_in_one_turn()
      {
         string prompt = Build(new EncounterContext());

         prompt.Should().Contain("treat it as RECEIVED");
         prompt.Should().Contain("in THIS reply");
         prompt.Should().Contain("never stalling the gift into a second turn");
      }

      // A prisoner cannot reach their inventory, so the whole gift teaching (guard included) must be absent for a
      // captive player, or a captor would be taught to accept gifts the prisoner cannot actually hand over.
      [Test]
      public void GIVEN_a_captive_player_WHEN_building_the_prompt_THEN_the_gift_teaching_is_absent()
      {
         string prompt = Build(new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Captive});

         prompt.Should().NotContain("ITEM OFFER");
      }
   }
}

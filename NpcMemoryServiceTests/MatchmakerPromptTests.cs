// Code written by Gabriel Mailhot, 25/07/2026.
// Pins PromptBuilder.AppendMatchmaker: the arrange_marriage offer that lets an NPC broker a match between
// one of the PLAYER's own unwed kin and one of the NPC's own unwed kin, an alliance of two houses in which
// the player is neither spouse. Distinct from the existing marriage-prospect block (the player themselves
// wedding into the NPC's house). Reported by CarolusIV, 2026-07-25: the player could not get the LLM to
// offer a match between a clan member and a foreign noble, because nothing in the prompt ever taught the
// action existed. The block must render ONLY when the host has supplied BOTH kin lists, since either half
// missing means the game cannot ground the offer (no giver, or no one of the player's own to give).

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
   public class MatchmakerPromptTests
   {
      // The offer must actually be legible as the arrange_marriage action, with the real names the game
      // resolved, so the LLM can emit a call the bridge can ground. This is the report itself: without this
      // block the LLM never knew the verb existed at all.
      [Test]
      public void GIVEN_both_kin_lists_present_WHEN_built_THEN_the_matchmaker_block_teaches_arrange_marriage()
      {
         string prompt = Build(playerKin: "Elara (age 22)", npcKin: "Baltoc (age 25)");

         prompt.Should().Contain("MATCHMAKER, YOU MAY ARRANGE A MATCH");
         prompt.Should().Contain("type: arrange_marriage");
         prompt.Should().Contain("Elara (age 22)");
      }

      // Half a match cannot be brokered: with no giver on the NPC's own side the game has nothing to hand
      // over, and teaching the action anyway would let the LLM promise a wedding the bridge will refuse.
      [Test]
      public void GIVEN_npc_has_no_kin_to_give_WHEN_built_THEN_arrange_marriage_is_not_taught()
      {
         string prompt = Build(playerKin: "Elara (age 22)", npcKin: null!);

         prompt.Should().NotContain("type: arrange_marriage");
      }

      // The mirror case: with no unwed kin on the player's OWN side there is no one to marry off, so the
      // offer must stay silent rather than invite a proposal the player cannot actually make good on.
      [Test]
      public void GIVEN_player_has_no_kin_to_offer_WHEN_built_THEN_arrange_marriage_is_not_taught()
      {
         string prompt = Build(playerKin: null!, npcKin: "Baltoc (age 25)");

         prompt.Should().NotContain("type: arrange_marriage");
      }

      #region private

      private static string Build(string playerKin, string npcKin)
      {
         NpcProfile npc = Npc();

         return new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
            .BuildSystemPrompt(npc, new WorldState {CurrentDay = 10},
               new EncounterContext {
                  MatchmakerPlayerKin = playerKin,
                  MatchmakerNpcKin = npcKin
               });
      }

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         Romantic = new RomanticProfile {
            Status = RomanticStatus.Curious,
            Orientation = SexualOrientation.BiCurious,
            Preferences = new List<RomanticPreference>()
         }
      };

      #endregion
   }
}

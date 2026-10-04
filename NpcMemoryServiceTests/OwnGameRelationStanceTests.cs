// Code written by Gabriel Mailhot, 04/10/2026.
// CURRENT STANCE under a per-hero relation mod. tashmetu (Nexus, 04/10/2026), author of Personal Relations: the
// "clan's standing" line carried the character's own game relation, so a lord at +100 there and 0 personal regard was
// played as a house that loved the player and a man who did not know him. The host now passes the clan leader's
// relation as the clan's standing and, only when it differs, the character's own (NpcProfile.OwnGameRelationWithPlayer).
// If the line were missing, the model would read the leader's warmth as the lord's; if it appeared without such a mod,
// every prompt would grow a line repeating the clan's standing.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class OwnGameRelationStanceTests
   {
      private static NpcProfile Npc(int? own) => new() {
         Id = "npc_test",
         Name = "Test Lady",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         ClanRelationWithPlayer = 100,
         OwnGameRelationWithPlayer = own,
         ReputationWithPlayer = 0
      };

      private static string Prompt(int? own)
         => new PromptBuilder().BuildSystemPrompt(Npc(own), new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = LeanPromptLevel.Full});

      // tashmetu's case: her father's house at +100, her own relation 0. She is told both, apart, so she does not speak
      // as one who loves the player.
      [Test]
      public void GIVEN_a_lady_whose_own_relation_differs_from_her_houses_WHEN_her_stance_is_written_THEN_she_is_told_her_own_apart()
      {
         string prompt = Prompt(0);

         prompt.Should().Contain("Your clan's standing toward this player (+100)");
         prompt.Should().Contain("Your own relation with this player, from what you have seen of them (0)");
      }

      // The base game, or a character with no relation of their own: the stance is exactly what it was.
      [Test]
      public void GIVEN_no_relation_of_her_own_WHEN_her_stance_is_written_THEN_no_extra_line_appears()
      {
         Prompt(null).Should().NotContain("Your own relation with this player");
      }
   }
}

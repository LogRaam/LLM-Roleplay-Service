// Code written by Gabriel Mailhot, 07/07/2026.
// Pins the quest-issuance teaching: the deed is self-verifying (no invented proof-item to carry back), and a
// hideout deed's computed bearing is surfaced to the giver so they can point the player to the right region.

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
   public class QuestPromptInstructionsTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildWithQuests(NpcProfile npc)
      {
         var builder = new PromptBuilder {EnableQuests = true};

         return builder.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10});
      }

      [Test]
      public void GIVEN_quests_are_enabled_WHEN_teaching_task_issuance_THEN_it_forbids_inventing_a_proof_item()
      {
         string prompt = BuildWithQuests(Npc());

         // The deed itself is the proof; the giver must never ask the player to bring back an object.
         prompt.Should().Contain("NO token, trophy");
      }

      [Test]
      public void GIVEN_an_outstanding_hideout_quest_with_a_bearing_WHEN_listing_the_players_quests_THEN_the_direction_is_surfaced()
      {
         var npc = new NpcProfile {
            Id = "npc_test",
            Name = "Test Lord",
            Faction = "Vlandia",
            Clan = "dey Meroc",
            ActiveQuests = new List<InformalQuest> {
               new() {
                  Type = QuestType.BanditHideout,
                  Description = "Clear the lair troubling my lands.",
                  DirectionHint = "north of Pravend",
                  Status = QuestStatus.Active
               }
            }
         };

         string prompt = BuildWithQuests(npc);

         prompt.Should().Contain("north of Pravend");
      }
   }
}

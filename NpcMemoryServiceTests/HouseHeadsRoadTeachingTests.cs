// Code written by Gabriel Mailhot, 14/11/2026.
// What this pins: a character who heads her house, facing a player who heads his, offers the road of leaving his house only
// while it exists, and never names a deed as the price of a wedding.
//
// Aporo (Discord, 14/11/2026, "Stuck in wedding that can't happen"): "I've got information that if i disband my clan it
// will happen, it removed my companions". His log: Captain Arwa, head of a minor house, was taught "if they would truly
// have you as a spouse, that they leave their own house to join yours" while no action could take that road for her; the
// model made up its price ("dissolve Banu Ara, dismiss your companions"), kept it in her memories, and he paid it. If this
// breaks, a character again sends the player to wreck his own house for a wedding the game will never seal.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class HouseHeadsRoadTeachingTests
   {
      private const string Road = "leave their own house to join yours";

      private static string Build(EncounterContext context)
      {
         PromptBuilder real = LeanRealPromptBudgetTests.RealBuilder();
         var builder = new PromptBuilder {
            ActionVocabulary = real.ActionVocabulary, WorldDescription = real.WorldDescription,
            BehaviorGuidelinesOverride = real.BehaviorGuidelinesOverride, AdultLevel = real.AdultLevel
         };

         return builder.BuildSystemPrompt(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // THE REPORT: no road for her, so none is offered, and no deed is named as its price.
      [Test]
      public void GIVEN_two_heads_of_houses_and_no_road_WHEN_the_teaching_is_built_THEN_leaving_the_house_is_not_offered()
      {
         string prompt = Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.LeadsTheirOwnHouse});

         prompt.Should().Contain("two heads of houses").And.NotContain(Road);
      }

      // THE REPORT: whatever the road, no character asks the player to disband his house or dismiss his companions.
      [Test]
      public void GIVEN_two_heads_of_houses_WHEN_the_teaching_is_built_THEN_no_deed_is_a_price_of_the_wedding()
      {
         Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.LeadsTheirOwnHouse})
            .Should().Contain("Never ask them to disband or dissolve their house, dismiss their companions");
      }

      // STAKE: where the road really exists (a ruler, an heir at hand), she still offers it.
      [Test]
      public void GIVEN_two_heads_of_houses_and_an_open_road_WHEN_the_teaching_is_built_THEN_leaving_the_house_is_offered()
      {
         Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.LeadsTheirOwnHouse, LeaveHouseRoadOpen = true})
            .Should().Contain(Road);
      }

      // STAKE: a road closed for a reason the player can mend is still named with what must change.
      [Test]
      public void GIVEN_two_heads_of_houses_and_a_road_closed_by_war_WHEN_the_teaching_is_built_THEN_the_road_and_its_obstacle_are_named()
      {
         Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.LeadsTheirOwnHouse, LeaveHouseClosedBecause = LeaveHouseObstacle.WarOrCaptivity})
            .Should().Contain(Road).And.Contain("at war or one of you is a");
      }
   }
}

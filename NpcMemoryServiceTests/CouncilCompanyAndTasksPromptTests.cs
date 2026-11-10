// Code written by Gabriel Mailhot, 10/11/2026.
// What these pin: a war council knows, as the game counts it, how many men the player leads and how long their food
// lasts, and which tasks the player already carries or has just done.
//
// bdionysis80 (Nexus, 10/11/2026), a war council about taking Hvalvik: "the characters continued insisting that the
// army was starving, that the supply wagons were nearly empty, and that villages needed to be raided", the quartermaster
// included, although the stores were full; and a second council set the very scouting a first one had seen done. A seat
// learned of the food only when it rode with the player, and between 7 and 21 days of food nothing was said at all, so
// the model filled the silence. If these fail, the table argues against the game's own count again, or sets the player
// the same task twice.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class CouncilCompanyAndTasksPromptTests
   {
      private static CouncilPromptInput Input(int? men = null, int? foodDays = null, IReadOnlyList<string>? tasks = null)
         => new() {
            Roster = new List<CouncilMemberInput> {new() {Name = "Athina"}},
            CompanyMen = men,
            CompanyFoodDays = foodDays,
            TasksAtHand = tasks ?? new List<string>()
         };

      // STAKE (bdionysis80): with the stores full, the table is told so plainly and told no raid is needed for food.
      [Test]
      public void GIVEN_a_company_with_food_for_weeks_WHEN_the_council_prompt_is_built_THEN_it_states_the_count_and_that_no_one_is_hungry()
      {
         string prompt = CouncilPromptBuilder.Build(Input(450, 60));

         prompt.Should().Contain("THE PLAYER'S COMPANY (the game's own count, true whatever anyone at the table believes): 450 men; food for 60 days.");
         prompt.Should().Contain("no one is hungry").And.Contain("no raid is needed for food");
      }

      // STAKE: the middle band, where nothing at all was said before, now says there is food enough.
      [Test]
      public void GIVEN_a_company_with_food_for_two_weeks_WHEN_the_council_prompt_is_built_THEN_it_says_there_is_food_enough()
      {
         CouncilPromptBuilder.Build(Input(450, 12)).Should().Contain("food for 12 days").And.Contain("There is food enough for now");
      }

      // STAKE: a company truly short of food is told so; the line follows the count, never a comfort.
      [Test]
      public void GIVEN_a_company_short_of_food_WHEN_the_council_prompt_is_built_THEN_it_says_so()
      {
         CouncilPromptBuilder.Build(Input(450, 3)).Should().Contain("will not last the week");
         CouncilPromptBuilder.Build(Input(450, 0)).Should().Contain("going hungry");
      }

      // STAKE (bdionysis80): the table sees the scouting already done, and is told not to set it again.
      [Test]
      public void GIVEN_a_task_already_done_WHEN_the_council_prompt_is_built_THEN_it_is_listed_with_the_rule_not_to_set_it_again()
      {
         string prompt = CouncilPromptBuilder.Build(Input(tasks: new List<string> {"Athina: scout an army of Sturgia, done: 1,257 men under Olek"}));

         prompt.Should().Contain("TASKS ALREADY SET THE PLAYER (the game's record): never set one of these again");
         prompt.Should().Contain("- Athina: scout an army of Sturgia, done: 1,257 men under Olek");
      }

      // STAKE: a council the mod gives no count and no tasks reads as before (an older host, a scene with no company).
      [Test]
      public void GIVEN_no_company_and_no_tasks_WHEN_the_council_prompt_is_built_THEN_neither_block_appears()
      {
         string prompt = CouncilPromptBuilder.Build(Input());

         prompt.Should().NotContain("THE PLAYER'S COMPANY").And.NotContain("TASKS ALREADY SET THE PLAYER");
      }
   }
}

// Code written by Gabriel Mailhot, 05/10/2026.
// give_item's quantity. fkasad (Nexus, 05/10/2026): "Take those ten ingots of Gromril", the lord thanked him for ten,
// and one left the inventory, nine stayed. The bridge already honours a "count" parameter (GiftQuantityPolicy), but the
// interpreter was never taught it: the catalog spec declared only "item" and described the deed as "one item". So the
// model could not say ten, and the game moved one. If this rule breaks, every gift of several goods moves a single one.

#region

using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GiveItemCountTeachingTests
   {
      // The interpreter learns that a gift of several of the same thing carries how many, under the name the bridge reads.
      [Test]
      public void GIVEN_the_interpreter_prompt_WHEN_it_teaches_a_gift_of_goods_THEN_it_teaches_how_many_under_count()
      {
         GameActionSpec spec = GameActionCatalog.All.First(s => s.Type == "give_item");

         spec.Parameters.Select(p => p.Name).Should().Contain("count", "the bridge reads action.Parameters[\"count\"]");
         ActionInterpreterPromptBuilder.StablePrefix.Should().Contain("count");
      }

      // The deed is no longer described as a single item, which told the model a gift of ten was a gift of one.
      [Test]
      public void GIVEN_the_gift_of_goods_WHEN_described_to_the_model_THEN_it_is_not_said_to_be_one_item()
      {
         GameActionCatalog.All.First(s => s.Type == "give_item").Description.Should().NotContain("gives one item");
      }
   }
}

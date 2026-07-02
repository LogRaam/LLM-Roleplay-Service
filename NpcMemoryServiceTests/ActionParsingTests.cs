// Code written by Gabriel Mailhot, 01/07/2026.

#region

using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Documents <see cref="SectionResponseParser" /> behaviour for the <c>[ACTION]</c>
   ///   section, which was previously untested.
   /// </summary>
   [TestFixture]
   public class ActionParsingTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      [Test]
      public void Single_action_with_type_only_is_parsed()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[ACTION]\ntype: imprison\n[/ACTION]";

         var result = _parser.Parse(raw);

         result.Actions.Should().HaveCount(1);
         result.Actions[0].Type.Should().Be("imprison");
         result.Actions[0].Context.Should().BeNull();
         result.Actions[0].Parameters.Should().BeEmpty();
      }

      [Test]
      public void Multiple_actions_are_all_parsed_in_order()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[ACTION]\ntype: give_money\namount: 100\n[/ACTION]\n" +
            "[ACTION]\ntype: recruit\n[/ACTION]";

         var result = _parser.Parse(raw);

         result.Actions.Should().HaveCount(2);
         result.Actions[0].Type.Should().Be("give_money");
         result.Actions[1].Type.Should().Be("recruit");
      }

      [Test]
      public void Action_with_context_and_parameters_passes_them_through()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[ACTION]\n" +
            "type: give_item\n" +
            "context: He offers his old blade.\n" +
            "item: longsword\n" +
            "quantity: 1\n" +
            "[/ACTION]";

         var result = _parser.Parse(raw);

         result.Actions.Should().HaveCount(1);
         result.Actions[0].Type.Should().Be("give_item");
         result.Actions[0].Context.Should().Be("He offers his old blade.");
         result.Actions[0].Parameters.Should().Contain("item", "longsword");
         result.Actions[0].Parameters.Should().Contain("quantity", "1");
         // "type" and "context" are consumed as dedicated fields, never duplicated into parameters.
         result.Actions[0].Parameters.Should().NotContainKey("type");
         result.Actions[0].Parameters.Should().NotContainKey("context");
      }

      [Test]
      public void Action_missing_type_is_silently_skipped()
      {
         // Documents current behaviour: a block with no "type:" line contributes no action
         // at all, rather than surfacing as a malformed/placeholder entry.
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[ACTION]\ncontext: no type here\n[/ACTION]\n" +
            "[ACTION]\ntype: recruit\n[/ACTION]";

         var result = _parser.Parse(raw);

         result.Actions.Should().HaveCount(1);
         result.Actions[0].Type.Should().Be("recruit");
      }
   }
}

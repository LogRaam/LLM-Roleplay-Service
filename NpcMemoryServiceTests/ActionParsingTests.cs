// Code written by Gabriel Mailhot, 01/07/2026.
//
// [ACTION] is the model's channel for literal game-state changes (give_money, recruit, marry...);
// the mod executes each parsed GameAction exactly once via BannerlordGameStateBridge.ExecuteAction.
// Unlike [QUEST], there is no ParsedResponse flag equivalent to QuestBlockMalformed for a garbled or
// type-less [ACTION] block: it is simply dropped. What this parser keeps or discards here IS the
// whole contract, there is no second chance downstream to warn the player an action failed to
// register.

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

      // Baseline: a type alone is enough to produce an executable action; Context and Parameters
      // must default to null/empty rather than throwing on a minimal, unadorned block.
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

      // Order matters: each action is executed once, in sequence, by the mod's main action loop.
      // If actions were reordered, merged, or one dropped, an intended sequence of game-state
      // changes (e.g. take payment, then recruit) could apply out of order or not at all.
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

      // The free-form Parameters dictionary is how each action type's specific data (item,
      // quantity...) reaches BannerlordGameStateBridge's per-type handling; "type" and "context"
      // leaking into it would corrupt any downstream lookup keyed on those same parameter names.
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

      // Also guards a real crash: GameAction.Type is used unguarded downstream
      // (Type.ToLowerInvariant() in ExecuteAction), so a null Type reaching that switch would
      // throw instead of being dropped here first. There is no [ACTION]-level equivalent of
      // QuestBlockMalformed, so this silent drop is the only outcome a garbled block gets.
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

// Code written by Gabriel Mailhot, 19/07/2026.
// Player report (Nexus, 2026-07-19): "In the dialogue box, I often see {"name": "change_relation",
// "parameters": {"delta": X}} ... This should be the NPC changing their regard, but no change actually
// occurs."
//
// Both halves of that sentence are one fault. A model tuned for tool-calling answers with a function CALL
// instead of our taught [ACTION] block; nothing parsed it, so the regard never moved, and nothing stripped
// it, so the character recited our plumbing aloud. Fixing only the display would silence a regard change
// the NPC plainly meant to make, which is the hollow-promise failure this codebase fights everywhere else.
// Fixing only the parse would leave the JSON in their mouth.
//
// Unlike RecoverStrayRelationChange (deliberately narrow, because reading intent out of prose IS a guess),
// this recovery is not restricted to one action: that JSON shape cannot occur in roleplay prose, so its
// presence is the intent. Safety does not rest here anyway. Every recovered action still passes through the
// host's bridge, which re-validates it, and a regard change is still capped and rate-limited by the
// RelationGate. The prompt proposes; the bridge rules.

#region

using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class JsonActionCallRecoveryTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      // THE REPORTED SHAPE, verbatim from the player. The deed must happen.
      [Test]
      public void GIVEN_a_relation_change_written_as_a_function_call_WHEN_parsing_THEN_the_action_is_recovered()
      {
         ParsedResponse parsed = _parser.Parse(
            "[DIALOGUE]You have my thanks.\n{\"name\": \"change_relation\", \"parameters\": {\"delta\": 5}}[/DIALOGUE]");

         GameAction action = parsed.Actions.Single();
         action.Type.Should().Be("change_relation");
         action.Parameters["delta"].Should().Be("5");
      }

      // The other half, and the one the player actually SAW: the character must not read the call aloud.
      [Test]
      public void GIVEN_a_function_call_in_the_spoken_line_WHEN_parsing_THEN_it_is_not_spoken()
      {
         ParsedResponse parsed = _parser.Parse(
            "[DIALOGUE]You have my thanks.\n{\"name\": \"change_relation\", \"parameters\": {\"delta\": 5}}[/DIALOGUE]");

         parsed.Dialogue.Should().NotContain("change_relation");
         parsed.Dialogue.Should().NotContain("parameters");
         parsed.Dialogue.Should().Contain("You have my thanks.");
      }

      // A negative delta is the same call, and the player reported seeing both signs. Pinned separately
      // because a sign dropped in parsing would turn an insult into a courtesy.
      [Test]
      public void GIVEN_a_negative_delta_WHEN_parsing_THEN_the_sign_survives()
      {
         ParsedResponse parsed = _parser.Parse(
            "[DIALOGUE]Get out.\n{\"name\": \"change_relation\", \"parameters\": {\"delta\": -8}}[/DIALOGUE]");

         parsed.Actions.Single().Parameters["delta"].Should().Be("-8");
      }

      // Not restricted to regard: a gift emitted as a call is just as lost, and just as visible, as a
      // relation change. The bridge re-validates it exactly as it would a properly emitted one.
      [Test]
      public void GIVEN_another_action_written_as_a_call_WHEN_parsing_THEN_it_is_recovered_too()
      {
         ParsedResponse parsed = _parser.Parse(
            "[DIALOGUE]Take this purse.\n{\"name\": \"give_gold\", \"parameters\": {\"amount\": 200}}[/DIALOGUE]");

         parsed.Actions.Single().Type.Should().Be("give_gold");
         parsed.Actions.Single().Parameters["amount"].Should().Be("200");
      }

      // Some models label the payload "arguments" instead of "parameters". Same intent, same recovery.
      [Test]
      public void GIVEN_the_arguments_spelling_WHEN_parsing_THEN_it_is_still_recovered()
      {
         _parser.Parse(
                                 "[DIALOGUE]Fine.\n{\"name\": \"change_relation\", \"arguments\": {\"delta\": 3}}[/DIALOGUE]")
                              .Actions.Single().Parameters["delta"].Should().Be("3");
      }

      // A properly emitted block WINS: recovery must never double-count, or one intent would move the
      // regard twice.
      [Test]
      public void GIVEN_both_a_proper_block_and_a_call_WHEN_parsing_THEN_the_action_applies_once()
      {
         ParsedResponse parsed = _parser.Parse(
            "[DIALOGUE]Well met.[/DIALOGUE]\n[ACTION]\ntype: change_relation\ndelta: 2\n[/ACTION]\n"
          + "{\"name\": \"change_relation\", \"parameters\": {\"delta\": 9}}");

         parsed.Actions.Count(a => a.Type == "change_relation").Should().Be(1);
         parsed.Actions.Single(a => a.Type == "change_relation").Parameters["delta"].Should().Be("2");
      }

      // The guard against over-eager matching: ordinary speech containing a brace is not a function call.
      // The pattern demands BOTH keys precisely so a character can still talk about a ledger or a list.
      [Test]
      public void GIVEN_prose_containing_braces_WHEN_parsing_THEN_nothing_is_recovered_and_nothing_is_stripped()
      {
         ParsedResponse parsed = _parser.Parse(
            "[DIALOGUE]The steward writes {the tally} in his book each night.[/DIALOGUE]");

         parsed.Actions.Should().BeEmpty();
         parsed.Dialogue.Should().Contain("{the tally}");
      }
   }
}

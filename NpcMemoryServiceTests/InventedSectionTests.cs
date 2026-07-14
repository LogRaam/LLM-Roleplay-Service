// Code written by Gabriel Mailhot, 13/07/2026.
// A player watched an NPC speak our plumbing aloud. The model had written a section we never taught,
// "[RELATION CHANGE] / delta = 2", so nothing parsed it: the regard never moved, no sound played, no green or
// red text appeared, and the raw block was simply read out in the speech bubble as if the NPC had said it
// (Fakade, 2026-07-13). Two failures in one, and the parser owned both.
//
// WHY IT MATTERS: the model is a stochastic text generator, so it WILL invent tags, misspell them, and use '='
// where we asked for ':'. The parser is the only thing standing between that and the player. These tests pin
// three rules it must never break again:
//   1. Whatever the model invents, the player NEVER reads our machinery. An unrecognised block is still
//      machinery, and it must be stripped from the spoken line, not shown.
//   2. An invented tag ENDS the dialogue, exactly as a known one does. Otherwise everything after it, block and
//      all, falls through into the NPC's mouth, which is precisely what happened here.
//   3. What the NPC plainly MEANT still happens. Silently dropping the regard would leave their word empty, the
//      hollow-promise failure this codebase fights everywhere else. The host's RelationGate still caps and
//      rate-limits the recovered action, so being generous here cannot become an exploit.

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
   public class InventedSectionTests
   {
      private static ParsedResponse Parse(string raw) => new SectionResponseParser().Parse(raw);

      // The reported case, exactly as the player saw it. The block must not survive into the spoken line: an NPC
      // reading "[RELATION CHANGE] delta = 2" out loud shatters the fiction the whole mod exists to build.
      [Test]
      public void GIVEN_the_reported_invented_block_WHEN_parsed_THEN_the_player_never_reads_the_machinery()
      {
         ParsedResponse r = Parse("[DIALOGUE]\nYou have earned my respect, Arwa.\n[RELATION CHANGE]\ndelta = 2\n[/DIALOGUE]");

         r.Dialogue.Should().NotContain("RELATION CHANGE");
         r.Dialogue.Should().NotContain("delta");
         r.Dialogue.Should().Contain("You have earned my respect");
      }

      // The other half of the report: the regard never moved. The NPC said it did, so it must. Recovered as the
      // change_relation the model should have emitted, and the RelationGate still rules on it downstream.
      [Test]
      public void GIVEN_the_reported_invented_block_WHEN_parsed_THEN_the_regard_the_npc_promised_actually_moves()
      {
         ParsedResponse r = Parse("[DIALOGUE]\nYou have earned my respect.\n[RELATION CHANGE]\ndelta = 2\n[/DIALOGUE]");

         GameAction? action = r.Actions.FirstOrDefault(a => a.Type == "change_relation");
         action.Should().NotBeNull();
         action!.Parameters["delta"].Should().Be("2");
      }

      // Without a [DIALOGUE] tag at all, an invented block used to fall outside every boundary the parser knew,
      // so the whole thing became the spoken line. An invented tag must end the dialogue as a known one does.
      [Test]
      public void GIVEN_an_invented_block_with_no_dialogue_tag_WHEN_parsed_THEN_it_still_ends_the_spoken_line()
      {
         ParsedResponse r = Parse("Well met, my friend.\n[RELATION CHANGE]\ndelta = 1\n");

         r.Dialogue.Should().Be("Well met, my friend.");
         r.Actions.Should().Contain(a => a.Type == "change_relation");
      }

      // A latent bug the report uncovered: the parser only ever looked for a colon, so even a PERFECTLY formed
      // [ACTION] block would have been dropped in silence had the model written 'delta = 2'.
      [Test]
      public void GIVEN_a_correct_action_block_that_uses_an_equals_sign_WHEN_parsed_THEN_it_is_no_longer_dropped_in_silence()
      {
         ParsedResponse r = Parse("[DIALOGUE]\nTake this.\n[/DIALOGUE]\n[ACTION]\ntype = give_gold\namount = 50\n[/ACTION]");

         GameAction? action = r.Actions.FirstOrDefault(a => a.Type == "give_gold");
         action.Should().NotBeNull();
         action!.Parameters["amount"].Should().Be("50");
      }

      // Never double-count: when the model DID emit a proper action, the recovery must stay out of the way, or a
      // model that hedged by writing both would move the regard twice.
      [Test]
      public void GIVEN_both_a_proper_action_and_a_stray_block_WHEN_parsed_THEN_the_regard_moves_exactly_once()
      {
         ParsedResponse r = Parse("[DIALOGUE]\nWell done.\n[/DIALOGUE]\n[ACTION]\ntype: change_relation\ndelta: 1\n[/ACTION]\n[RELATION CHANGE]\ndelta = 5\n");

         r.Actions.Count(a => a.Type == "change_relation").Should().Be(1);
         r.Actions.First(a => a.Type == "change_relation").Parameters["delta"].Should().Be("1");
      }

      // The discriminator is ALL-CAPS on its own line. Ordinary prose in brackets is the NPC's own words and must
      // survive: strip it and we would be censoring the writing we asked for.
      [Test]
      public void GIVEN_lower_case_brackets_inside_real_prose_WHEN_parsed_THEN_the_npcs_own_words_survive()
      {
         ParsedResponse r = Parse("[DIALOGUE]\nThe letter is torn, the name [unreadable] beneath the blood.\n[/DIALOGUE]");

         r.Dialogue.Should().Contain("[unreadable]");
      }

      // An invented block we cannot make sense of is stripped from view and left unrecovered, never guessed at.
      // Silence beats inventing an action the model never asked for.
      [Test]
      public void GIVEN_an_invented_block_we_cannot_interpret_WHEN_parsed_THEN_it_is_hidden_but_nothing_is_invented()
      {
         ParsedResponse r = Parse("[DIALOGUE]\nAs you wish.\n[MOOD SHIFT]\nvalue = grim\n[/DIALOGUE]");

         r.Dialogue.Should().NotContain("MOOD SHIFT");
         r.Dialogue.Should().NotContain("grim");
         r.Dialogue.Should().Contain("As you wish");
         r.Actions.Should().BeEmpty();
      }
   }
}

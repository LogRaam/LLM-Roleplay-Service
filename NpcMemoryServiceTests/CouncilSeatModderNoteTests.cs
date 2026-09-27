// Code written by Gabriel Mailhot, 27/09/2026.
// The council's door for another mod's text, one seat at a time. tashmetu (Bellum Civile bridge, Nexus, 26/09/2026)
// built bc_temper, a {{variable}} saying how a lord judges the people around him, and saw it behave in a private word;
// the council, where "a cautious vassal objects to a reckless marshal in front of the player" would matter most,
// carried no modder text at all (no behavior_guidelines, no cr_patch, no {{variables}}), so he left it alone "until
// there's a hook there". The hook is a council_seat.txt template, contributed through cr_patch like any prompt file,
// expanded once per seated member with that member's own variables, and rendered under that member's roster line.
//
// What breaks if this is wrong: a note under the wrong seat puts one lord's private judgement in another's mouth; a
// note with no name on it reads as the player's ("you" is the player at this table); and an unbounded note, multiplied
// by six seats, eats the small models' budget the Compact prompt fights for.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CouncilSeatModderNoteTests
   {
      private static Dictionary<string, string> VarsFor(string seat, string temper)
         => new() {["char"] = seat, ["user"] = "Arwa", ["bc_temper"] = temper};

      // Nobody contributed a template: the council is exactly what it was before the hook existed.
      [Test]
      public void GIVEN_no_template_WHEN_a_seat_note_is_composed_THEN_there_is_none()
      {
         CouncilSeatNote.Compose(null, VarsFor("Derthert", "x")).Should().BeNull();
         CouncilSeatNote.Compose("  \n ", VarsFor("Derthert", "x")).Should().BeNull();
      }

      // The point of the hook: each seat reads its OWN value of a variable, not the anchor's.
      [Test]
      public void GIVEN_a_template_WHEN_composed_for_two_seats_THEN_each_carries_its_own_values()
      {
         const string template = "{{char}} {{bc_temper}}";

         CouncilSeatNote.Compose(template, VarsFor("Derthert", "thinks Aldric rash.")).Should().Be("Derthert thinks Aldric rash.");
         CouncilSeatNote.Compose(template, VarsFor("Aldric", "thinks Derthert timid.")).Should().Be("Aldric thinks Derthert timid.");
      }

      // tashmetu's token is empty when a lord sees no contrast worth voicing; the seat must then carry no note at all,
      // not a heading with nothing under it.
      [Test]
      public void GIVEN_a_variable_that_resolves_empty_WHEN_composed_THEN_the_seat_has_no_note()
      {
         CouncilSeatNote.Compose("{{bc_temper}}", VarsFor("Derthert", "")).Should().BeNull();
      }

      // A cr_patch contribution arrives under an "=== FURTHER GUIDANCE ===" heading, which structures the big prompt
      // and means nothing inside one seat's line.
      [Test]
      public void GIVEN_a_contribution_under_a_section_heading_WHEN_composed_THEN_the_heading_is_dropped()
      {
         CouncilSeatNote.Compose("=== FURTHER GUIDANCE ===\n{{bc_temper}}", VarsFor("Derthert", "distrusts the king."))
                        .Should().Be("distrusts the king.");
      }

      // Six seats multiply whatever one seat carries: past the cap a note is cut at a sentence, never mid-word.
      [Test]
      public void GIVEN_a_note_longer_than_the_cap_WHEN_composed_THEN_it_is_cut_at_a_sentence_within_the_cap()
      {
         string sentence = new string('a', 90) + ". ";
         string template = string.Concat(System.Linq.Enumerable.Repeat(sentence, 20));

         string? note = CouncilSeatNote.Compose(template, VarsFor("Derthert", ""));

         note!.Length.Should().BeLessThanOrEqualTo(CouncilSeatNote.MaxChars);
         note.Should().EndWith(".");
      }

      // "You" is the player at this table ("remembers of you", "regard toward the player"): the note is announced as
      // THAT seat's own view, by name, under that seat, so it can never be read as said to or about the player.
      [Test]
      public void GIVEN_a_seat_with_a_note_WHEN_the_council_prompt_is_built_THEN_the_note_sits_under_that_seat_by_name()
      {
         string prompt = CouncilPromptBuilder.Build(new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {
               new() {Name = "Derthert", ModderNote = "He thinks Aldric rash."},
               new() {Name = "Aldric"}
            }
         });

         int derthert = prompt.IndexOf("- Derthert", System.StringComparison.Ordinal);
         int aldric = prompt.IndexOf("- Aldric", System.StringComparison.Ordinal);
         int note = prompt.IndexOf("Derthert's own view", System.StringComparison.Ordinal);

         note.Should().BeGreaterThan(derthert).And.BeLessThan(aldric);
         prompt.Should().Contain("He thinks Aldric rash.");
      }

      // A seat without a note renders exactly as before.
      [Test]
      public void GIVEN_no_seat_has_a_note_WHEN_the_council_prompt_is_built_THEN_no_view_line_appears()
      {
         string prompt = CouncilPromptBuilder.Build(new CouncilPromptInput {Roster = new List<CouncilMemberInput> {new() {Name = "Aldric"}}});

         prompt.Should().NotContain("own view");
      }
   }
}

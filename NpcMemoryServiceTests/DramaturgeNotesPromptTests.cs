// Code written by Gabriel Mailhot, 28/11/2026.
// Stage 6 of the group-scene plan, the per-turn machinery of the Dramaturge mode (Gabriel, 28/11/2026: "le mode dramaturge
// avec DRAMA+PROSE+INTERPRET+NSFW"). A cheap reader keeps the character's inner life after each line, and the next reply is
// written from where it stands now; a keeper reads each reply before it is shown and, on a lie, a repeat, a contradiction or
// an invention, has it written once more with the reason. Not A/B-validated: it lives only under the Dramaturge mode.
//
// These tests pin the two per-turn sections. If they are wrong: a turn of any other mode sees its prompt move (and the golden
// prompts with it), or the inner life lands above the cache marker and costs the cache on every turn.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class DramaturgeNotesPromptTests
   {
      private static NpcProfile Npc() => new() {Id = "lord_test", Name = "Alympia", Faction = "Empire", Clan = "Pethros"};

      private static string Prompt(EncounterContext ctx, bool enabled = true, LeanPromptLevel lean = LeanPromptLevel.Full)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature, PlayerName = "Arwa", EnableDramaturgeNotes = enabled}
           .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 50}, ctx);

      // STAKE: the default must not move. A turn with no note, a blank one, or a host that never turned the sections on
      // writes the very same prompt.
      [Test]
      public void GIVEN_no_note_a_blank_one_or_the_flag_off_WHEN_the_prompt_is_built_THEN_it_is_the_same_as_without()
      {
         string bare = Prompt(new EncounterContext());

         Prompt(new EncounterContext {InnerLifeNote = "  ", DraftRefusedNote = ""}).Should().Be(bare);
         Prompt(new EncounterContext {InnerLifeNote = "- you feel: wary", DraftRefusedNote = "a lie"}, enabled: false).Should().Be(bare);
      }

      // STAKE: the character writes from where it stands now, told never to say it outright, and the note changes every turn,
      // so it must sit below the CURRENT ENCOUNTER marker or every turn pays for the whole prefix again.
      [Test]
      public void GIVEN_an_inner_life_WHEN_the_prompt_is_built_THEN_it_reads_exactly_so_below_the_marker()
      {
         string prompt = Prompt(new EncounterContext {InnerLifeNote = "- you feel: wary\n- what you want now: to be left alone"}).Replace("\r\n", "\n");

         prompt.Should().Contain(
            "WHERE YOU STAND NOW (your inner life at this moment; yours alone, never say it outright):\n"
            + "- you feel: wary\n- what you want now: to be left alone\n"
            + "Let it shape what you say and how you say it; say less than you feel.\n");
         prompt.IndexOf("WHERE YOU STAND NOW").Should().BeGreaterThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading));
      }

      // STAKE: the rewrite knows why its first draft was refused (the NPCs-never-lie rule), and only that turn carries it.
      [Test]
      public void GIVEN_a_refused_draft_WHEN_the_rewrite_prompt_is_built_THEN_the_reason_is_written_below_the_marker()
      {
         string prompt = Prompt(new EncounterContext {DraftRefusedNote = "You said something you know is false: \"I was never in Lageta\"."}).Replace("\r\n", "\n");

         prompt.Should().Contain("YOUR FIRST DRAFT OF THIS REPLY WAS REFUSED:\nYou said something you know is false: \"I was never in Lageta\".\nWrite this reply again without that fault.\n");
         prompt.IndexOf("YOUR FIRST DRAFT").Should().BeGreaterThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading));
         prompt.Should().NotContain("WHERE YOU STAND NOW");
      }

      // STAKE (Lean budgets): the compact prompt carries the inner life too, a few lines.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_an_inner_life_is_carried_THEN_it_is_there()
      {
         string compact = Prompt(new EncounterContext {LeanLevel = LeanPromptLevel.Lean, InnerLifeNote = "- you feel: wary"}, lean: LeanPromptLevel.Lean);

         compact.Should().Contain("WHERE YOU STAND NOW");
      }
   }
}

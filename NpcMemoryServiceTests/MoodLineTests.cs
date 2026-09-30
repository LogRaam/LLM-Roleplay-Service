// Code written by Gabriel Mailhot, 30/09/2026.
// The mood of the day, increments 3 and 4 (ROADMAP, THE MOOD OF THE DAY): the line the character is given about his
// day, and how the player moves it during the conversation. Ruling 1: mood COLOURS delivery and never decides an
// outcome, so the line must say so. Ruling 4: the player moves it, one band per turn and two across a conversation,
// so a bleak man can be warmed but not transformed, and the prompt says where he started and where he is now.
// Ruling 5: wit is rare and English (understatement, at one's own expense, said straight). If these fail, a mood
// either never reaches the model, or reads as permission to refuse, or turns a grim man cheerful in one line.

#region

using FluentAssertions;
using NpcMemoryService.Core.Mood;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class MoodLineTests
   {
      // ── The ladder the player climbs ──────────────────────────────────────────────────────────────

      // Warming a grim man by the most a conversation allows brings him to guarded, never to good spirits.
      [Test]
      public void GIVEN_a_grim_man_WHEN_the_player_lifts_him_as_far_as_a_conversation_allows_THEN_he_is_warmed_not_transformed()
      {
         MoodLadder.Shift(DayMood.Grim, 5).Should().Be(DayMood.Guarded);
      }

      // One lift is one band.
      [Test]
      public void GIVEN_a_guarded_man_WHEN_lifted_once_THEN_he_is_at_an_ordinary_ease()
      {
         MoodLadder.Shift(DayMood.Guarded, 1).Should().Be(DayMood.Even);
      }

      // Restless is beside the ladder, level with an ordinary day: lifted he opens up, lowered he closes.
      [Test]
      public void GIVEN_a_restless_man_WHEN_moved_THEN_he_settles_or_sours()
      {
         MoodLadder.Shift(DayMood.Restless, 1).Should().Be(DayMood.Expansive);
         MoodLadder.Shift(DayMood.Restless, -1).Should().Be(DayMood.Guarded);
      }

      // A turn moves one band at most, whatever the model claims; the conversation two at most.
      [Test]
      public void GIVEN_a_turn_s_judgement_WHEN_added_THEN_a_turn_is_one_band_and_a_conversation_two()
      {
         MoodLadder.AddTurn(0, 3).Should().Be(1);
         MoodLadder.AddTurn(2, 1).Should().Be(2);
         MoodLadder.AddTurn(-2, -1).Should().Be(-2);
      }

      // ── The line ─────────────────────────────────────────────────────────────────────────────────

      // Ruling 1 in the prompt itself: how he feels, never what he decides.
      [Test]
      public void GIVEN_a_grim_day_WHEN_composed_THEN_the_line_names_it_and_says_it_never_decides()
      {
         string line = MoodLine.Compose(DayMood.Grim, DayMood.Grim, false);

         line.Should().Contain("grim");
         line.Should().Contain("never what you will agree to");
      }

      // An ordinary day renders no mood of its own (the affordability rule), but the player can still move it, so the
      // character is still told how to say so.
      [Test]
      public void GIVEN_an_ordinary_day_WHEN_composed_THEN_no_mood_is_described_but_the_mood_line_is_taught()
      {
         string line = MoodLine.Compose(DayMood.Even, DayMood.Even, false);

         line.Should().NotContain("grim");
         line.Should().Contain("mood: lifted");
      }

      // Ruling 4: the prompt carries both facts, where he started and where the player brought him.
      [Test]
      public void GIVEN_a_man_the_player_has_warmed_WHEN_composed_THEN_the_line_says_where_he_began_and_where_he_is()
      {
         string line = MoodLine.Compose(DayMood.Grim, DayMood.ShortTempered, false);

         line.Should().Contain("began this conversation");
         line.Should().Contain("grim");
         line.Should().Contain("patience");
      }

      // Ruling 5: only when permitted, and then as understatement, at one's own expense, said straight.
      [Test]
      public void GIVEN_wit_permitted_WHEN_composed_THEN_one_dry_remark_is_allowed_and_otherwise_never()
      {
         MoodLine.Compose(DayMood.HighSpirits, DayMood.HighSpirits, true).Should().Contain("understatement");
         MoodLine.Compose(DayMood.HighSpirits, DayMood.HighSpirits, false).Should().NotContain("understatement");
      }

      // No day drawn, nothing said.
      [Test]
      public void GIVEN_no_day_WHEN_composed_THEN_nothing_is_written()
      {
         MoodLine.Compose(null, null, false).Should().BeEmpty();
      }

      // ── The judgement comes back ─────────────────────────────────────────────────────────────────

      // The character's own verdict on the exchange rides in the [STANCE] block it already knows, and is read.
      [Test]
      public void GIVEN_a_reply_whose_stance_block_says_the_mood_lifted_WHEN_parsed_THEN_the_lift_is_read()
      {
         var parsed = new SectionResponseParser().Parse("[DIALOGUE]Well met.[/DIALOGUE]\n[STANCE]\nmood: lifted\n[/STANCE]");

         parsed.StanceShift.Should().NotBeNull();
         parsed.StanceShift!.Mood.Should().Be(1);
         parsed.Dialogue.Should().NotContain("mood");
      }

      [Test]
      public void GIVEN_a_reply_whose_mood_was_lowered_WHEN_parsed_THEN_the_fall_is_read()
      {
         new SectionResponseParser().Parse("[DIALOGUE]Leave me.[/DIALOGUE]\n[STANCE]\nmood: lowered\n[/STANCE]").StanceShift!.Mood.Should().Be(-1);
      }
   }
}

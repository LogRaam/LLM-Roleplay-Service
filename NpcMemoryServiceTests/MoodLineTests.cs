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

      // ── Self-esteem in the prompt (E3, 30/09/2026) ─────────────────────────────────────────────────

      // Lordfadooboo: "it may have affects on their expectations when speaking to you and how they respond to failure...
      // a character might become insecure with you... they might require you to tell them things they want to hear."
      [Test]
      public void GIVEN_a_shaken_man_WHEN_his_self_regard_is_composed_THEN_it_speaks_of_offence_failure_and_wanting_reassurance()
      {
         string line = SelfEsteemLine.Compose(SelfEsteemBand.Shaken);

         line.Should().Contain("shaken");
         line.Should().Contain("reassurance");
         line.Should().Contain("never what you will agree to");
      }

      // "If they have a high self esteem they might be unphased by difficulties."
      [Test]
      public void GIVEN_a_proud_man_WHEN_composed_THEN_he_is_not_easily_shaken_and_expects_deference()
      {
         string line = SelfEsteemLine.Compose(SelfEsteemBand.Proud);

         line.Should().Contain("not easily shaken");
         line.Should().Contain("deference");
      }

      // Most men are steady most days: that says nothing, and nothing known says nothing.
      [Test]
      public void GIVEN_a_steady_man_or_none_WHEN_composed_THEN_nothing_is_written()
      {
         SelfEsteemLine.Compose(SelfEsteemBand.Steady).Should().BeEmpty();
         SelfEsteemLine.Compose(null).Should().BeEmpty();
      }

      // ── Letters (increment 5, Ruling 6): a letter is written on a day too ───────────────────────

      // "A note dashed off by a man having a bleak one should not read like the same man at a feast."
      [Test]
      public void GIVEN_a_grim_day_WHEN_he_writes_THEN_the_letter_is_told_the_day_and_that_it_decides_nothing()
      {
         string line = MoodLine.ForLetter(DayMood.Grim)!;

         line.Should().Contain("grim");
         line.Should().Contain("never in what you decide");
      }

      // An ordinary day, or none drawn, adds nothing to a letter.
      [Test]
      public void GIVEN_an_ordinary_day_or_none_WHEN_he_writes_THEN_nothing_is_added()
      {
         MoodLine.ForLetter(DayMood.Even).Should().BeNull();
         MoodLine.ForLetter(null).Should().BeNull();
      }

      // The letter builders carry it: an initial letter written on a grim day says so.
      [Test]
      public void GIVEN_an_initial_letter_on_a_grim_day_WHEN_built_THEN_the_day_is_in_the_instruction()
      {
         var npc = new NpcMemoryService.Core.Models.NpcProfile {Id = "lord_1", Name = "Derthert", Clan = "Dey Meroc", Faction = "Vlandia"};

         NpcMemoryService.Core.Prompts.LetterPromptBuilder
            .BuildInitialLetterMessage(npc, NpcMemoryService.Core.Models.LetterReason.RomanticCorrespondence, "", "Aldric", mood: DayMood.Grim)
            .Should().Contain("grim");
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

      // fkasad (Nexus, 05/10/2026): Dorin sought Bardin out to thank him, and his short-tempered day made him curt
      // instead. The day decides the manner, never why the conversation is happening: a man come to thank still thanks.
      [Test]
      public void GIVEN_a_bad_day_WHEN_the_mood_is_written_THEN_it_never_overrides_why_the_character_is_here()
      {
         MoodLine.Compose(DayMood.ShortTempered, DayMood.ShortTempered, false)
                 .Should().Contain("never why this conversation is happening");
      }
   }
}

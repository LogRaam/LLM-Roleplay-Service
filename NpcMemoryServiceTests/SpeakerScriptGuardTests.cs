// Code written by Gabriel Mailhot, 13/09/2026.
// Written from Gabriel's log at 13:31:32, where Nabb's reply came back as a script with the PLAYER's lines
// in it, and those lines said the briefing plan out loud to the person it was meant to stay hidden from.
// The reply below is that reply, shortened but not reworded.

#region

using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class SpeakerScriptGuardTests
   {
      private const string Nabb = "Nabb the Bloody Handed";
      private const string Arwa = "Arwa";

      // Gabriel's actual reply, trimmed in the middle but otherwise as the model wrote it.
      private const string TheScript =
         "*Ira's fingers linger on Nabb's sleeve for a heartbeat longer than necessary.*\n"
       + "\n"
       + "[Nabb]: *Nabb holds his ground as Ira steps back. He looks at her, and something like admiration "
       + "flickers across his face.*\n"
       + "\n"
       + "[Arwa]: *Arwa steps forward, reclaiming the attention of the room.* \"A proposal, Lady Ira. I brought "
       + "Nabb because he is the finest blade I have, and because he has a gift for making himself "
       + "indispensable. I offer you his service. His counsel. His blade. And whatever else may grow from "
       + "that.\"\n"
       + "\n"
       + "[Nabb]: *Nabb's lips twitch. His place, for now, is to be the offer on the table.*";

      // ── the reported case ──

      [Test]
      public void GIVEN_the_reply_Gabriel_got_WHEN_guarded_THEN_the_players_speech_is_gone()
      {
         SpeakerScriptVerdict verdict = SpeakerScriptGuard.Cut(TheScript, Nabb, Arwa);

         verdict.Text.Should().NotContain("A proposal, Lady Ira");
         verdict.Text.Should().NotContain("whatever else may grow from that");
         verdict.Text.Should().NotContain("the finest blade I have");
      }

      // The point of the whole thing: what the player's fabricated speech GAVE AWAY was the briefing.
      [Test]
      public void GIVEN_the_reply_Gabriel_got_WHEN_guarded_THEN_the_briefing_plan_is_not_said_aloud()
      {
         SpeakerScriptGuard.Cut(TheScript, Nabb, Arwa)
                           .Text
                           .Should()
                           .NotContain("gift for making himself indispensable");
      }

      [Test]
      public void GIVEN_the_reply_Gabriel_got_WHEN_guarded_THEN_it_is_reported_as_having_spoken_for_the_player()
      {
         SpeakerScriptVerdict verdict = SpeakerScriptGuard.Cut(TheScript, Nabb, Arwa);

         verdict.CutAnything.Should().BeTrue();
         verdict.SpokeForThePlayer.Should().BeTrue();
         verdict.CutSpeakers.Should().Contain("Arwa");
      }

      // The speaker keeps everything that was actually his, label or no label: this must SILENCE nobody but
      // the people who were never speaking.
      [Test]
      public void GIVEN_the_reply_Gabriel_got_WHEN_guarded_THEN_Nabb_keeps_every_word_of_his_own()
      {
         string kept = SpeakerScriptGuard.Cut(TheScript, Nabb, Arwa).Text;

         kept.Should().Contain("Nabb holds his ground");        // his first labelled turn
         kept.Should().Contain("Nabb's lips twitch");           // his second, after the player's was cut
         kept.Should().Contain("Ira's fingers linger");         // the unlabelled opening is his prose
         kept.Should().NotContain("[Nabb]");                    // his own label is redundant, so it goes
      }

      // ── what must survive untouched ──

      // The ordinary reply, which is almost every reply. Nothing here looks like a label and nothing may move.
      [Test]
      public void GIVEN_an_ordinary_reply_with_no_labels_WHEN_guarded_THEN_it_is_returned_exactly_as_written()
      {
         const string plain = "*He inclines his head.* \"As you say, my lady. I will see it done by morning.\"";

         SpeakerScriptVerdict verdict = SpeakerScriptGuard.Cut(plain, Nabb, Arwa);

         verdict.Text.Should().Be(plain);
         verdict.CutAnything.Should().BeFalse();
         verdict.SpokeForThePlayer.Should().BeFalse();
      }

      // THE FALSE-POSITIVE THAT WOULD MATTER MOST. Prose opens a title-cased word before a colon all the time,
      // and cutting a real line is a far worse failure than missing a stray label. A bare word is a speaker
      // only when it names somebody actually in the scene.
      [Test]
      public void GIVEN_prose_that_opens_a_line_with_a_word_and_a_colon_WHEN_guarded_THEN_nothing_is_cut()
      {
         const string prose = "\"I will agree, on terms.\"\nOne condition: the men stay under my command.\n"
                            + "A warning: cross me and there will be no second conversation.";

         SpeakerScriptVerdict verdict = SpeakerScriptGuard.Cut(prose, Nabb, Arwa);

         verdict.CutAnything.Should().BeFalse();
         verdict.Text.Should().Be(prose);
      }

      // An NPC narrating what the player does, unlabelled, is this mod's ordinary third-person style and has
      // never been the complaint. Only a LABELLED turn is certain enough to cut.
      [Test]
      public void GIVEN_the_speaker_narrating_the_player_without_a_label_WHEN_guarded_THEN_it_stands()
      {
         const string narrated = "*He watches Arwa go still, reading the silence for what it is.* \"She says "
                               + "nothing. That is answer enough.\"";

         SpeakerScriptGuard.Cut(narrated, Nabb, Arwa).CutAnything.Should().BeFalse();
      }

      // ── the rest of the shape ──

      // A bare "Arwa:" is the same offence as "[Arwa]:" and is caught, because the name is one in the scene.
      [Test]
      public void GIVEN_the_player_scripted_without_brackets_WHEN_guarded_THEN_it_is_still_cut()
      {
         const string bare = "*He steps back.*\nArwa: \"Then we have an accord.\"";

         SpeakerScriptVerdict verdict = SpeakerScriptGuard.Cut(bare, Nabb, Arwa);

         verdict.SpokeForThePlayer.Should().BeTrue();
         verdict.Text.Should().NotContain("Then we have an accord");
      }

      // Another NPC scripted is cut too, but it is NOT the player case: the caller keeps this turn's [EVENT]
      // for it, and that difference has to be readable from the verdict alone.
      [Test]
      public void GIVEN_another_npc_scripted_WHEN_guarded_THEN_it_is_cut_but_not_flagged_as_the_player()
      {
         const string script = "*He nods.*\n[Ira]: \"And what would you have me say to that?\"";

         SpeakerScriptVerdict verdict = SpeakerScriptGuard.Cut(script, Nabb, Arwa);

         verdict.CutAnything.Should().BeTrue();
         verdict.SpokeForThePlayer.Should().BeFalse();
         verdict.Text.Should().NotContain("what would you have me say");
      }

      // A model labels a turn by the short name; the speaker is known by the long one. Over-matching the
      // speaker's OWN label is the safe direction to be wrong in: it keeps their words.
      [Test]
      public void GIVEN_a_short_label_for_a_long_name_WHEN_matched_THEN_it_is_the_same_person()
      {
         SpeakerScriptGuard.IsSamePerson("Nabb", Nabb).Should().BeTrue();
         SpeakerScriptGuard.IsSamePerson("Ira", "Ira of Pethros").Should().BeTrue();
         SpeakerScriptGuard.IsSamePerson("Coll", Nabb).Should().BeFalse();
         SpeakerScriptGuard.IsSamePerson(null, Nabb).Should().BeFalse();
         SpeakerScriptGuard.IsSamePerson("Nabb", null).Should().BeFalse();
      }

      [Test]
      public void GIVEN_nothing_at_all_WHEN_guarded_THEN_it_declines_quietly_rather_than_throwing()
      {
         SpeakerScriptGuard.Cut(null, Nabb, Arwa).CutAnything.Should().BeFalse();
         SpeakerScriptGuard.Cut("", Nabb, Arwa).CutAnything.Should().BeFalse();
         SpeakerScriptGuard.Cut("   ", Nabb, Arwa).Text.Should().Be("   ");
         SpeakerScriptGuard.Cut(TheScript, null, null).CutAnything.Should().BeTrue(); // unknown speaker: keep nobody's parts
      }
   }
}

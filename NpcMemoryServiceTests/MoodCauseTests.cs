// Code written by Gabriel Mailhot, 18/10/2026.
// Why the day is what it is (Lordfadooboo, 18/10/2026: "the ai struggles to invent a reason for their mood"). Gabriel:
// the list lives in the code, and the real cause is always named when there is one. If these fail, a character asked
// what troubles him either has nothing to say, blames a fact that did not cause his mood (grim over a victory), names a
// trifle when his realm is losing, or tells a different small story each time he is asked on the same day.

#region

using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Mood;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class MoodCauseTests
   {
      // STAKE: an ordinary day renders nothing, so it needs no cause either.
      [Test]
      public void GIVEN_an_ordinary_day_WHEN_its_cause_is_asked_THEN_there_is_none()
      {
         MoodCause.Of(DayMood.Even, new MoodFacts {PersonalBlow = true}, 7).Should().BeNull();
      }

      // STAKE: Gabriel's ruling: the real cause is always named. A grim man whose kin died is grim over that, in the
      // host's own words, never over a stubbed toe.
      [Test]
      public void GIVEN_a_blow_of_his_own_WHEN_he_is_grim_THEN_the_blow_is_the_cause_in_the_host_s_words()
      {
         DayCause cause = MoodCause.Of(DayMood.Grim, new MoodFacts {PersonalBlow = true, PersonalBlowWhat = "the death of your brother Olek"}, 7)!;

         cause.IsReal.Should().BeTrue();
         cause.Text.Should().Be("the death of your brother Olek");
      }

      // STAKE: a save from before the blow's kind was kept still has a blow on record: it is named vaguely but truly,
      // never invented.
      [Test]
      public void GIVEN_a_blow_whose_kind_was_never_kept_WHEN_he_is_grim_THEN_it_is_named_vaguely_and_truly()
      {
         DayCause cause = MoodCause.Of(DayMood.Grim, new MoodFacts {PersonalBlow = true}, 7)!;

         cause.IsReal.Should().BeTrue();
         cause.Text.Should().Be("a blow of your own these last days");
      }

      // STAKE: of several facts, the one that leaned this mood most: a grim man who lost kin and whose realm is losing
      // is grim over the death first (6 against 4).
      [Test]
      public void GIVEN_two_facts_that_lean_grim_WHEN_he_is_grim_THEN_the_stronger_is_named()
      {
         var facts = new MoodFacts {RealmHasLatelyLost = true, IsHurt = true, PersonalBlow = true, PersonalBlowWhat = "the loss of Pravend"};

         MoodCause.Of(DayMood.Grim, facts, 7)!.Text.Should().Be("the loss of Pravend");
         MoodCause.Of(DayMood.ShortTempered, facts, 7)!.Text.Should().Be("your wounds");
      }

      // STAKE: a fact that did not lean the mood did not cause it: a grim man with a fresh gladness is not grim over
      // the gladness, and a restless one is not restless over a wound (wounds do not lean restless).
      [Test]
      public void GIVEN_a_fact_that_did_not_lean_the_mood_WHEN_its_cause_is_asked_THEN_a_trifle_is_given_not_the_fact()
      {
         DayCause grim = MoodCause.Of(DayMood.Grim, new MoodFacts {PersonalGladness = true, PersonalGladnessWhat = "a fief newly yours"}, 7)!;
         DayCause restless = MoodCause.Of(DayMood.Restless, new MoodFacts {IsHurt = true}, 7)!;

         grim.IsReal.Should().BeFalse();
         MoodCause.Trifles[DayMood.Grim].Should().Contain(grim.Text);
         restless.IsReal.Should().BeFalse();
         MoodCause.Trifles[DayMood.Restless].Should().Contain(restless.Text);
      }

      // STAKE: a captive's day is his captivity, whatever mood it took.
      [Test]
      public void GIVEN_a_captive_WHEN_his_cause_is_asked_THEN_it_is_the_captivity()
      {
         foreach (DayMood m in new[] {DayMood.Grim, DayMood.ShortTempered, DayMood.Guarded})
            MoodCause.Of(m, new MoodFacts {IsCaptive = true, PersonalBlow = true}, 7)!.Text.Should().Be("being held prisoner");
      }

      // STAKE: good news causes good days: a realm winning lifts an expansive lord, his own gladness a high-spirited one.
      [Test]
      public void GIVEN_good_news_WHEN_he_is_in_good_humour_THEN_the_good_news_is_the_cause()
      {
         var facts = new MoodFacts {RealmHasLatelyWon = true, PersonalGladness = true, PersonalGladnessWhat = "the tournament you won at Lageta"};

         MoodCause.Of(DayMood.Expansive, facts, 7)!.Text.Should().Be("your realm's war going well");
         MoodCause.Of(DayMood.HighSpirits, facts, 7)!.Text.Should().Be("the tournament you won at Lageta");
      }

      // STAKE: asked twice the same day he tells the same small story; another day may bring another.
      [Test]
      public void GIVEN_a_day_no_fact_explains_WHEN_asked_twice_THEN_the_same_trifle_and_other_days_vary()
      {
         int seed = MoodOfTheDay.SeedFor("lord_1_1", 120);

         MoodCause.Of(DayMood.Guarded, new MoodFacts(), seed).Should().Be(MoodCause.Of(DayMood.Guarded, new MoodFacts(), seed));
         Enumerable.Range(100, 30).Select(d => MoodCause.Of(DayMood.Guarded, new MoodFacts(), MoodOfTheDay.SeedFor("lord_1_1", d))!.Text)
                   .Distinct().Count().Should().BeGreaterThan(1);
      }

      // STAKE: every mood but the ordinary one has trifles, and a good day's trifles never explain a bad day.
      [Test]
      public void GIVEN_the_table_WHEN_read_THEN_every_coloured_mood_has_trifles_of_its_own_colour()
      {
         foreach (DayMood m in new[] {DayMood.Grim, DayMood.ShortTempered, DayMood.Guarded, DayMood.Restless, DayMood.Expansive, DayMood.HighSpirits})
            MoodCause.Trifles[m].Should().NotBeEmpty();

         MoodCause.Trifles[DayMood.Grim].Should().NotIntersectWith(MoodCause.Trifles[DayMood.HighSpirits]);
      }
   }
}

// Code written by Gabriel Mailhot, 14/09/2026.
// The mood of the day, increment 1. What these pin is the six rulings in ROADMAP.md, in the order they
// matter: that a day's facts FORBID moods rather than select them, that the draw is stable for a day and
// different across days, and that wit is rare on top of a mood that is already uncommon.

#region

using FluentAssertions;
using NpcMemoryService.Core.Mood;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class MoodOfTheDayTests
   {
      // An ordinary man having an ordinary day, somewhere he can be at ease. Everything is open to him, so a
      // test below that finds a mood missing knows the FACT removed it rather than the default.
      private static MoodFacts Ordinary() => new() {AtEase = true};

      [Test]
      public void GIVEN_a_day_with_nothing_pressing_WHEN_asked_THEN_every_mood_is_open_to_him()
      {
         MoodOfTheDay.Permitted(Ordinary()).Should().HaveCount(7);
      }

      // THE HALF THE FIRST DRAFT WAS MISSING (Gabriel, 14/09/2026). Forbidding alone made every influence
      // negative: a lord whose realm had just won was merely ALLOWED to be expansive, at the same odds as any
      // other day, and no player would ever feel it. Good news must LEAN, not merely permit.
      [Test]
      public void GIVEN_his_realm_has_lately_won_WHEN_weighed_THEN_good_humour_is_likelier_and_bleakness_is_not()
      {
         var plain = MoodOfTheDay.Weights(Ordinary());
         var won = MoodOfTheDay.Weights(Ordinary() with {RealmHasLatelyWon = true});

         won[DayMood.Expansive].Should().BeGreaterThan(plain[DayMood.Expansive]);
         won[DayMood.HighSpirits].Should().BeGreaterThan(plain[DayMood.HighSpirits]);
         won[DayMood.Grim].Should().BeLessThan(plain[DayMood.Grim], "gladness dampens bleakness");
         won[DayMood.Grim].Should().BeGreaterThan(0, "but a man may be glad and still grim; it is not forbidden");
      }

      // And the mirror, so the lean is not a one-way tweak that only flatters good news.
      [Test]
      public void GIVEN_a_blow_of_his_own_WHEN_weighed_THEN_bleakness_is_likelier()
      {
         var plain = MoodOfTheDay.Weights(Ordinary());
         var struck = MoodOfTheDay.Weights(Ordinary() with {PersonalBlow = true});

         struck[DayMood.Grim].Should().BeGreaterThan(plain[DayMood.Grim]);
         struck[DayMood.Guarded].Should().BeGreaterThan(plain[DayMood.Guarded]);
      }

      // A lean must never revive a mood the day forbade outright, or the two halves of the table fight.
      [Test]
      public void GIVEN_a_mood_the_day_forbids_WHEN_something_else_leans_toward_it_THEN_it_stays_forbidden()
      {
         // Hurt forbids HighSpirits; gladness and ease both lean toward it.
         var f = Ordinary() with {IsHurt = true, PersonalGladness = true};

         MoodOfTheDay.Weights(f)[DayMood.HighSpirits].Should().Be(0);
         MoodOfTheDay.Permitted(f).Should().NotContain(DayMood.HighSpirits);
      }

      // Every mood in the enum must appear in the table, or one exists that can never be drawn and nobody
      // would notice - the same orphan-step rule the chat-close plan is pinned by.
      [Test]
      public void GIVEN_the_table_WHEN_read_THEN_every_mood_that_exists_is_in_it()
      {
         var table = MoodOfTheDay.Weights(Ordinary());

         foreach (DayMood m in System.Enum.GetValues(typeof(DayMood)).Cast<DayMood>())
            table.Should().ContainKey(m, $"{m} exists, so the table must say what makes it likely");
      }

      // THE MECHANISM, and the reason it forbids rather than selects: a host that can read nothing must get
      // the full range, not an empty one. A selecting rule has to be complete or it returns nothing.
      [Test]
      public void GIVEN_no_facts_at_all_WHEN_asked_THEN_it_constrains_nothing_rather_than_returning_nothing()
      {
         MoodOfTheDay.Permitted(null).Should().HaveCount(7);
      }

      // ── what a hard day forbids ──

      // The example Gabriel's ruling is written around: a lord who has just lost a fief must not wake cheerful.
      [Test]
      public void GIVEN_his_realm_has_lately_lost_ground_WHEN_asked_THEN_he_cannot_be_in_good_spirits()
      {
         IReadOnlyList<DayMood> p = MoodOfTheDay.Permitted(Ordinary() with {RealmHasLatelyLost = true});

         p.Should().NotContain(DayMood.HighSpirits);
         p.Should().NotContain(DayMood.Expansive);
         p.Should().Contain(DayMood.Grim);
      }

      // A man with a spear wound does not have an unremarkable afternoon either - Even goes with the rest.
      [Test]
      public void GIVEN_he_is_really_hurt_WHEN_asked_THEN_not_even_an_ordinary_day_is_open_to_him()
      {
         IReadOnlyList<DayMood> p = MoodOfTheDay.Permitted(Ordinary() with {IsHurt = true});

         p.Should().NotContain(DayMood.HighSpirits);
         p.Should().NotContain(DayMood.Expansive);
         p.Should().NotContain(DayMood.Even);
      }

      // THE DISTINCTION WORTH HAVING. A maiming is CARRIED, not suffered afresh, so it forbids lightness
      // without forbidding composure - which is exactly the man whose dry remark about his own ruined hand
      // shows that it has not beaten him. If this ever collapses into the wound rule, that character is lost.
      [Test]
      public void GIVEN_he_is_maimed_rather_than_freshly_wounded_WHEN_asked_THEN_composure_is_still_open_to_him()
      {
         IReadOnlyList<DayMood> p = MoodOfTheDay.Permitted(Ordinary() with {IsMaimed = true});

         p.Should().NotContain(DayMood.HighSpirits);
         p.Should().Contain(DayMood.Even);
         p.Should().Contain(DayMood.Guarded);
      }

      [Test]
      public void GIVEN_empty_coffers_WHEN_asked_THEN_he_cannot_be_open_handed()
      {
         MoodOfTheDay.Permitted(Ordinary() with {CoffersAreEmpty = true})
                     .Should()
                     .NotContain(DayMood.Expansive);
      }

      [Test]
      public void GIVEN_the_small_hours_WHEN_asked_THEN_nobody_is_expansive_at_three_in_the_morning()
      {
         IReadOnlyList<DayMood> p = MoodOfTheDay.Permitted(Ordinary() with {IsDeepNight = true});

         p.Should().NotContain(DayMood.Expansive);
         p.Should().NotContain(DayMood.HighSpirits);
      }

      // Good spirits need somewhere to happen; a siege camp is not it.
      [Test]
      public void GIVEN_he_is_not_anywhere_he_can_be_at_ease_WHEN_asked_THEN_good_spirits_are_closed()
      {
         MoodOfTheDay.Permitted(new MoodFacts()).Should().NotContain(DayMood.HighSpirits);
      }

      [Test]
      public void GIVEN_a_captive_WHEN_asked_THEN_he_is_not_having_a_mood_but_a_captivity()
      {
         IReadOnlyList<DayMood> p = MoodOfTheDay.Permitted(new MoodFacts {IsCaptive = true, AtEase = true});

         p.Should().NotContain(DayMood.HighSpirits);
         p.Should().NotContain(DayMood.Expansive);
         p.Should().NotContain(DayMood.Restless);
         p.Should().NotContain(DayMood.Even);
      }

      // Gladness does not FORCE good spirits - a man can be glad and still short with you - and nor does it
      // FORBID bleakness. Moving from forbidding to weights made this truer: a man can have had good news and
      // still be grim about something else, so gladness makes that unlikely rather than impossible. The first
      // draft asserted it was closed outright, and the weighted table is the better answer.
      [Test]
      public void GIVEN_a_gladness_of_his_own_WHEN_asked_THEN_bleakness_is_unlikely_rather_than_impossible()
      {
         MoodFacts glad = Ordinary() with {PersonalGladness = true};

         MoodOfTheDay.Weights(glad)[DayMood.Grim]
                     .Should()
                     .BeLessThan(MoodOfTheDay.Weights(Ordinary())[DayMood.Grim]);

         IReadOnlyList<DayMood> p = MoodOfTheDay.Permitted(glad);
         p.Should().Contain(DayMood.Grim);
         p.Should().Contain(DayMood.ShortTempered);
      }

      // THE FLOOR. Pile on every misery at once and there must still be a mood to draw, or the draw throws on
      // somebody's save instead of failing here where it can be seen.
      [Test]
      public void GIVEN_every_misery_at_once_WHEN_asked_THEN_there_is_still_a_mood_to_draw()
      {
         var wretched = new MoodFacts {
            IsHurt = true, IsMaimed = true, CoffersAreEmpty = true, RealmHasLatelyLost = true,
            PersonalBlow = true, IsDeepNight = true, IsCaptive = true, AtEase = false
         };

         MoodOfTheDay.Permitted(wretched).Should().NotBeEmpty();

         // Grim, short-tempered or closed - any of the three is a truthful reading of a wretched day, and
         // pinning one exactly would be pinning the hash rather than the design.
         MoodOfTheDay.Draw(wretched, 12345)
                     .Should()
                     .BeOneOf(DayMood.Grim, DayMood.ShortTempered, DayMood.Guarded);
      }

      // ── the draw ──

      // Stable within a day: talking to the same man twice in one afternoon finds the same man. A person's
      // day does not reset because you walked out and back in.
      [Test]
      public void GIVEN_the_same_seed_WHEN_drawn_again_THEN_it_is_the_same_man()
      {
         MoodFacts f = Ordinary();

         MoodOfTheDay.Draw(f, 4242).Should().Be(MoodOfTheDay.Draw(f, 4242));
      }

      // And different across days, or this is just another fixed trait wearing a new name. Not every seed
      // need differ - that would be a broken hash - but a run of them must not be one value.
      [Test]
      public void GIVEN_a_run_of_days_WHEN_drawn_THEN_the_same_man_is_not_the_same_every_day()
      {
         MoodFacts f = Ordinary();

         Enumerable.Range(0, 60)
                   .Select(d => MoodOfTheDay.Draw(f, 7700 + d))
                   .Distinct()
                   .Should()
                   .HaveCountGreaterThan(3, "a mood that never changes is a trait, not a mood");
      }

      // The draw may only ever return something the day permits - the join between the two halves, and the
      // one place a bug would put a cheerful man in a burning castle.
      [Test]
      public void GIVEN_any_day_and_any_seed_WHEN_drawn_THEN_it_is_always_one_the_day_permits()
      {
         foreach (MoodFacts f in new[] {
                     Ordinary(),
                     Ordinary() with {IsHurt = true},
                     Ordinary() with {RealmHasLatelyLost = true},
                     new MoodFacts {IsCaptive = true},
                     new MoodFacts()
                  })
         {
            IReadOnlyList<DayMood> permitted = MoodOfTheDay.Permitted(f);
            for (var seed = 0; seed < 200; seed++)
               permitted.Should().Contain(MoodOfTheDay.Draw(f, seed));
         }
      }

      // ── wit ──

      // Ruling 5: wit is a mark of quality and must be RARE. It sits on top of HighSpirits, which is itself
      // uncommon, so this asserts the second gate exists at all rather than always passing.
      [Test]
      public void GIVEN_good_spirits_across_many_days_WHEN_asked_THEN_wit_surfaces_only_sometimes()
      {
         MoodFacts f = Ordinary();
         int witty = Enumerable.Range(0, 400).Count(s => MoodOfTheDay.WitIsPermitted(DayMood.HighSpirits, f, s));

         witty.Should().BeGreaterThan(0, "a wit that never surfaces is a feature nobody ever sees");
         witty.Should().BeLessThan(200, "once in a while is the mechanism, not a limitation");
      }

      // No mood but good spirits may carry it, whatever the seed. A grim man does not quip.
      [Test]
      public void GIVEN_any_mood_but_good_spirits_WHEN_asked_THEN_wit_never_surfaces()
      {
         MoodFacts f = Ordinary();

         foreach (DayMood m in new[] {
                     DayMood.Even, DayMood.Grim, DayMood.ShortTempered,
                     DayMood.Guarded, DayMood.Restless, DayMood.Expansive
                  })
            for (var seed = 0; seed < 100; seed++)
               MoodOfTheDay.WitIsPermitted(m, f, seed).Should().BeFalse($"{m} is not a mood that jokes");
      }

      // THE ONE THAT KEEPS "RARE" HONEST. If wit reused the mood's own roll, every high-spirited day would be
      // a witty one and "rare" would quietly mean "whenever he is cheerful". The two draws must disagree.
      [Test]
      public void GIVEN_the_same_seed_WHEN_drawing_mood_and_wit_THEN_wit_is_not_merely_the_mood_roll_again()
      {
         MoodFacts f = Ordinary();

         var goodDays = Enumerable.Range(0, 600)
                                  .Where(s => MoodOfTheDay.Draw(f, s) == DayMood.HighSpirits)
                                  .ToList();

         goodDays.Should().NotBeEmpty("the fixture needs some good days to reason about");
         goodDays.Any(s => !MoodOfTheDay.WitIsPermitted(DayMood.HighSpirits, f, s))
                 .Should()
                 .BeTrue("some high-spirited days must pass without a remark");
      }

      [Test]
      public void GIVEN_a_captive_in_however_good_a_humour_WHEN_asked_THEN_he_does_not_quip()
      {
         var held = new MoodFacts {IsCaptive = true, AtEase = true};

         for (var seed = 0; seed < 100; seed++)
            MoodOfTheDay.WitIsPermitted(DayMood.HighSpirits, held, seed).Should().BeFalse();
      }
   }
}

// Code written by Gabriel Mailhot, 21/09/2026.
// What these pin: that the SDK still keeps Calradia's calendar by default, and that a host with another world can
// change it without the SDK arguing.
//
// This exists because of a layering mistake I made and Gabriel caught within the hour: the calendar first shipped as
// CalradianCalendar, two consts, in the engine-AGNOSTIC assembly. "CR peut injecter le calendrier dans le SDK au
// runtime en creant une instance." 21 and 84 are Bannerlord facts, and this half of the codebase is the half that is
// supposed not to know what game it is serving.
//
// The precedent was one folder away: PromptLore defaults to Calradia and is overwritten at launch by the mod from
// ModuleData/setting.json, precisely so a total conversion can reskin the mod. Such a conversion could already rename
// the world, and its year was still 84 days long, so every "a season ago" the mod wrote was wrong for it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Calendar;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class WorldCalendarTests
   {
      // These are mutable statics, so a test that changed the world and did not change it back would poison every
      // test that ran after it. That is the price of the pattern, paid here.
      [SetUp]
      public void SetUp() => WorldCalendar.Reset();

      [TearDown]
      public void TearDown() => WorldCalendar.Reset();

      // A STOCK INSTALL IS UNCHANGED, which is the promise that lets this ship at all.
      [Test]
      public void GIVEN_a_host_that_sets_nothing_WHEN_the_calendar_is_read_THEN_it_is_still_Calradia()
      {
         WorldCalendar.DaysPerSeason.Should().Be(21);
         WorldCalendar.SeasonsPerYear.Should().Be(4);
         WorldCalendar.DaysPerYear.Should().Be(84);
      }

      // THE WHOLE POINT: another world, another year, and every window in the mod follows it.
      [Test]
      public void GIVEN_a_total_conversion_with_its_own_calendar_WHEN_it_sets_one_THEN_the_year_follows()
      {
         WorldCalendar.DaysPerSeason = 30;
         WorldCalendar.SeasonsPerYear = 12;

         WorldCalendar.DaysPerYear.Should().Be(360);
         WorldCalendar.Seasons(3).Should().Be(90);
         WorldCalendar.Years(2).Should().Be(720);
      }

      // THE YEAR IS NEVER STORED, so the three numbers cannot drift into disagreeing with each other, which is the
      // failure a third settable field would have invited.
      [Test]
      public void GIVEN_only_the_season_changes_WHEN_the_year_is_read_THEN_it_is_recomputed_rather_than_remembered()
      {
         WorldCalendar.DaysPerSeason = 25;

         WorldCalendar.DaysPerYear.Should().Be(100, "four seasons of 25, not the 84 it was a moment ago");
      }

      // A TYPO IN SOMEBODY'S setting.json MUST NOT STOP TIME. Every recency window in the mod divides by these, and a
      // year of zero days would take the whole prompt with it.
      [Test]
      public void GIVEN_a_nonsense_value_WHEN_it_is_set_THEN_it_is_refused_and_the_world_keeps_its_calendar()
      {
         WorldCalendar.DaysPerSeason = 0;
         WorldCalendar.SeasonsPerYear = -3;

         WorldCalendar.DaysPerSeason.Should().Be(21);
         WorldCalendar.SeasonsPerYear.Should().Be(4);
      }

      // Reset is what the tests above lean on, and what a host reloading its setting file leans on too.
      [Test]
      public void GIVEN_a_changed_calendar_WHEN_it_is_reset_THEN_Calradia_comes_back()
      {
         WorldCalendar.DaysPerSeason = 7;
         WorldCalendar.SeasonsPerYear = 2;

         WorldCalendar.Reset();

         WorldCalendar.DaysPerYear.Should().Be(84);
      }
   }
}

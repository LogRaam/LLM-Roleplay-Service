// Code written by Gabriel Mailhot, 21/09/2026.
// How long a season and a year are in this world, in ONE place.
//
// Found by reading docs/maps/constants.md (21/09/2026). The calendar existed already, as
// CalradiaRemembers.Logic.ChronicleCalendar, and four files used it. Everywhere else the same two numbers were
// written out again:
//
//   - as named copies (MeetingGapDescriber.SeasonDays / YearDays);
//   - as bare literals (the bridge's `% 84`, and the prompt builder's "the year is 84 days, 4 seasons of 21" plus
//     its own `days / 21` and `days / 84` for the recency suffix);
//   - and hidden inside tuning values that MEAN a season or a year: 168 for two years, 252 for three seasons, 42
//     and 63 for two and three seasons.
//
// The home has to be HERE rather than in Logic, and that is the whole reason this file exists: the SDK cannot
// reference Logic, but Logic references the SDK, so this is the only shelf both can reach. The prompt builder was
// restating the calendar precisely because it could not see it.
//
// Nothing is tuned here. A day is a day; this only says how many of them make a season and a year, which is a fact
// about the world and not a dial.

namespace NpcMemoryService.Core.Calendar
{
   /// <summary>The length of a season and a year in this world, shared by everything that counts days.</summary>
   public static class CalradianCalendar
   {
      /// <summary>In-game days in one Calradian season. Four of them make a year.</summary>
      public const int DaysPerSeason = 21;

      /// <summary>In-game days in one Calradian year.</summary>
      public const int DaysPerYear = DaysPerSeason * SeasonsPerYear;

      /// <summary>Spring, summer, autumn, winter.</summary>
      public const int SeasonsPerYear = 4;

      /// <summary>
      ///   That many seasons, in days. For a tuning value that MEANS "a season" or "three seasons", written this way
      ///   so the intent survives: `Seasons(3)` says what 63 does not.
      /// </summary>
      public static int Seasons(int count) => count * DaysPerSeason;

      /// <summary>That many years, in days. See <see cref="Seasons" />: `Years(2)` says what 168 does not.</summary>
      public static int Years(int count) => count * DaysPerYear;
   }
}

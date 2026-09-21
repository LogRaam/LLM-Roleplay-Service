// Code written by Gabriel Mailhot, 21/09/2026.
// How long a season and a year are IN THE HOST'S WORLD, in one place, and settable by the host.
//
// This file was born wrong, hours before this version of it. It shipped as CalradianCalendar, with 21 and 84 as
// consts, and Gabriel caught the layering error at once: "CR peut injecter le calendrier dans le SDK au runtime en
// creant une instance." He is right. 21 and 84 are BANNERLORD facts. This assembly is the engine-agnostic half, whose
// whole purpose is to know nothing about the game it serves, and it had just been handed a Calradian constant.
//
// The pattern to follow already existed one folder away: PromptLore holds the world's name and adjective, defaults to
// Calradia so the SDK stands alone, and is overwritten at launch by the mod from an editable ModuleData/setting.json,
// deliberately so a total conversion can reskin the mod. A conversion could already rename the world; its year was
// still forced to be 84 days long. Now it is not.
//
// Mutable statics are a real cost, taken with open eyes and for the same reason PromptLore takes it: dozens of static
// policies read these, and threading a calendar instance through every one of them would be a far larger change than
// the problem deserves. The guard below is what keeps that honest: a nonsense value is refused rather than stored, so
// no part of the system can be handed a year of zero days by a typo in somebody's setting.json.

namespace NpcMemoryService.Core.Calendar
{
   /// <summary>
   ///   The length of a season and a year in the host's world, read by everything that counts days. Defaults to
   ///   Bannerlord's Calradia (a 21-day season, four to the year) so the SDK behaves on its own; the host overwrites
   ///   it at launch, as it does <see cref="Prompts.PromptLore" />.
   /// </summary>
   public static class WorldCalendar
   {
      /// <summary>Calradia's own season length, and the value this resets to.</summary>
      public const int DefaultDaysPerSeason = 21;

      /// <summary>Four seasons to a year, as in Calradia, and the value this resets to.</summary>
      public const int DefaultSeasonsPerYear = 4;

      private static int _daysPerSeason = DefaultDaysPerSeason;
      private static int _seasonsPerYear = DefaultSeasonsPerYear;

      /// <summary>In-game days in one season. A value below 1 is refused, since every window in the mod is measured in these.</summary>
      public static int DaysPerSeason
      {
         get => _daysPerSeason;
         set { if (value >= 1) _daysPerSeason = value; }
      }

      /// <summary>Seasons in one year. A value below 1 is refused.</summary>
      public static int SeasonsPerYear
      {
         get => _seasonsPerYear;
         set { if (value >= 1) _seasonsPerYear = value; }
      }

      /// <summary>In-game days in one year: the season length times the number of seasons, never stored separately so the three can never disagree.</summary>
      public static int DaysPerYear => DaysPerSeason * SeasonsPerYear;

      /// <summary>
      ///   That many seasons, in days. For a tuning value that MEANS "a season" or "three seasons", written this way
      ///   so the intent survives: <c>Seasons(3)</c> says what 63 does not.
      /// </summary>
      public static int Seasons(int count) => count * DaysPerSeason;

      /// <summary>That many years, in days. See <see cref="Seasons" />: <c>Years(2)</c> says what 168 does not.</summary>
      public static int Years(int count) => count * DaysPerYear;

      /// <summary>Back to Calradia. For a test that changed the world, and for a host reloading its setting file.</summary>
      public static void Reset()
      {
         _daysPerSeason = DefaultDaysPerSeason;
         _seasonsPerYear = DefaultSeasonsPerYear;
      }
   }
}

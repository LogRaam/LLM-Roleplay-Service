// Code written by Gabriel Mailhot, 18/10/2026.
// THE MOOD OF THE DAY: why the character is having the day he is having. Lordfadooboo (Nexus, 18/10/2026): "sometimes
// the ai struggles to invent a reason for their mood and engaging with that mechanic can sometimes end the moment its
// addressed. so is it possible to have a list of potential reasons for the daily mood?" The line told the model HOW he
// feels and never WHY, so a player who asked "what troubles you?" met an invention or a shrug.
//
// Gabriel (18/10/2026): the list lives in the code, and the real cause is ALWAYS named when there is one. A real cause
// is a fact of the day that leaned the drawn mood (MoodOfTheDay.Weights): a fact that did not lean it did not cause it,
// so a grim man with a gladness on record is not grim because of it. Of several, the one that leaned it most; a captive's
// is always the captivity. With none, a trifle drawn from the table below by the day's seed (another mix, so the cause
// is not the mood roll over again): the same trifle all day. A trifle names no one, nothing the mod keeps (a kinsman, a
// battle, a fief, a letter) and no weather the player could see on the map, and suits a lord, a companion or a
// notable alike: NPCs never lie, and nobody can check a stubbed toe.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Mood
{
   /// <summary>Why the day is what it is: a fact of the game, or a small private trifle.</summary>
   public sealed record DayCause
   {
      /// <summary>The cause, a noun phrase from the character's side ("your wounds", "a bad night's sleep").</summary>
      public string Text { get; init; } = "";

      /// <summary>True when it is a fact of the game; false for a trifle from the table.</summary>
      public bool IsReal { get; init; }
   }

   /// <summary>Picks the cause of a drawn mood. Pure and seeded.</summary>
   public static class MoodCause
   {
      /// <summary>
      ///   THE TRIFLES, by mood: small private causes for a day no fact explains. Public because it is the design
      ///   document (Gabriel: the list lives in the code). Each one fits anyone, names no one and nothing the game keeps.
      /// </summary>
      public static readonly IReadOnlyDictionary<DayMood, IReadOnlyList<string>> Trifles = BuildTrifles();

      /// <summary>
      ///   The cause of <paramref name="mood" /> on this day, or null for an ordinary day (it renders nothing).
      ///   <paramref name="seed" /> is the day's seed (<see cref="MoodOfTheDay.SeedFor" />), the one the mood was drawn by.
      /// </summary>
      public static DayCause? Of(DayMood mood, MoodFacts? facts, int seed)
      {
         if (mood == DayMood.Even) return null;

         string? real = RealCause(mood, facts ?? new MoodFacts());
         if (real != null) return new DayCause {Text = real, IsReal = true};

         IReadOnlyList<string> list = Trifles[mood];

         return new DayCause {Text = list[MoodOfTheDay.Index(seed ^ 0x3A91, list.Count)], IsReal = false};
      }

      // The fact that leaned this mood the most, by MoodOfTheDay.Weights' own leans (first listed wins a tie). Facts
      // that only set the scene (the hour, being at ease, self-regard) are the ground of a day, not a cause one names.
      private static string? RealCause(DayMood mood, MoodFacts f)
      {
         if (f.IsCaptive) return "being held prisoner";

         var leaned = new List<(int By, string Why)>();
         void If(bool fact, int by, string why)
         {
            if (fact) leaned.Add((by, why));
         }

         string blow = string.IsNullOrWhiteSpace(f.PersonalBlowWhat) ? "a blow of your own these last days" : f.PersonalBlowWhat!.Trim();
         string gladness = string.IsNullOrWhiteSpace(f.PersonalGladnessWhat) ? "a gladness of your own these last days" : f.PersonalGladnessWhat!.Trim();

         switch (mood)
         {
            case DayMood.Grim:
               If(f.PersonalBlow, 6, blow);
               If(f.RealmHasLatelyLost, 4, "your realm's war going badly");
               If(f.IsHurt, 2, "your wounds");
               break;
            case DayMood.ShortTempered:
               If(f.IsHurt, 4, "your wounds");
               If(f.RealmHasLatelyLost, 2, "your realm's war going badly");
               If(f.CoffersAreEmpty, 2, "coin running short");
               break;
            case DayMood.Guarded:
               If(f.CoffersAreEmpty, 3, "coin running short");
               If(f.PersonalBlow, 2, blow);
               If(f.IsMaimed, 2, "the maiming you carry");
               break;
            case DayMood.Expansive:
               If(f.RealmHasLatelyWon, 4, "your realm's war going well");
               If(f.PersonalGladness, 3, gladness);
               break;
            case DayMood.HighSpirits:
               If(f.PersonalGladness, 5, gladness);
               If(f.RealmHasLatelyWon, 3, "your realm's war going well");
               break;
         }

         (int By, string Why)? best = null;
         foreach ((int By, string Why) l in leaned)
            if (best == null || l.By > best.Value.By) best = l;

         return best?.Why;
      }

      private static Dictionary<DayMood, IReadOnlyList<string>> BuildTrifles()
      {
         string[] low = {
            "a bad night's sleep", "an old ache acting up since morning", "a stubbed toe that still smarts", "a meal that sat badly",
            "a headache since waking", "a simple task botched by someone who should have known better", "a quarrel this morning over something small",
            "a dream that left a heaviness you cannot shake", "a boot that has rubbed your heel raw"
         };

         return new Dictionary<DayMood, IReadOnlyList<string>> {
            [DayMood.Grim] = low,
            [DayMood.ShortTempered] = low,
            [DayMood.Guarded] = low,
            [DayMood.Restless] = new[] {
               "too long in one place", "a short night that left you wide awake", "a dream that left you uneasy",
               "a small matter you want settled that will not hurry", "more vigour this morning than the day has use for"
            },
            [DayMood.Expansive] = new[] {
               "a good night's sleep", "a fine meal last evening", "an evening in good company", "no ache anywhere this morning",
               "a small matter settled at last", "a good bargain struck over something small"
            },
            [DayMood.HighSpirits] = new[] {
               "a good night's sleep", "a fine meal last evening", "an evening in good company", "no ache anywhere this morning",
               "a small matter settled at last", "a good bargain struck over something small"
            }
         };
      }
   }
}

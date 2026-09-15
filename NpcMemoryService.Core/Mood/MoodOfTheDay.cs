// Code written by Gabriel Mailhot, 14/09/2026.
// THE MOOD OF THE DAY, increment 1: the pure draw. Design and Gabriel's rulings are in ROADMAP.md.
//
// WHY. Three players have now said the same thing, most recently basildingleberry: "many of them seem to talk
// the same way, focus on politics". His objection to the existing remedy decided the shape - authoring
// character_overrides.json per lord does not scale, so whatever fixes this must cost the player nothing.
// Promised on the pinned Nexus roadmap since 03/07/2026 as "a mood randomizer".
//
// IT IS NOT A RANDOMIZER. A lord who has just lost a fief must not wake up cheerful. What v2.5.9 shipped is
// exactly the material that constrains the draw: own_body knows he is wounded, house_means that his coffers
// are empty, realm_news how his kingdom fares, here_and_now the hour. So this does not INVENT a mood.
//
// WEIGHTS, NOT A WHITELIST (Gabriel, 14/09/2026: "il faut lister toutes les humeurs possibles, et comment
// chacune est influencée"). The first draft only FORBADE, which made every influence negative: a lord whose
// realm had just won was merely *allowed* to be expansive, at the same odds as any other day, and the player
// would never feel it. Each mood now carries a weight that facts raise and lower, and a weight of zero is the
// forbidding rule preserved exactly. One mechanism, and it reads as a table - which is what the feature owes
// anyone tuning it.
//
// WHAT IT MUST NEVER DO, and this is the whole safety of the feature: mood COLOURS delivery and never decides
// an outcome. This project's drop-order principle is that what changes what a character may SAY OR DO outranks
// what colours how he says it; mood is the second kind. A mood that made a lord refuse an oath would be an
// invisible gate a player can neither see nor learn. Nothing here returns a permission.

#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Mood
{
   /// <summary>
   ///   What kind of day this character is having. Deliberately few: each has to be distinguishable in prose
   ///   by a reader who never sees the label, or it is a distinction only the code believes in.
   /// </summary>
   public enum DayMood
   {
      /// <summary>
      ///   An ordinary day. Renders NOTHING, the same affordability property own_body has - most days are
      ///   unremarkable, and a mood that always speaks is weather rather than character.
      /// </summary>
      Even,

      /// <summary>Weighed down. Answers are shorter than the question deserves.</summary>
      Grim,

      /// <summary>Wants this over with. Courtesy is present but thin.</summary>
      ShortTempered,

      /// <summary>Giving nothing away today. Not hostile - closed.</summary>
      Guarded,

      /// <summary>Energy with nowhere to put it. Paces, changes the subject, gets ahead of things.</summary>
      Restless,

      /// <summary>Open-handed and talkative. Says more than he strictly meant to.</summary>
      Expansive,

      /// <summary>
      ///   In genuinely good spirits. The ONLY mood that can permit wit, and even then only rarely - see
      ///   <see cref="MoodOfTheDay.WitIsPermitted" />.
      /// </summary>
      HighSpirits
   }

   /// <summary>How the day is pressing. The host reads these off the live game; every one is optional.</summary>
   public sealed record MoodFacts
   {
      /// <summary>Hurt badly enough that it shows in how they stand (own_body's own reading).</summary>
      public bool IsHurt { get; init; }

      /// <summary>Carrying a permanent maiming, which is a different weight from a wound that heals.</summary>
      public bool IsMaimed { get; init; }

      /// <summary>Their house cannot presently pay its way.</summary>
      public bool CoffersAreEmpty { get; init; }

      /// <summary>Their realm has lately lost ground - a fief fallen, a field lost.</summary>
      public bool RealmHasLatelyLost { get; init; }

      /// <summary>Their realm has lately gained - a fief taken, a war won.</summary>
      public bool RealmHasLatelyWon { get; init; }

      /// <summary>A blow of their own lately: a death in the house, a fief of theirs taken.</summary>
      public bool PersonalBlow { get; init; }

      /// <summary>A gladness of their own lately: a wedding, a birth, an honour.</summary>
      public bool PersonalGladness { get; init; }

      /// <summary>Standing somewhere that permits ease - a feast, a hall at leisure - not a camp at war.</summary>
      public bool AtEase { get; init; }

      /// <summary>The small hours. Nobody is expansive at three in the morning.</summary>
      public bool IsDeepNight { get; init; }

      /// <summary>Held prisoner. Overrides nearly everything else.</summary>
      public bool IsCaptive { get; init; }
   }

   /// <summary>
   ///   The pure draw. Engine-free and seeded, so the same character on the same day is the same man however
   ///   many times this is asked.
   /// </summary>
   public static class MoodOfTheDay
   {
      /// <summary>
      ///   Out of how many conversations a permitted wit may surface. Gabriel: "il faut que ce soit rare." A
      ///   dry remark once in a long while is a moment the player remembers; the same remark every time is a
      ///   verbal tic that cheapens the character, so rarity here is the MECHANISM, not a limitation. This
      ///   gate sits on top of <see cref="DayMood.HighSpirits" />, which is itself uncommon.
      /// </summary>
      public const int WitInOneConversationIn = 4;

      /// <summary>
      ///   THE TABLE. Every mood with the weight this day gives it; zero means the day forbids it outright.
      ///   Public because it IS the design document: anyone tuning this should be able to read what raises and
      ///   lowers each mood without reading the draw.
      ///   <para>
      ///     Base weights are not uniform on purpose. <see cref="DayMood.Even" /> is the commonest because
      ///     most days are ordinary and it renders nothing, which is what keeps the coloured days meaningful
      ///     and the prompt cheap.
      ///   </para>
      /// </summary>
      public static IReadOnlyDictionary<DayMood, int> Weights(MoodFacts facts)
      {
         var w = new Dictionary<DayMood, int> {
            [DayMood.Even] = 6,
            [DayMood.Grim] = 2,
            [DayMood.ShortTempered] = 3,
            [DayMood.Guarded] = 3,
            [DayMood.Restless] = 3,
            [DayMood.Expansive] = 2,
            [DayMood.HighSpirits] = 2
         };

         if (facts == null) return w;

         // ── what forbids outright (weight 0) ───────────────────────────────────────────────────────
         // A captive is not having a mood, he is having a captivity.
         if (facts.IsCaptive) Forbid(w, DayMood.HighSpirits, DayMood.Expansive, DayMood.Restless, DayMood.Even);

         // A man with a spear wound does not have an unremarkable afternoon.
         if (facts.IsHurt) Forbid(w, DayMood.HighSpirits, DayMood.Expansive, DayMood.Even);

         // A maiming is CARRIED, not suffered afresh: it forbids lightness without forbidding composure -
         // which is exactly the man whose dry remark about his own ruined hand shows it has not beaten him.
         if (facts.IsMaimed) Forbid(w, DayMood.HighSpirits);

         if (facts.PersonalBlow) Forbid(w, DayMood.HighSpirits, DayMood.Expansive, DayMood.Even);
         if (facts.RealmHasLatelyLost) Forbid(w, DayMood.HighSpirits, DayMood.Expansive);
         if (facts.CoffersAreEmpty) Forbid(w, DayMood.Expansive); // no one is open-handed with nothing
         if (facts.IsDeepNight) Forbid(w, DayMood.Expansive, DayMood.HighSpirits);
         if (!facts.AtEase) Forbid(w, DayMood.HighSpirits); // good spirits need somewhere to happen

         // ── what LEANS, which is the half the first draft was missing ──────────────────────────────
         if (facts.RealmHasLatelyLost) { Lean(w, DayMood.Grim, 4); Lean(w, DayMood.ShortTempered, 2); }
         if (facts.PersonalBlow) { Lean(w, DayMood.Grim, 6); Lean(w, DayMood.Guarded, 2); }
         if (facts.IsHurt) { Lean(w, DayMood.ShortTempered, 4); Lean(w, DayMood.Grim, 2); }
         if (facts.IsMaimed) Lean(w, DayMood.Guarded, 2);
         if (facts.CoffersAreEmpty) { Lean(w, DayMood.Guarded, 3); Lean(w, DayMood.ShortTempered, 2); }
         if (facts.IsCaptive) { Lean(w, DayMood.Grim, 6); Lean(w, DayMood.Guarded, 4); }
         if (facts.IsDeepNight) { Lean(w, DayMood.Guarded, 2); Lean(w, DayMood.Restless, 2); }
         if (!facts.AtEase) Lean(w, DayMood.Restless, 2);

         if (facts.RealmHasLatelyWon) { Lean(w, DayMood.Expansive, 4); Lean(w, DayMood.HighSpirits, 3); Dampen(w, DayMood.Grim); }
         if (facts.PersonalGladness) { Lean(w, DayMood.HighSpirits, 5); Lean(w, DayMood.Expansive, 3); Dampen(w, DayMood.Grim); }
         if (facts.AtEase) { Lean(w, DayMood.Expansive, 2); Lean(w, DayMood.HighSpirits, 2); }

         // The floor. Grim is never forbidden above, but a future rule could, and an all-zero table would make
         // the draw throw on somebody's save rather than fail here where it can be seen.
         if (w.Values.All(v => v <= 0)) w[DayMood.Grim] = 1;

         return w;
      }

      /// <summary>Every mood this day allows at all, in enum order. A convenience read of <see cref="Weights" />.</summary>
      public static IReadOnlyList<DayMood> Permitted(MoodFacts facts)
         => Weights(facts).Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();

      /// <summary>
      ///   The mood itself, drawn against the weights. <paramref name="seed" /> must be built from the hero and
      ///   the DAY, never the turn: stable within a conversation or the character reads as manic, different
      ///   between days or this is just another fixed trait. Talking to the same man twice in one afternoon
      ///   finds the same man, which is right - a person's day does not reset because you walked out and back.
      /// </summary>
      public static DayMood Draw(MoodFacts facts, int seed)
      {
         IReadOnlyDictionary<DayMood, int> w = Weights(facts);
         int total = w.Values.Where(v => v > 0).Sum();

         if (total <= 0) return DayMood.Grim;

         int roll = Index(seed, total);

         foreach (KeyValuePair<DayMood, int> kv in w)
         {
            if (kv.Value <= 0) continue;
            if (roll < kv.Value) return kv.Key;

            roll -= kv.Value;
         }

         return DayMood.Grim; // unreachable while total > 0; a return rather than a throw on a live save
      }

      /// <summary>
      ///   Whether a dry remark may surface at all. Three gates, all of which must pass: the day is genuinely
      ///   a good one, the rarity draw allows it, and the scene is not one where levity is grotesque.
      ///   <para>
      ///     Uses a DIFFERENT mixing of the seed from <see cref="Draw" />, so wit is not merely a property of
      ///     the roll that produced the mood - otherwise every high-spirited day would be a witty one and
      ///     "rare" would quietly mean "whenever he is cheerful".
      ///   </para>
      /// </summary>
      public static bool WitIsPermitted(DayMood mood, MoodFacts facts, int seed)
      {
         if (mood != DayMood.HighSpirits) return false;
         if (facts is {IsCaptive: true}) return false;

         return Index(seed ^ 0x5F37, WitInOneConversationIn) == 0;
      }

      #region private

      private static void Forbid(IDictionary<DayMood, int> w, params DayMood[] moods)
      {
         foreach (DayMood m in moods) w[m] = 0;
      }

      /// <summary>Raises a mood's weight, but never revives one the day has forbidden outright.</summary>
      private static void Lean(IDictionary<DayMood, int> w, DayMood mood, int by)
      {
         if (w[mood] > 0) w[mood] += by;
      }

      /// <summary>Halves a mood's weight without forbidding it: gladness makes bleakness unlikely, not impossible.</summary>
      private static void Dampen(IDictionary<DayMood, int> w, DayMood mood) => w[mood] = w[mood] / 2;

      /// <summary>
      ///   A small deterministic mix. Not cryptography and not trying to be: it must only be stable for a
      ///   given seed, well spread, and identical on every machine - which rules out
      ///   <c>string.GetHashCode</c>, randomised per process since .NET Core.
      /// </summary>
      private static int Index(int seed, int count)
      {
         if (count <= 1) return 0;

         unchecked
         {
            var h = (uint) seed;
            h ^= h >> 16;
            h *= 0x7FEB352D;
            h ^= h >> 15;
            h *= 0x846CA68B;
            h ^= h >> 16;

            return (int) (h % (uint) count);
         }
      }

      #endregion
   }
}

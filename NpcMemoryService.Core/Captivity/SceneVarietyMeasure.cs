// Code written by Gabriel Mailhot, 24/07/2026.
// AUDIT-CAPTIVE-STYLE T6: the inter-scene variety metric. T1 (rotating writing lens), T3 (de-listified
// sensory blocks) and T4 (memory summaries drop the vivid prose) all aim at ONE reported fault: captive
// scenes reused the same distinctive phrasings from one scene to the next ("sweet", "the tendons of his
// neck"). Those fixes are unverifiable without a number that measures cross-scene echo, which is what this
// pure measure provides. It lives in Core (shared by both the mod and the SDK) so the in-game harness
// (CrSelfTest, net472) and the live SDK harness (LiveLlmHarness, net10) score identically, and so the
// scoring itself is unit-tested rather than trusted.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace NpcMemoryService.Core.Captivity
{
   /// <summary>
   ///   One distinctive phrase (word n-gram) that recurs across more than one scene, with the number of
   ///   DISTINCT scenes it appears in. This is the unit of cross-scene echo the audit is about: a phrase in
   ///   a single scene is fine, the same phrase surfacing scene after scene is the flatness players reported.
   /// </summary>
   public sealed class ScenePhraseRecurrence
   {
      public ScenePhraseRecurrence(string phrase, int sceneCount)
      {
         Phrase = phrase;
         SceneCount = sceneCount;
      }

      /// <summary>The normalized n-gram (lowercased, punctuation stripped), e.g. "the tendons of his neck".</summary>
      public string Phrase { get; }

      /// <summary>How many DISTINCT scenes contain this phrase (always >= 2 for a recurrence).</summary>
      public int SceneCount { get; }
   }

   /// <summary>
   ///   The result of scoring a corpus of scene texts for cross-scene phrase reuse. Lower is more varied.
   ///   <see cref="CrossSceneReuseCount" /> is the headline number a threshold test asserts against;
   ///   <see cref="SharedPhrases" /> lets a human reviewer (Gabriel reads the numbers) see WHICH phrasings
   ///   the model is leaning on so a tuning pass is informed, not blind.
   /// </summary>
   public sealed class SceneVarietyReport
   {
      public SceneVarietyReport(int nGramSize, int sceneCount, int distinctPhraseCount, IReadOnlyList<ScenePhraseRecurrence> sharedPhrases)
      {
         NGramSize = nGramSize;
         SceneCount = sceneCount;
         DistinctPhraseCount = distinctPhraseCount;
         SharedPhrases = sharedPhrases;
      }

      /// <summary>The n-gram width used (number of contiguous words per phrase).</summary>
      public int NGramSize { get; }

      /// <summary>How many scenes were scored.</summary>
      public int SceneCount { get; }

      /// <summary>Total distinct n-grams across the whole corpus (the denominator for <see cref="ReuseRatio" />).</summary>
      public int DistinctPhraseCount { get; }

      /// <summary>
      ///   Every distinctive phrase that appears in two or more scenes, ordered worst first (most scenes,
      ///   then alphabetically for a stable report).
      /// </summary>
      public IReadOnlyList<ScenePhraseRecurrence> SharedPhrases { get; }

      /// <summary>The headline: how many distinct phrases recur across scenes. 0 means no cross-scene echo.</summary>
      public int CrossSceneReuseCount => SharedPhrases.Count;

      /// <summary>
      ///   Cross-scene reuse as a fraction of all distinct phrasing (0 = perfectly varied, higher = more
      ///   echo). Normalizing by corpus size keeps the number comparable whether 3 or 30 scenes were scored.
      /// </summary>
      public double ReuseRatio => DistinctPhraseCount == 0
         ? 0d
         : (double) CrossSceneReuseCount / DistinctPhraseCount;

      /// <summary>The worst <paramref name="top" /> recurring phrases, for a compact report line.</summary>
      public IReadOnlyList<ScenePhraseRecurrence> WorstOffenders(int top)
         => SharedPhrases.Take(Math.Max(0, top)).ToList();
   }

   /// <summary>
   ///   Scores how much distinctive PHRASING recurs across a set of scene texts (the T6 metric). It looks for
   ///   contiguous word runs (n-grams) that show up in more than one scene: a 4-word verbatim run shared by
   ///   two independently generated scenes is almost never coincidence, it is the model reaching for the same
   ///   turn of phrase, which is exactly the cross-scene sameness the style pass fights. Function-word runs
   ///   ("out of the corner") are the metric's only real noise, so callers keep n at 4+ and read the reported
   ///   phrases, not the raw count alone.
   /// </summary>
   public static class SceneVarietyMeasure
   {
      /// <summary>
      ///   The default n-gram width. Four words is long enough that an ordinary shared run is rarely
      ///   accidental, yet short enough to catch the reported signature phrases ("the tendons of his neck"),
      ///   which a 6-word window (CrSelfTest's intra-beat verbatim check) would miss.
      /// </summary>
      public const int DefaultNGramSize = 4;

      /// <summary>
      ///   Scores <paramref name="scenes" /> for cross-scene phrase reuse. Each scene is the full concatenated
      ///   prose of one encounter. A phrase is counted once per scene it appears in (a phrase repeated WITHIN a
      ///   scene is that scene's own business; T6 is about echo BETWEEN scenes), and reported only when it
      ///   spans two or more scenes.
      /// </summary>
      public static SceneVarietyReport Measure(IEnumerable<string> scenes, int nGramSize = DefaultNGramSize)
      {
         if (nGramSize < 1) throw new ArgumentOutOfRangeException(nameof(nGramSize), "n-gram width must be at least 1.");

         List<HashSet<string>> perScene = (scenes ?? Enumerable.Empty<string>())
                                         .Select(s => NGrams(s, nGramSize))
                                         .ToList();

         // sceneCount per distinct phrase: how many scenes contain it at least once.
         var sceneCountByPhrase = new Dictionary<string, int>();
         foreach (HashSet<string> grams in perScene)
            foreach (string gram in grams)
               sceneCountByPhrase[gram] = sceneCountByPhrase.TryGetValue(gram, out int c)
                  ? c + 1
                  : 1;

         List<ScenePhraseRecurrence> shared = sceneCountByPhrase
                                             .Where(kv => kv.Value >= 2)
                                             .Select(kv => new ScenePhraseRecurrence(kv.Key, kv.Value))
                                             .OrderByDescending(r => r.SceneCount)
                                             .ThenBy(r => r.Phrase, StringComparer.Ordinal)
                                             .ToList();

         return new SceneVarietyReport(nGramSize, perScene.Count, sceneCountByPhrase.Count, shared);
      }

      /// <summary>The distinct word n-grams of one text, normalized. Empty when the text has fewer than n words.</summary>
      public static HashSet<string> NGrams(string text, int nGramSize)
      {
         var grams = new HashSet<string>();
         if (nGramSize < 1) return grams;

         string[] words = Normalize(text).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
         for (var i = 0; i + nGramSize <= words.Length; i++)
            grams.Add(string.Join(" ", words, i, nGramSize));

         return grams;
      }

      /// <summary>
      ///   Lowercases and reduces every non-letter/digit to a single space, so phrase comparison ignores
      ///   punctuation, casing, and quote marks. Mirrors CrSelfTest's own NormalizeForCompare so the in-game
      ///   and offline scores are on the same footing.
      /// </summary>
      public static string Normalize(string text)
      {
         if (string.IsNullOrEmpty(text)) return string.Empty;

         var sb = new StringBuilder(text.Length);
         foreach (char ch in text)
            sb.Append(char.IsLetterOrDigit(ch)
               ? char.ToLowerInvariant(ch)
               : ' ');

         return sb.ToString();
      }
   }
}

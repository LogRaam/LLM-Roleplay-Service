// Code written by Gabriel Mailhot, 17/08/2026.
// The pure judge of the interpreter extraction bench: given the actions an interpreter emitted for a case's prose,
// and the case's expectation, it returns a single verdict. Pure so the scoring rules (what counts as a hit, how a
// parameter is matched, when a withhold is correct) are unit-tested with synthetic action lists, no LLM needed;
// the real interpreter run feeds its parsed actions in here elsewhere.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>Scores one interpreter result (the actions it emitted) against one <see cref="ActionBenchCase" />.</summary>
   public static class ActionBenchScorer
   {
      /// <summary>
      ///   Judges <paramref name="emitted" /> against <paramref name="test" />. A NEGATIVE case is a
      ///   <see cref="ActionBenchVerdict.FalsePositive" /> if the forbidden type appears at all, else a
      ///   <see cref="ActionBenchVerdict.CorrectWithhold" />. A POSITIVE case scores over ALL its expected actions:
      ///   <see cref="ActionBenchVerdict.Hit" /> when every one is emitted with matching params;
      ///   <see cref="ActionBenchVerdict.Miss" /> when none of the expected types appear at all;
      ///   <see cref="ActionBenchVerdict.ParamMismatch" /> when every expected type appears but a value is wrong or
      ///   missing; and <see cref="ActionBenchVerdict.PartialHit" /> when some expected actions landed and others
      ///   were dropped (the "1 of 2" failure a multi-action case exists to catch).
      /// </summary>
      public static ActionBenchVerdict Score(IReadOnlyList<GameAction> emitted, ActionBenchCase test)
      {
         if (test == null) throw new ArgumentNullException(nameof(test));
         if (emitted == null) emitted = new List<GameAction>();

         if (test.IsNegative)
            return emitted.Any(a => TypeEquals(a.Type, test.ForbiddenType))
               ? ActionBenchVerdict.FalsePositive
               : ActionBenchVerdict.CorrectWithhold;

         int total = test.Expected.Count;
         int typePresent = test.Expected.Count(e => emitted.Any(a => TypeEquals(a.Type, e.Type)));
         int matched = test.Expected.Count(e =>
            emitted.Any(a => TypeEquals(a.Type, e.Type) && AllParamsMatch(a, e.Params)));

         if (matched == total) return ActionBenchVerdict.Hit;
         if (typePresent == 0) return ActionBenchVerdict.Miss;
         if (typePresent == total) return ActionBenchVerdict.ParamMismatch;

         return ActionBenchVerdict.PartialHit;
      }

      #region private

      private static bool TypeEquals(string a, string b)
         => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

      private static bool AllParamsMatch(GameAction action, IReadOnlyDictionary<string, string> expected)
      {
         foreach (KeyValuePair<string, string> want in expected)
         {
            if (action.Parameters == null || !action.Parameters.TryGetValue(want.Key, out string actual))
               return false;

            if (!ValueMatches(want.Value, actual)) return false;
         }

         return true;
      }

      // A numeric expectation (an amount, a delta) must match exactly, digits are unforgiving. A textual one (a
      // captive's name, a fief) is matched TOLERANTLY, case-insensitive substring, because a model legitimately
      // writes "Lord Derthert" for an expected "Derthert" and that is a correct extraction, not a mismatch.
      private static bool ValueMatches(string expected, string actual)
      {
         string want = (expected ?? string.Empty).Trim();
         string got = (actual ?? string.Empty).Trim();

         if (IsSignedInteger(want))
            return string.Equals(want, got, StringComparison.Ordinal);

         return got.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0;
      }

      private static bool IsSignedInteger(string s)
      {
         if (string.IsNullOrEmpty(s)) return false;

         int start = s[0] == '-' || s[0] == '+' ? 1 : 0;
         if (start == s.Length) return false;

         for (int i = start; i < s.Length; i++)
            if (!char.IsDigit(s[i]))
               return false;

         return true;
      }

      #endregion
   }
}

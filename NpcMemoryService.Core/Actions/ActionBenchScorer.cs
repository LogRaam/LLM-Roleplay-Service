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
      ///   <see cref="ActionBenchVerdict.CorrectWithhold" />. A POSITIVE case is a
      ///   <see cref="ActionBenchVerdict.Miss" /> if the expected type never appears, a
      ///   <see cref="ActionBenchVerdict.Hit" /> if any emitted action of that type carries every required
      ///   parameter, otherwise a <see cref="ActionBenchVerdict.ParamMismatch" /> (the deed was recognised but a
      ///   value was wrong or missing, a softer failure than a plain miss and worth telling apart).
      /// </summary>
      public static ActionBenchVerdict Score(IReadOnlyList<GameAction> emitted, ActionBenchCase test)
      {
         if (test == null) throw new ArgumentNullException(nameof(test));
         if (emitted == null) emitted = new List<GameAction>();

         if (test.IsNegative)
            return emitted.Any(a => TypeEquals(a.Type, test.ForbiddenType))
               ? ActionBenchVerdict.FalsePositive
               : ActionBenchVerdict.CorrectWithhold;

         List<GameAction> ofExpectedType = emitted.Where(a => TypeEquals(a.Type, test.ExpectedType)).ToList();

         if (ofExpectedType.Count == 0) return ActionBenchVerdict.Miss;

         return ofExpectedType.Any(a => AllParamsMatch(a, test.ExpectedParams))
            ? ActionBenchVerdict.Hit
            : ActionBenchVerdict.ParamMismatch;
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

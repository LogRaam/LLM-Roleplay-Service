// Code written by Gabriel Mailhot, 17/08/2026.
// The result of running the interpreter extraction bench: one ActionBenchResult per case, and an ActionBenchReport
// that aggregates them into a scoreboard. The aggregation and the human-readable formatting are PURE (fed synthetic
// results in tests, no LLM), so the numbers a run prints are trustworthy; ActionBenchRunner does the actual LLM
// calls that produce the results.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>One case's outcome after the interpreter ran on it: the emitted actions and the scored verdict.</summary>
   public sealed class ActionBenchResult
   {
      public ActionBenchResult(ActionBenchCase test, bool callSucceeded, string error,
         IReadOnlyList<GameAction> emitted, ActionBenchVerdict verdict)
      {
         Case = test;
         CallSucceeded = callSucceeded;
         Error = error;
         Emitted = emitted ?? new List<GameAction>();
         Verdict = verdict;
      }

      /// <summary>The case that was run.</summary>
      public ActionBenchCase Case { get; }

      /// <summary>False when the interpreter LLM call itself failed (transport, timeout): its verdict is not meaningful.</summary>
      public bool CallSucceeded { get; }

      /// <summary>The call error, when <see cref="CallSucceeded" /> is false.</summary>
      public string Error { get; }

      /// <summary>The actions the interpreter emitted (empty on a failed call).</summary>
      public IReadOnlyList<GameAction> Emitted { get; }

      /// <summary>The scored verdict; only meaningful when <see cref="CallSucceeded" />.</summary>
      public ActionBenchVerdict Verdict { get; }

      /// <summary>A pass is a call that succeeded AND either hit the expected action or correctly withheld a forbidden one.</summary>
      public bool IsPass => CallSucceeded
                         && (Verdict == ActionBenchVerdict.Hit || Verdict == ActionBenchVerdict.CorrectWithhold);
   }

   /// <summary>Aggregates <see cref="ActionBenchResult" />s into totals and a readable scoreboard.</summary>
   public sealed class ActionBenchReport
   {
      public ActionBenchReport(IReadOnlyList<ActionBenchResult> results)
      {
         Results = results ?? new List<ActionBenchResult>();

         foreach (ActionBenchResult r in Results)
         {
            if (!r.CallSucceeded)
            {
               CallFailures++;
               continue;
            }

            switch (r.Verdict)
            {
               case ActionBenchVerdict.Hit: Hits++; break;
               case ActionBenchVerdict.Miss: Misses++; break;
               case ActionBenchVerdict.ParamMismatch: ParamMismatches++; break;
               case ActionBenchVerdict.CorrectWithhold: CorrectWithholds++; break;
               case ActionBenchVerdict.FalsePositive: FalsePositives++; break;
            }
         }
      }

      /// <summary>Every case's result, in the order they were run.</summary>
      public IReadOnlyList<ActionBenchResult> Results { get; }

      public int Total => Results.Count;
      public int CallFailures { get; }
      public int Hits { get; }
      public int Misses { get; }
      public int ParamMismatches { get; }
      public int CorrectWithholds { get; }
      public int FalsePositives { get; }

      /// <summary>Positive cases that hit, plus negative cases that correctly withheld.</summary>
      public int Passed => Hits + CorrectWithholds;

      /// <summary>
      ///   A compact scoreboard: a totals line, then one line per NON-passing case (the actionable part, what to
      ///   go tighten in the shared catalog). A call failure is listed too, so a run that spent tokens on nothing
      ///   is never mistaken for a clean pass.
      /// </summary>
      public string Format()
      {
         var sb = new StringBuilder();

         int positives = Hits + Misses + ParamMismatches;
         int negatives = CorrectWithholds + FalsePositives;

         sb.Append("ACTION BENCH: ").Append(Passed).Append('/').Append(Total).AppendLine(" passed")
           .Append("  positives: ").Append(Hits).Append('/').Append(positives).Append(" hit")
           .Append(" (").Append(Misses).Append(" missed, ").Append(ParamMismatches).AppendLine(" param-mismatch)")
           .Append("  negatives: ").Append(CorrectWithholds).Append('/').Append(negatives).AppendLine(" withheld")
           .Append("  call failures: ").Append(CallFailures).AppendLine();

         List<ActionBenchResult> failing = Results.Where(r => !r.IsPass).ToList();
         if (failing.Count == 0)
         {
            sb.AppendLine("All cases passed.");

            return sb.ToString();
         }

         sb.AppendLine("FAILURES:");
         foreach (ActionBenchResult r in failing)
            sb.Append("  ").Append(Label(r).PadRight(16)).Append(r.Case.Verb.PadRight(22)).AppendLine(Detail(r));

         return sb.ToString();
      }

      #region private

      private static string Label(ActionBenchResult r)
         => r.CallSucceeded ? r.Verdict.ToString() : "CALL_FAILED";

      private static string Detail(ActionBenchResult r)
      {
         if (!r.CallSucceeded) return "(" + (r.Error ?? "no result") + ")";

         switch (r.Verdict)
         {
            case ActionBenchVerdict.Miss:
               return "expected " + r.Case.ExpectedType + ", emitted [" + EmittedTypes(r) + "]";
            case ActionBenchVerdict.ParamMismatch:
               return "expected " + r.Case.ExpectedType + " " + Params(r.Case.ExpectedParams);
            case ActionBenchVerdict.FalsePositive:
               return "wrongly emitted " + r.Case.ForbiddenType;
            default:
               return r.Case.Id;
         }
      }

      private static string EmittedTypes(ActionBenchResult r)
         => r.Emitted.Count == 0 ? "" : string.Join(", ", r.Emitted.Select(a => a.Type));

      private static string Params(IReadOnlyDictionary<string, string> p)
         => p.Count == 0 ? "" : string.Join(", ", p.Select(kv => kv.Key + "=" + kv.Value));

      #endregion
   }
}

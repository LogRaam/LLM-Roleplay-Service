// Code written by Gabriel Mailhot, 17/08/2026.
// The result of running the interpreter extraction bench: one ActionBenchResult per case RUN (a case may be run
// several times, one per pass), and an ActionBenchReport that aggregates them into a scoreboard. With more than one
// pass a case is judged by MAJORITY, so a single unlucky run of a nondeterministic model does not read as a stable
// failure; that is what separates a real gap to tune from grok's dice. The aggregation and formatting are PURE (fed
// synthetic results in tests, no LLM); ActionBenchRunner does the actual LLM calls that produce the results.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>One case RUN's outcome after the interpreter ran on it: the emitted actions and the scored verdict.</summary>
   public sealed class ActionBenchResult
   {
      public ActionBenchResult(ActionBenchCase test, bool callSucceeded, string error,
         IReadOnlyList<GameAction> emitted, ActionBenchVerdict verdict, string emittedEvent = null)
      {
         Case = test;
         CallSucceeded = callSucceeded;
         Error = error;
         Emitted = emitted ?? new List<GameAction>();
         Verdict = verdict;
         EmittedEvent = emittedEvent;
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

      /// <summary>
      ///   The [EVENT] the interpreter emitted, if any ("type: summary"), else null. Surfaced so a MISS can reveal
      ///   the interpreter recorded an EVENT instead of the expected action (e.g. a romantic beat logged as an
      ///   intimacy EVENT rather than take_as_secret_lover), which the action-only tally would otherwise hide.
      /// </summary>
      public string EmittedEvent { get; }

      /// <summary>A pass is a call that succeeded AND either hit every expected action or correctly withheld a forbidden one.</summary>
      public bool IsPass => CallSucceeded
                         && (Verdict == ActionBenchVerdict.Hit || Verdict == ActionBenchVerdict.CorrectWithhold);
   }

   /// <summary>Aggregates <see cref="ActionBenchResult" />s (possibly several runs per case) into a scoreboard.</summary>
   public sealed class ActionBenchReport
   {
      private readonly List<IGrouping<string, ActionBenchResult>> _byCase;

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
               case ActionBenchVerdict.PartialHit: PartialHits++; break;
               case ActionBenchVerdict.Miss: Misses++; break;
               case ActionBenchVerdict.ParamMismatch: ParamMismatches++; break;
               case ActionBenchVerdict.CorrectWithhold: CorrectWithholds++; break;
               case ActionBenchVerdict.FalsePositive: FalsePositives++; break;
            }
         }

         _byCase = Results.GroupBy(r => r.Case.Id).ToList();
         CasesPassed = _byCase.Count(PassesByMajority);
         CasesSolid = _byCase.Count(AllPass);
      }

      /// <summary>Every case run's result, in the order they were run.</summary>
      public IReadOnlyList<ActionBenchResult> Results { get; }

      /// <summary>Total case RUNS (cases times passes).</summary>
      public int TotalRuns => Results.Count;

      /// <summary>Distinct cases (a case run several times counts once).</summary>
      public int TotalCases => _byCase.Count;

      /// <summary>Cases that passed by majority of their runs (includes the flaky ones that did not pass every run).</summary>
      public int CasesPassed { get; }

      /// <summary>Cases that passed EVERY run (100%). A passed-but-not-solid case is flaky and still worth refining.</summary>
      public int CasesSolid { get; }

      /// <summary>Cases that passed by majority but not unanimously: noise remains on them, so they can be refined further.</summary>
      public int CasesFlaky => CasesPassed - CasesSolid;

      public int CallFailures { get; }
      public int Hits { get; }
      public int PartialHits { get; }
      public int Misses { get; }
      public int ParamMismatches { get; }
      public int CorrectWithholds { get; }
      public int FalsePositives { get; }

      /// <summary>
      ///   A compact scoreboard: a totals line over cases (pass by majority) and runs, then one line per case that
      ///   did NOT pass the majority of its runs, with its pass rate and a representative failure, the actionable
      ///   part to go tighten in the shared catalog. A call failure is surfaced too, so a run that spent tokens on
      ///   nothing is never mistaken for a clean pass.
      /// </summary>
      public string Format()
      {
         var sb = new StringBuilder();

         int passesPer = TotalCases > 0 ? TotalRuns / TotalCases : 0;

         sb.Append("ACTION BENCH: ").Append(CasesPassed).Append('/').Append(TotalCases).Append(" cases passed");
         if (passesPer > 1) sb.Append(" (majority of ").Append(passesPer).Append(" passes each)");
         sb.AppendLine();
         sb.Append("  ").Append(CasesSolid).Append(" solid (100%), ").Append(CasesFlaky)
           .Append(" flaky (passed but noisy), ").Append(TotalCases - CasesPassed).Append(" failing").AppendLine();
         sb.Append("  runs: ").Append(Hits).Append(" hit, ").Append(PartialHits).Append(" partial, ")
           .Append(Misses).Append(" miss, ").Append(ParamMismatches).Append(" param-mismatch, ")
           .Append(CorrectWithholds).Append(" withheld, ").Append(FalsePositives).Append(" false-positive")
           .AppendLine();
         sb.Append("  call failures: ").Append(CallFailures).AppendLine();

         List<IGrouping<string, ActionBenchResult>> flaky =
            _byCase.Where(g => PassesByMajority(g) && !AllPass(g)).ToList();
         List<IGrouping<string, ActionBenchResult>> failing = _byCase.Where(g => !PassesByMajority(g)).ToList();

         if (flaky.Count == 0 && failing.Count == 0)
         {
            sb.AppendLine("All cases passed cleanly.");

            return sb.ToString();
         }

         // Flaky first: these already pass, but the remaining noise on them is exactly what can still be refined
         // (a tell or anti-pattern that only lands most of the time), so they are the next lever after the failures.
         if (flaky.Count > 0)
         {
            sb.AppendLine("FLAKY (passed but not unanimous, still refineable):");
            foreach (IGrouping<string, ActionBenchResult> g in flaky) AppendCaseLine(sb, g);
         }

         if (failing.Count > 0)
         {
            sb.AppendLine("FAILURES (by case):");
            foreach (IGrouping<string, ActionBenchResult> g in failing) AppendCaseLine(sb, g);
         }

         return sb.ToString();
      }

      private static void AppendCaseLine(StringBuilder sb, IGrouping<string, ActionBenchResult> caseRuns)
      {
         int passes = caseRuns.Count(r => r.IsPass);
         int runs = caseRuns.Count();
         ActionBenchResult sample = caseRuns.FirstOrDefault(r => !r.IsPass) ?? caseRuns.First();

         sb.Append("  ").Append(Label(sample).PadRight(16)).Append(sample.Case.Id.PadRight(34))
           .Append(passes).Append('/').Append(runs).Append("  ").AppendLine(Detail(sample));
      }

      #region private

      private static bool PassesByMajority(IGrouping<string, ActionBenchResult> caseRuns)
      {
         int passes = caseRuns.Count(r => r.IsPass);

         return passes * 2 > caseRuns.Count();
      }

      private static bool AllPass(IGrouping<string, ActionBenchResult> caseRuns) => caseRuns.All(r => r.IsPass);

      private static string Label(ActionBenchResult r)
         => r.CallSucceeded ? r.Verdict.ToString() : "CALL_FAILED";

      private static string Detail(ActionBenchResult r)
      {
         if (!r.CallSucceeded) return "(" + (r.Error ?? "no result") + ")";

         string evt = string.IsNullOrEmpty(r.EmittedEvent) ? "" : " (+EVENT " + r.EmittedEvent + ")";

         switch (r.Verdict)
         {
            case ActionBenchVerdict.Miss:
            case ActionBenchVerdict.PartialHit:
               return "expected [" + ExpectedTypes(r) + "], emitted [" + EmittedTypes(r) + "]" + evt;
            case ActionBenchVerdict.ParamMismatch:
               return "expected " + ExpectedWithParams(r) + evt;
            case ActionBenchVerdict.FalsePositive:
               return "wrongly emitted " + r.Case.ForbiddenType + evt;
            default:
               return r.Case.Id + evt;
         }
      }

      private static string ExpectedTypes(ActionBenchResult r)
         => string.Join(", ", r.Case.Expected.Select(e => e.Type));

      private static string ExpectedWithParams(ActionBenchResult r)
         => string.Join(", ", r.Case.Expected.Select(e =>
            e.Params.Count == 0 ? e.Type : e.Type + " " + string.Join("/", e.Params.Select(kv => kv.Key + "=" + kv.Value))));

      private static string EmittedTypes(ActionBenchResult r)
         => r.Emitted.Count == 0 ? "" : string.Join(", ", r.Emitted.Select(a => a.Type));

      #endregion
   }
}

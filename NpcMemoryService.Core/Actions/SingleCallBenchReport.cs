// Code written by Gabriel Mailhot, 19/08/2026.
// Pure tallies + scoreboard for the single-call bench. It groups the per-pass results by scenario, judges each scenario
// by MAJORITY (so a noisy model's dice do not read as a stable divergence), and reports the headline the ship decision
// turns on: the share of scenarios where the single call's own tags AGREE with the interpreter's on the same authored
// prose. A high agreement rate means one call can do the job of two; the divergences are the worklist. Never scores a
// failed call as agreement - a single-call or interpreter failure is surfaced, not hidden.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>One single-call bench run: the authored prose, both tag sets, and their agreement (or a failure note).</summary>
   public sealed class SingleCallBenchResult
   {
      /// <summary>Creates a scored result (call succeeded, agreement computed).</summary>
      public SingleCallBenchResult(string scenarioId, string prose, IReadOnlyList<string> selfTypes,
         IReadOnlyList<string> interpreterTypes, SingleCallAgreementResult agreement)
      {
         ScenarioId = scenarioId;
         Prose = prose;
         SelfTypes = selfTypes ?? new List<string>();
         InterpreterTypes = interpreterTypes ?? new List<string>();
         Agreement = agreement;
      }

      /// <summary>Creates a failed result (a call failed or the prose was empty), never scored as agreement.</summary>
      public SingleCallBenchResult(string scenarioId, string failureNote)
      {
         ScenarioId = scenarioId;
         FailureNote = failureNote;
         SelfTypes = new List<string>();
         InterpreterTypes = new List<string>();
      }

      /// <summary>The scenario this result belongs to.</summary>
      public string ScenarioId { get; }

      /// <summary>The prose the single call authored (for the reader), or null on failure.</summary>
      public string? Prose { get; }

      /// <summary>The action types the single call self-emitted.</summary>
      public IReadOnlyList<string> SelfTypes { get; }

      /// <summary>The action types the interpreter extracted from the same prose.</summary>
      public IReadOnlyList<string> InterpreterTypes { get; }

      /// <summary>The agreement verdict, or null if this run failed before it could be scored.</summary>
      public SingleCallAgreementResult? Agreement { get; }

      /// <summary>Non-null when this run failed (single-call error, empty prose, or interpreter error).</summary>
      public string? FailureNote { get; }

      /// <summary>True when the run was scored (both calls succeeded and produced usable output).</summary>
      public bool Scored => FailureNote == null && Agreement != null;
   }

   /// <summary>Aggregates single-call bench results into a majority-per-scenario scoreboard.</summary>
   public sealed class SingleCallBenchReport
   {
      private readonly List<SingleCallBenchResult> _results;

      /// <summary>Builds a report over all per-pass results.</summary>
      public SingleCallBenchReport(IReadOnlyList<SingleCallBenchResult> results)
      {
         _results = results?.ToList() ?? new List<SingleCallBenchResult>();
      }

      /// <summary>Distinct scenarios that produced at least one result.</summary>
      public int TotalScenarios => _results.Select(r => r.ScenarioId).Distinct().Count();

      /// <summary>Scenarios whose MAJORITY of scored passes agreed (self-tags == interpreter-tags on the prose).</summary>
      public int ScenariosAgreeing => _results
         .GroupBy(r => r.ScenarioId)
         .Count(g => MajorityAgrees(g));

      /// <summary>Scenarios where at least one pass failed before it could be scored.</summary>
      public int ScenariosWithFailures => _results
         .GroupBy(r => r.ScenarioId)
         .Count(g => g.Any(r => !r.Scored));

      /// <summary>Renders the full scoreboard: headline agreement, then a line per scenario, divergences first.</summary>
      public string Format()
      {
         var sb = new StringBuilder();
         sb.AppendLine($"SINGLE-CALL BENCH: {ScenariosAgreeing}/{TotalScenarios} scenarios AGREE with the interpreter on the authored prose (majority of passes).");
         sb.AppendLine($"  {ScenariosWithFailures} scenario(s) had a failed call (single-call or interpreter); shown below.");
         sb.AppendLine();

         List<IGrouping<string, SingleCallBenchResult>> groups = _results.GroupBy(r => r.ScenarioId).ToList();

         // Divergences and failures first (the worklist), then the agreeing scenarios.
         foreach (IGrouping<string, SingleCallBenchResult> g in groups.Where(g => !MajorityAgrees(g)))
            sb.Append(FormatScenario(g, false));

         foreach (IGrouping<string, SingleCallBenchResult> g in groups.Where(MajorityAgrees))
            sb.Append(FormatScenario(g, true));

         return sb.ToString();
      }

      private static bool MajorityAgrees(IGrouping<string, SingleCallBenchResult> g)
      {
         List<SingleCallBenchResult> scored = g.Where(r => r.Scored).ToList();
         if (scored.Count == 0) return false;

         // r.Scored guarantees r.Agreement is non-null (see the Scored property); the ! reflects that invariant.
         int agree = scored.Count(r => r.Agreement!.Verdict == SingleCallAgreementVerdict.Agree);

         return agree * 2 > scored.Count;
      }

      private static string FormatScenario(IGrouping<string, SingleCallBenchResult> g, bool agreeing)
      {
         var sb = new StringBuilder();
         List<SingleCallBenchResult> passes = g.ToList();
         int scored = passes.Count(r => r.Scored);
         // r.Scored guarantees r.Agreement is non-null (see the Scored property); the ! reflects that invariant.
         int agree = passes.Count(r => r.Scored && r.Agreement!.Verdict == SingleCallAgreementVerdict.Agree);

         sb.AppendLine($"  {(agreeing ? "AGREE " : "DIVERGE")}  {g.Key}  ({agree}/{passes.Count} agreed)");

         // Show one representative pass: a diverging one when present (the informative case), else the first.
         SingleCallBenchResult sample = passes.FirstOrDefault(r => r.Scored && r.Agreement!.Verdict == SingleCallAgreementVerdict.Diverge)
                                       ?? passes.FirstOrDefault(r => r.Scored)
                                       ?? passes.First();

         if (!sample.Scored)
         {
            sb.AppendLine($"      FAILED: {sample.FailureNote}");

            return sb.ToString();
         }

         // Show the authored prose on divergences (the worklist): the tags alone cannot tell whether the single call
         // dropped a real deed or the interpreter over-tagged - only the text the tags were drawn from can.
         if (!agreeing)
            sb.AppendLine($"      prose: {Snippet(sample.Prose)}");

         sb.AppendLine($"      self:        [{string.Join(", ", sample.SelfTypes)}]");
         sb.AppendLine($"      interpreter: [{string.Join(", ", sample.InterpreterTypes)}]");

         // sample.Scored (checked above, which returns on !Scored) guarantees sample.Agreement is non-null here.
         if (sample.Agreement!.OnlySelf.Count > 0)
            sb.AppendLine($"      only self (self-grounding drift?): [{string.Join(", ", sample.Agreement.OnlySelf)}]");
         if (sample.Agreement.OnlyInterpreter.Count > 0)
            sb.AppendLine($"      only interpreter (single call missed?): [{string.Join(", ", sample.Agreement.OnlyInterpreter)}]");

         return sb.ToString();
      }

      // A single-lined, bounded slice of the authored prose so a divergence line stays readable while still carrying
      // enough text to judge who was right.
      private static string Snippet(string? prose)
      {
         if (string.IsNullOrWhiteSpace(prose)) return "(none)";

         // string.IsNullOrWhiteSpace returning false above guarantees prose is non-null here.
         string flat = string.Join(" ", prose!.Split(new[] {'\r', '\n'}, System.StringSplitOptions.RemoveEmptyEntries)
                                             .Select(l => l.Trim()));

         return flat.Length <= 500 ? flat : flat.Substring(0, 500) + "...";
      }
   }
}

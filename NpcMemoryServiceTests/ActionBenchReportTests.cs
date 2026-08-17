// Code written by Gabriel Mailhot, 17/08/2026.
// The bench report is what a run actually shows the developer, so its arithmetic and its scoreboard must be exact:
// a wrong tally or a swallowed failure would send tuning at the wrong verb, or hide a broken run behind a clean
// number. With more than one pass a case is judged by MAJORITY, so these pure tests (synthetic results, no LLM) pin
// the run tallies, the pass rule, the majority rule, and that the scoreboard surfaces every non-passing case.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ActionBenchReportTests
   {
      private static ActionBenchResult Run(string id, string verb, ActionBenchVerdict verdict, bool callOk = true)
      {
         bool negative = verdict == ActionBenchVerdict.CorrectWithhold || verdict == ActionBenchVerdict.FalsePositive;
         ActionBenchCase test = negative
            ? ActionBenchCase.ExpectNone(id, verb, "ctx", "prose", verb)
            : ActionBenchCase.Expect(id, verb, "ctx", "prose", verb);

         return new ActionBenchResult(test, callOk, callOk ? null : "timeout", new List<GameAction>(), verdict);
      }

      // Each verdict must land in its own tally and nowhere else: a hit is not a partial, a call failure is not a
      // miss. CasesPassed counts distinct cases that passed, TotalCases counts distinct cases.
      [Test]
      public void GIVEN_a_mix_of_results_WHEN_aggregated_THEN_each_tally_is_exact()
      {
         var report = new ActionBenchReport(new[] {
            Run("give_gold", "give_gold", ActionBenchVerdict.Hit),
            Run("marry", "marry", ActionBenchVerdict.PartialHit),
            Run("give_troops", "give_troops", ActionBenchVerdict.Miss),
            Run("grant_stipend", "grant_stipend", ActionBenchVerdict.ParamMismatch),
            Run("take_gold", "take_gold", ActionBenchVerdict.CorrectWithhold),
            Run("sell", "sell_prisoner", ActionBenchVerdict.FalsePositive),
            Run("sway", "sway_opinion", ActionBenchVerdict.Hit, callOk: false)
         });

         report.TotalRuns.Should().Be(7);
         report.TotalCases.Should().Be(7);
         report.Hits.Should().Be(1);
         report.PartialHits.Should().Be(1);
         report.Misses.Should().Be(1);
         report.ParamMismatches.Should().Be(1);
         report.CorrectWithholds.Should().Be(1);
         report.FalsePositives.Should().Be(1);
         report.CallFailures.Should().Be(1);
         report.CasesPassed.Should().Be(2); // the hit and the correct withhold
      }

      // A partial hit (the "1 of 2" extraction) is NOT a pass: only every expected action landing counts, or a
      // dropped second deed would flatter the interpreter.
      [Test]
      public void GIVEN_a_partial_hit_WHEN_aggregated_THEN_it_is_not_counted_as_a_pass()
      {
         var report = new ActionBenchReport(new[] {Run("marry_and_warm", "marry", ActionBenchVerdict.PartialHit)});

         report.CasesPassed.Should().Be(0);
         report.PartialHits.Should().Be(1);
      }

      // The majority rule: a case run several times passes if MOST of its runs pass, so one unlucky run of a
      // nondeterministic model does not read as a stable failure.
      [Test]
      public void GIVEN_a_case_run_three_times_with_two_passes_WHEN_aggregated_THEN_the_case_passes_by_majority()
      {
         var report = new ActionBenchReport(new[] {
            Run("give_influence", "give_influence", ActionBenchVerdict.Hit),
            Run("give_influence", "give_influence", ActionBenchVerdict.Hit),
            Run("give_influence", "give_influence", ActionBenchVerdict.Miss)
         });

         report.TotalRuns.Should().Be(3);
         report.TotalCases.Should().Be(1);
         report.CasesPassed.Should().Be(1); // 2 of 3 passed
         report.CasesSolid.Should().Be(0);  // but not unanimous, so not solid
         report.CasesFlaky.Should().Be(1);  // it is flaky: still refineable
      }

      // Gabriel's point (2026-08-17): a case that passes 3/3 is SOLID; one that passes 2/3 still has noise on it and
      // can be refined further. The report must tell the two apart, and surface the flaky (but passing) cases so
      // they are not lost among the clean ones.
      [Test]
      public void GIVEN_a_solid_case_and_a_flaky_case_WHEN_formatted_THEN_only_the_flaky_one_is_flagged_refineable()
      {
         var report = new ActionBenchReport(new[] {
            Run("give_gold", "give_gold", ActionBenchVerdict.Hit),          // solid: 2/2
            Run("give_gold", "give_gold", ActionBenchVerdict.Hit),
            Run("give_influence", "give_influence", ActionBenchVerdict.CorrectWithhold), // flaky: 2/3
            Run("give_influence", "give_influence", ActionBenchVerdict.CorrectWithhold),
            Run("give_influence", "give_influence", ActionBenchVerdict.FalsePositive)
         });

         report.CasesSolid.Should().Be(1);
         report.CasesFlaky.Should().Be(1);
         report.CasesPassed.Should().Be(2);

         string board = report.Format();
         board.Should().Contain("FLAKY");
         board.Should().Contain("give_influence");
         board.Should().NotContain("give_gold"); // the solid case is not flagged for refinement
      }

      // The other side of the majority rule: mostly-failing runs fail the case, and the scoreboard shows the rate.
      [Test]
      public void GIVEN_a_case_that_fails_most_of_its_runs_WHEN_formatted_THEN_it_is_listed_with_its_pass_rate()
      {
         var report = new ActionBenchReport(new[] {
            Run("give_influence", "give_influence", ActionBenchVerdict.FalsePositive),
            Run("give_influence", "give_influence", ActionBenchVerdict.FalsePositive),
            Run("give_influence", "give_influence", ActionBenchVerdict.CorrectWithhold)
         });

         report.CasesPassed.Should().Be(0);

         string board = report.Format();
         board.Should().Contain("give_influence");
         board.Should().Contain("1/3"); // one of three runs passed
      }

      // The scoreboard is the actionable output: it must name every failing case and not a passing one.
      [Test]
      public void GIVEN_a_report_WHEN_formatted_THEN_it_lists_the_failing_cases_and_not_the_passing_one()
      {
         var report = new ActionBenchReport(new[] {
            Run("give_gold", "give_gold", ActionBenchVerdict.Hit),
            Run("give_troops", "give_troops", ActionBenchVerdict.Miss),
            Run("take_gold", "take_gold", ActionBenchVerdict.FalsePositive)
         });

         string board = report.Format();

         board.Should().Contain("1/3 cases passed");
         board.Should().Contain("give_troops");
         board.Should().Contain("take_gold");
         board.Should().Contain("FAILURES");
      }

      // The clean run says so plainly rather than printing an empty FAILURES list, which reads as a formatting bug.
      [Test]
      public void GIVEN_an_all_pass_report_WHEN_formatted_THEN_it_says_all_cases_passed()
      {
         var report = new ActionBenchReport(new[] {
            Run("give_gold", "give_gold", ActionBenchVerdict.Hit),
            Run("take_gold", "take_gold", ActionBenchVerdict.CorrectWithhold)
         });

         string board = report.Format();

         board.Should().Contain("2/2 cases passed");
         board.Should().Contain("All cases passed");
         board.Should().NotContain("FAILURES");
      }
   }
}

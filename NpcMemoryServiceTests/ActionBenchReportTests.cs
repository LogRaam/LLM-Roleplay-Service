// Code written by Gabriel Mailhot, 17/08/2026.
// The bench report is what a run actually shows the developer, so its arithmetic and its scoreboard must be exact:
// a wrong tally or a swallowed failure would send tuning at the wrong verb, or hide a broken run behind a clean
// number. These pure tests feed synthetic results (no LLM) and pin the counts, the pass rule, and that the
// scoreboard surfaces every non-passing case (including a call failure).

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
      private static ActionBenchResult Positive(string verb, ActionBenchVerdict verdict)
         => new ActionBenchResult(ActionBenchCase.Expect(verb, verb, "ctx", "prose", verb),
            true, null, new List<GameAction>(), verdict);

      private static ActionBenchResult Negative(string verb, ActionBenchVerdict verdict)
         => new ActionBenchResult(ActionBenchCase.ExpectNone(verb + "_neg", verb, "ctx", "prose", verb),
            true, null, new List<GameAction>(), verdict);

      private static ActionBenchResult Failed(string verb)
         => new ActionBenchResult(ActionBenchCase.Expect(verb, verb, "ctx", "prose", verb),
            false, "timeout", null, ActionBenchVerdict.Miss);

      // Each verdict must land in its own tally and nowhere else: a hit is not a withhold, a call failure is not a
      // miss. Passed is exactly hits plus correct withholds, the two "the interpreter got it right" outcomes.
      [Test]
      public void GIVEN_a_mix_of_results_WHEN_aggregated_THEN_each_tally_is_exact()
      {
         var report = new ActionBenchReport(new[] {
            Positive("give_gold", ActionBenchVerdict.Hit),
            Positive("give_troops", ActionBenchVerdict.Miss),
            Positive("grant_stipend", ActionBenchVerdict.ParamMismatch),
            Negative("take_gold", ActionBenchVerdict.CorrectWithhold),
            Negative("marry", ActionBenchVerdict.FalsePositive),
            Failed("sway_opinion")
         });

         report.Total.Should().Be(6);
         report.Hits.Should().Be(1);
         report.Misses.Should().Be(1);
         report.ParamMismatches.Should().Be(1);
         report.CorrectWithholds.Should().Be(1);
         report.FalsePositives.Should().Be(1);
         report.CallFailures.Should().Be(1);
         report.Passed.Should().Be(2); // the hit and the correct withhold
      }

      // A call failure is not a pass: it spent tokens and produced nothing, so it must count against the run and be
      // visible, never quietly folded into "withheld" just because no forbidden action came back.
      [Test]
      public void GIVEN_a_call_failure_WHEN_aggregated_THEN_it_is_not_counted_as_a_pass()
      {
         var report = new ActionBenchReport(new[] {Failed("give_gold")});

         report.Passed.Should().Be(0);
         report.CallFailures.Should().Be(1);
      }

      // The scoreboard is the actionable output: it must name every failing case (so tuning knows where to look)
      // and must NOT list a case that passed. The totals line reports the pass count.
      [Test]
      public void GIVEN_a_report_WHEN_formatted_THEN_it_lists_the_failing_cases_and_not_the_passing_one()
      {
         var report = new ActionBenchReport(new[] {
            Positive("give_gold", ActionBenchVerdict.Hit),
            Positive("give_troops", ActionBenchVerdict.Miss),
            Negative("take_gold", ActionBenchVerdict.FalsePositive)
         });

         string board = report.Format();

         board.Should().Contain("1/3 passed");
         board.Should().Contain("give_troops");   // the miss is surfaced
         board.Should().Contain("take_gold");     // the false positive is surfaced
         board.Should().Contain("FAILURES:");
      }

      // The clean run: nothing failed, so the scoreboard says so plainly rather than printing an empty FAILURES
      // list, which reads as a formatting bug.
      [Test]
      public void GIVEN_an_all_pass_report_WHEN_formatted_THEN_it_says_all_cases_passed()
      {
         var report = new ActionBenchReport(new[] {
            Positive("give_gold", ActionBenchVerdict.Hit),
            Negative("take_gold", ActionBenchVerdict.CorrectWithhold)
         });

         string board = report.Format();

         board.Should().Contain("2/2 passed");
         board.Should().Contain("All cases passed");
         board.Should().NotContain("FAILURES:");
      }
   }
}

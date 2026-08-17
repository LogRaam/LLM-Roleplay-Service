// Code written by Gabriel Mailhot, 17/08/2026.
// Firewalled smoke run of the interpreter extraction bench: it drives ActionBenchRunner on a SMALL, fixed subset
// of cases through the real interpreter, so an accidental selection costs only a handful of calls, and prints the
// scoreboard for a developer to read. It is a bench, not a gate: it asserts the pipeline RAN (a report for every
// case, no call failures), never a particular hit rate, which varies by model. The full sweep over all cases is
// the in-game cr.action_bench; run this to sanity-check the wiring against a live model.

#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NUnit.Framework;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   [TestFixture]
   [Explicit("Calls a real LLM and spends tokens. Run deliberately: CR_RUN_LIVE_LLM=1 plus --filter TestCategory=LiveLlm.")]
   public sealed class ActionBenchLiveLlmTests : LiveLlmHarness
   {
      // A small, deliberately fixed subset spanning a numeric-param positive, a name-param positive, a multi-param
      // positive, and both negative families, so a smoke run exercises the whole scoring surface for only a few calls.
      private static readonly HashSet<string> SubsetIds = new HashSet<string> {
         "give_gold", "release_prisoner", "swear_oath",
         "give_gold_narrated_only", "give_gold_conditional_promise"
      };

      [Test]
      public async Task GIVEN_a_subset_of_bench_cases_WHEN_run_through_the_real_interpreter_THEN_the_pipeline_produces_a_scoreboard()
      {
         List<ActionBenchCase> subset = ActionBenchCatalog.All.Where(c => SubsetIds.Contains(c.Id)).ToList();
         subset.Should().NotBeEmpty("the founding case ids must still exist in the catalog");

         ActionBenchReport report = await ActionBenchRunner.RunAsync(Client, subset);

         TestContext.Out.WriteLine(report.Format());

         report.TotalCases.Should().Be(subset.Count);
         report.CallFailures.Should().Be(0, "a live run past the firewall should reach the model for every case");
      }
   }
}

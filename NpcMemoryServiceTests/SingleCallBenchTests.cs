// Code written by Gabriel Mailhot, 19/08/2026.
// The single-call bench decides whether one model call can replace the two-call pipeline. Its verdict is only
// trustworthy if the agreement scorer, the corpus shape, and the majority-report tallies are themselves correct: a
// scorer that counted a failed interpreter call as agreement, or a report that let a noisy single divergence sink a
// scenario, would give a false green (or false red) light on retiring the second call. These pure tests pin that logic.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SingleCallBenchTests
   {
      // The core promise of the metric: identical substantive deeds = agreement, regardless of order or of the noisy
      // chat-flow fallbacks. If this were wrong, every run would read as divergence and single-call would look worse
      // than it is.
      [Test]
      public void GIVEN_the_same_substantive_types_in_any_order_WHEN_compared_THEN_they_agree()
      {
         SingleCallAgreementResult r = SingleCallAgreement.Compare(
            new[] {"take_gold", "change_relation"},
            new[] {"change_relation", "take_gold"});

         r.Verdict.Should().Be(SingleCallAgreementVerdict.Agree);
         r.OnlySelf.Should().BeEmpty();
         r.OnlyInterpreter.Should().BeEmpty();
      }

      // change_relation and end_conversation are chat-flow noise: either side adding one is not a disagreement about
      // what DEED happened. If they counted, a scenario where the single call added a +1 warmth would falsely diverge.
      [Test]
      public void GIVEN_only_the_noisy_fallbacks_differ_WHEN_compared_THEN_they_still_agree()
      {
         SingleCallAgreementResult r = SingleCallAgreement.Compare(
            new[] {"execute_prisoner", "change_relation", "end_conversation"},
            new[] {"execute_prisoner"});

         r.Verdict.Should().Be(SingleCallAgreementVerdict.Agree);
      }

      // The whole point of the cross-check: a deed the single call self-tagged but the interpreter did NOT support from
      // the same prose is the self-grounding drift we are hunting. It must surface as only-self, and diverge.
      [Test]
      public void GIVEN_the_single_call_tagged_a_deed_the_interpreter_did_not_WHEN_compared_THEN_it_diverges_as_only_self()
      {
         SingleCallAgreementResult r = SingleCallAgreement.Compare(
            new[] {"turn_nemesis"},
            new string[0]);

         r.Verdict.Should().Be(SingleCallAgreementVerdict.Diverge);
         r.OnlySelf.Should().ContainSingle().Which.Should().Be("turn_nemesis");
         r.OnlyInterpreter.Should().BeEmpty();
      }

      // A failed interpreter call arrives as a null type set. It must be treated as "extracted nothing" (a divergence
      // when the single call did tag something), NEVER silently as agreement, or a broken run would inflate the score.
      [Test]
      public void GIVEN_a_null_interpreter_set_WHEN_compared_against_a_real_deed_THEN_it_diverges()
      {
         SingleCallAgreementResult r = SingleCallAgreement.Compare(new[] {"give_gold"}, null);

         r.Verdict.Should().Be(SingleCallAgreementVerdict.Diverge);
         r.OnlyInterpreter.Should().BeEmpty();
         r.OnlySelf.Should().ContainSingle().Which.Should().Be("give_gold");
      }

      // Every scenario must be runnable: a real id, real facts, a real player line. A blank one would waste a call and
      // read as coverage it does not provide.
      [Test]
      public void GIVEN_the_corpus_WHEN_inspected_THEN_every_scenario_is_well_formed_with_a_unique_id()
      {
         SingleCallBenchCatalog.All.Should().NotBeEmpty();
         SingleCallBenchCatalog.All.Select(s => s.Id).Should().OnlyHaveUniqueItems();

         foreach (SingleCallBenchScenario s in SingleCallBenchCatalog.All)
         {
            s.Id.Should().NotBeNullOrWhiteSpace();
            s.Facts.Should().NotBeNullOrWhiteSpace();
            s.PlayerMessage.Should().NotBeNullOrWhiteSpace();
            s.IntendedDeed.Should().NotBeNullOrWhiteSpace();
         }
      }

      // A scenario is judged by MAJORITY so one noisy pass cannot sink it. Two agrees against one divergence must read
      // as an agreeing scenario; the report headline depends on this.
      [Test]
      public void GIVEN_two_agree_and_one_diverge_for_a_scenario_WHEN_reported_THEN_the_scenario_counts_as_agreeing()
      {
         var results = new List<SingleCallBenchResult> {
            Scored("s1", SingleCallAgreementVerdict.Agree),
            Scored("s1", SingleCallAgreementVerdict.Agree),
            Scored("s1", SingleCallAgreementVerdict.Diverge)
         };

         var report = new SingleCallBenchReport(results);

         report.TotalScenarios.Should().Be(1);
         report.ScenariosAgreeing.Should().Be(1);
      }

      // A scenario every pass of which failed must NOT count as agreeing (it was never scored), and must be flagged as
      // a failure, so a run that could not reach the model is never mistaken for a passing one.
      [Test]
      public void GIVEN_a_scenario_that_only_ever_failed_WHEN_reported_THEN_it_is_not_counted_as_agreeing()
      {
         var results = new List<SingleCallBenchResult> {
            new SingleCallBenchResult("s2", "single-call failed: HTTP 402")
         };

         var report = new SingleCallBenchReport(results);

         report.TotalScenarios.Should().Be(1);
         report.ScenariosAgreeing.Should().Be(0);
         report.ScenariosWithFailures.Should().Be(1);
      }

      private static SingleCallBenchResult Scored(string id, SingleCallAgreementVerdict verdict)
      {
         var agreement = new SingleCallAgreementResult(verdict, new List<string>(), new List<string>());

         return new SingleCallBenchResult(id, "some prose", new List<string> {"x"}, new List<string> {"x"}, agreement);
      }
   }
}

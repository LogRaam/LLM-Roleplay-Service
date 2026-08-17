// Code written by Gabriel Mailhot, 17/08/2026.
// The extraction bench is only as trustworthy as its judge: if the scorer mis-scores, every downstream fidelity
// number is a lie. These pure tests feed ActionBenchScorer synthetic emitted-action lists (no LLM) and pin exactly
// what counts as a hit, a miss, a parameter mismatch, a correct withhold, and a false positive, so the bench's
// verdicts mean what the report claims they mean.

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
   public class ActionBenchScorerTests
   {
      private static GameAction Act(string type, params (string key, string value)[] parameters)
      {
         var dict = new Dictionary<string, string>();
         foreach ((string key, string value) in parameters) dict[key] = value;

         return new GameAction {Type = type, Parameters = dict};
      }

      // The plain success: the expected verb is emitted and every required parameter matches. Anything less than
      // this must NOT read as a hit, or the bench would flatter the interpreter.
      [Test]
      public void GIVEN_the_expected_action_with_matching_params_WHEN_scored_THEN_it_is_a_hit()
      {
         ActionBenchCase test = ActionBenchCase.Expect("give_gold", "give_gold", "ctx", "prose", "give_gold",
            new Dictionary<string, string> {{"amount", "500"}});

         ActionBenchScorer.Score(new[] {Act("give_gold", ("amount", "500"))}, test)
            .Should().Be(ActionBenchVerdict.Hit);
      }

      // The deed the prose plainly showed was never tagged: the exact failure the whole bench exists to catch.
      [Test]
      public void GIVEN_the_expected_action_absent_WHEN_scored_THEN_it_is_a_miss()
      {
         ActionBenchCase test = ActionBenchCase.Expect("give_gold", "give_gold", "ctx", "prose", "give_gold",
            new Dictionary<string, string> {{"amount", "500"}});

         ActionBenchScorer.Score(new GameAction[0], test).Should().Be(ActionBenchVerdict.Miss);
      }

      // The verb was recognised but the amount is wrong: a softer failure than a plain miss (the model saw the deed,
      // it botched the value), and the report tells them apart so tuning can target the right half.
      [Test]
      public void GIVEN_the_expected_action_with_a_wrong_numeric_param_WHEN_scored_THEN_it_is_a_param_mismatch()
      {
         ActionBenchCase test = ActionBenchCase.Expect("give_gold", "give_gold", "ctx", "prose", "give_gold",
            new Dictionary<string, string> {{"amount", "500"}});

         ActionBenchScorer.Score(new[] {Act("give_gold", ("amount", "100"))}, test)
            .Should().Be(ActionBenchVerdict.ParamMismatch);
      }

      // A numeric parameter is unforgiving: 499 is not 500, so a near miss on an amount or a delta is still a
      // mismatch, never a hit.
      [Test]
      public void GIVEN_a_numeric_param_off_by_one_WHEN_scored_THEN_it_is_not_a_hit()
      {
         ActionBenchCase test = ActionBenchCase.Expect("change_relation", "change_relation", "ctx", "prose",
            "change_relation", new Dictionary<string, string> {{"delta", "-15"}});

         ActionBenchScorer.Score(new[] {Act("change_relation", ("delta", "-14"))}, test)
            .Should().Be(ActionBenchVerdict.ParamMismatch);
      }

      // A NAME parameter is matched tolerantly: a model that writes "Lord Derthert" for an expected "Derthert" has
      // extracted the right captive, and scoring that as a mismatch would punish a correct answer.
      [Test]
      public void GIVEN_a_name_param_with_an_honorific_prefix_WHEN_scored_THEN_it_still_hits()
      {
         ActionBenchCase test = ActionBenchCase.Expect("release_prisoner", "release_prisoner", "ctx", "prose",
            "release_prisoner", new Dictionary<string, string> {{"target", "Derthert"}});

         ActionBenchScorer.Score(new[] {Act("release_prisoner", ("target", "Lord Derthert"))}, test)
            .Should().Be(ActionBenchVerdict.Hit);
      }

      // The verb takes no parameters (or none are required): its bare presence is the whole expectation.
      [Test]
      public void GIVEN_a_paramless_expected_action_present_WHEN_scored_THEN_it_is_a_hit()
      {
         ActionBenchCase test = ActionBenchCase.Expect("pay_blackmail", "pay_blackmail", "ctx", "prose", "pay_blackmail");

         ActionBenchScorer.Score(new[] {Act("pay_blackmail")}, test).Should().Be(ActionBenchVerdict.Hit);
      }

      // A negative case with the forbidden verb absent is the point of the whole negative half: the interpreter
      // correctly withheld on a look-alike (narrated-not-done, a conditional promise, a neighbouring verb).
      [Test]
      public void GIVEN_a_negative_case_with_the_forbidden_verb_absent_WHEN_scored_THEN_it_is_a_correct_withhold()
      {
         ActionBenchCase test = ActionBenchCase.ExpectNone("give_gold_narrated", "give_gold", "ctx", "prose", "give_gold");

         ActionBenchScorer.Score(new[] {Act("change_relation", ("delta", "1"))}, test)
            .Should().Be(ActionBenchVerdict.CorrectWithhold);
      }

      // The failure a negative case guards against: the interpreter fired the look-alike anyway (a promise read as a
      // gift, a mention read as a deed). This is the hallucination the anti-patterns exist to suppress.
      [Test]
      public void GIVEN_a_negative_case_with_the_forbidden_verb_emitted_WHEN_scored_THEN_it_is_a_false_positive()
      {
         ActionBenchCase test = ActionBenchCase.ExpectNone("give_gold_conditional", "give_gold", "ctx", "prose", "give_gold");

         ActionBenchScorer.Score(new[] {Act("give_gold", ("amount", "1000"))}, test)
            .Should().Be(ActionBenchVerdict.FalsePositive);
      }

      // Type matching ignores case and surrounding whitespace, the same tolerance the live parser affords, so a
      // stylistic difference in how the model wrote the type never counts as a miss.
      [Test]
      public void GIVEN_the_expected_type_in_a_different_case_WHEN_scored_THEN_it_still_hits()
      {
         ActionBenchCase test = ActionBenchCase.Expect("free_prisoner", "free_prisoner", "ctx", "prose", "free_prisoner");

         ActionBenchScorer.Score(new[] {Act("Free_Prisoner")}, test).Should().Be(ActionBenchVerdict.Hit);
      }

      // Multi-action, the whole point of Gabriel's concern: a reply that does TWO deeds must produce BOTH tags. When
      // both are emitted with matching params, it hits.
      [Test]
      public void GIVEN_a_two_action_case_with_both_emitted_WHEN_scored_THEN_it_is_a_hit()
      {
         ActionBenchCase test = ActionBenchCase
            .Expect("gold_and_oath", "give_gold", "ctx", "prose", "give_gold", new Dictionary<string, string> {{"amount", "300"}})
            .And("swear_oath", new Dictionary<string, string> {{"oath_kind", "keep_peace"}});

         ActionBenchScorer.Score(new[] {Act("give_gold", ("amount", "300")), Act("swear_oath", ("oath_kind", "keep_peace"))}, test)
            .Should().Be(ActionBenchVerdict.Hit);
      }

      // The exact "1 of 2" failure the multi-action case exists to catch: one expected deed landed, the other was
      // dropped. This must be a PartialHit, distinct from both a clean hit and a total miss.
      [Test]
      public void GIVEN_a_two_action_case_with_only_one_emitted_WHEN_scored_THEN_it_is_a_partial_hit()
      {
         ActionBenchCase test = ActionBenchCase
            .Expect("gold_and_oath", "give_gold", "ctx", "prose", "give_gold", new Dictionary<string, string> {{"amount", "300"}})
            .And("swear_oath", new Dictionary<string, string> {{"oath_kind", "keep_peace"}});

         ActionBenchScorer.Score(new[] {Act("give_gold", ("amount", "300"))}, test)
            .Should().Be(ActionBenchVerdict.PartialHit);
      }

      // Neither expected action emitted is a full miss, even in a multi-action case, so a partial is never confused
      // with a total blank.
      [Test]
      public void GIVEN_a_two_action_case_with_neither_emitted_WHEN_scored_THEN_it_is_a_miss()
      {
         ActionBenchCase test = ActionBenchCase
            .Expect("gold_and_oath", "give_gold", "ctx", "prose", "give_gold")
            .And("swear_oath");

         ActionBenchScorer.Score(new[] {Act("change_relation", ("delta", "2"))}, test)
            .Should().Be(ActionBenchVerdict.Miss);
      }
   }
}

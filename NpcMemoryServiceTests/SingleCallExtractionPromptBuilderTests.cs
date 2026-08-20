// Code written by Gabriel Mailhot, 19/08/2026.
// The single-call spike asks ONE model to be both the roleplayer and the scribe. This prompt is the whole experiment:
// if the three-step contract (reply -> CHECK -> tags) or the grounding rules were quietly dropped, the probe would
// measure a naive single call (the thing that historically degraded both prose and tags) instead of the CHECK-guided
// one, and we would draw the wrong conclusion about whether the two-call split can be retired. These tests pin that
// the contract, the concept-level rules, and the shared catalog discriminants are all actually present.

#region

using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SingleCallExtractionPromptBuilderTests
   {
      // The whole point of the spike is the ORDER: prose first (what the player sees), then the grounding check, then
      // the tags. If the three steps are not spelled out, the model reverts to interleaving tags into the prose, which
      // is the failure mode the two-call design was built to avoid.
      [Test]
      public void GIVEN_the_single_call_prompt_WHEN_built_THEN_it_lays_out_the_three_step_reply_check_tags_contract()
      {
         string prompt = SingleCallExtractionPromptBuilder.Build("PLAYER: Aldric. YOU: Caladog, a lord.");

         prompt.Should().Contain("STEP 1 - THE REPLY");
         prompt.Should().Contain("STEP 2 - THE CHECK");
         prompt.Should().Contain("STEP 3 - THE TAGS");
         prompt.Should().Contain("[DIALOGUE]");
         prompt.Should().Contain("CHECK <type>:");
         prompt.IndexOf("STEP 1", System.StringComparison.Ordinal)
            .Should().BeLessThan(prompt.IndexOf("STEP 3", System.StringComparison.Ordinal), "the reply must be asked for before the tags");
      }

      // The spike's first run proved the model gets the reasoning right but improvises an unparsable tag shape (bare
      // verbs, verbs in their own brackets, free-text events) unless the EXACT format is pinned - the parser then
      // recovered nothing and every scenario read as [none]. This guards that the literal "type: <verb>" template and
      // the delta/amount/target parameter names are actually taught, so the tags parse.
      [Test]
      public void GIVEN_the_single_call_prompt_WHEN_built_THEN_it_pins_the_exact_type_verb_tag_format()
      {
         string prompt = SingleCallExtractionPromptBuilder.Build("PLAYER: Aldric. YOU: Caladog, a lord.");

         prompt.Should().Contain("EXACT TAG FORMAT");
         prompt.Should().Contain("type: take_gold");
         prompt.Should().Contain("type: change_relation");
         prompt.Should().Contain("delta:");
         prompt.Should().Contain("amount:");
         prompt.Should().Contain("NEVER a bare verb");
      }

      // The spike's second run exposed the real single-call risk: when uncertain, a chatty model reasons OUT LOUD, and
      // that rumination leaks into the player-visible [DIALOGUE] and blows the token budget before any tag is emitted
      // (give_gold, 2026-08-19 18:26). The discipline block is the guard: nothing outside the blocks, no deliberating
      // in prose. If this line were dropped, the prompt would silently re-open the leak the two-call split avoids.
      [Test]
      public void GIVEN_the_single_call_prompt_WHEN_built_THEN_it_forbids_reasoning_leaking_into_the_visible_reply()
      {
         string prompt = SingleCallExtractionPromptBuilder.Build("PLAYER: Aldric. YOU: Caladog, a lord.");

         prompt.Should().Contain("DISCIPLINE");
         prompt.Should().Contain("NEVER think out loud");
         prompt.Should().Contain("do NOT deliberate in prose");
      }

      // The CHECK is only worth anything if it still carries the concept-level withholds (completed-deed, direction,
      // the vow carve-out, the terminal-deed rule). These are what turn a naive single call into the guided one.
      [Test]
      public void GIVEN_the_single_call_prompt_WHEN_built_THEN_it_carries_the_grounding_rules()
      {
         string prompt = SingleCallExtractionPromptBuilder.Build("PLAYER: Aldric. YOU: Caladog, a lord.");

         prompt.Should().Contain("ONLY A COMPLETED DEED COUNTS");
         prompt.Should().Contain("DIRECTION IS EVERYTHING");
         prompt.Should().Contain("A VOW IS A PRESENT DEED");
         prompt.Should().Contain("A BEAT THAT ENDS ON A DEED RECORDS THE DEED");
      }

      // The single call must teach the SAME per-verb discriminants the interpreter does, drawn from the shared
      // catalog: this is the "one source of quality" principle. If a dispatchable verb were missing, the single call
      // would be blind to it while the interpreter still handled it, reintroducing the divergence we are removing.
      [Test]
      public void GIVEN_the_single_call_prompt_WHEN_built_THEN_it_renders_every_catalog_verb_with_its_tells()
      {
         string prompt = SingleCallExtractionPromptBuilder.Build("PLAYER: Aldric. YOU: Caladog, a lord.");

         foreach (string verb in GameActionCatalog.Types)
            prompt.Should().Contain(verb, $"the single-call prompt must teach the dispatchable verb '{verb}'");

         prompt.Should().Contain("emit when:");
         prompt.Should().Contain("not when:");
      }

      // A trivial guard: the facts the caller passes (who is who, live state) must actually reach the prompt, or every
      // scenario would read against a blank stage and the probe would be meaningless.
      [Test]
      public void GIVEN_scenario_facts_WHEN_built_THEN_they_appear_in_the_prompt()
      {
         string prompt = SingleCallExtractionPromptBuilder.Build("PLAYER: Rhobart holds the hero captive Sanjar. YOU: Yerengul.");

         prompt.Should().Contain("Rhobart holds the hero captive Sanjar");
      }
   }
}

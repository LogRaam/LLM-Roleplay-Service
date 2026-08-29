// Code written by Gabriel Mailhot, 28/08/2026.
// Increment 4 of a public extension API: PromptVariableRegistry is the engine-agnostic store a third-party
// mod's facade (CalradiaRemembers.CrPrompt) writes to, so a mod can contribute a live {{name}} prompt
// variable without a Harmony patch on PromptBuilder.BuildPromptVariables. These tests pin the registration
// channel's contract in isolation: a registered provider's value must actually surface through Compose, a
// re-registration must replace rather than stack, and a throwing provider must never be able to take the
// whole composition down with it, since Compose runs once per prompt build for EVERY conversation.

#region

using System;
using FluentAssertions;
using NpcMemoryService.Core.Extension;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PromptVariableRegistryTests
   {
      [SetUp]
      public void SetUp()
      {
         PromptVariableRegistry.Clear();
      }

      [TearDown]
      public void TearDown()
      {
         PromptVariableRegistry.Clear();
      }

      // STAKE: the whole point of the door is that a registered provider's value actually reaches the prompt.
      // If Compose never surfaced it, a third-party mod's {{bc_court_state}} would silently never appear.
      [Test]
      public void GIVEN_a_registered_provider_WHEN_Compose_runs_THEN_its_value_appears_keyed_by_name()
      {
         PromptVariableRegistry.Register("bc_court_state", facts => $"court-of-{facts.NpcId}");

         var result = PromptVariableRegistry.Compose(new PromptVarFacts {NpcId = "lord_007"});

         result.Should().ContainKey("bc_court_state");
         result["bc_court_state"].Should().Be("court-of-lord_007");
      }

      // STAKE: a mod that reloads, or a second registration under the same convention name, must never leave
      // two providers answering for one token: the LAST registration is authoritative, never a stack of both.
      [Test]
      public void GIVEN_a_name_already_registered_WHEN_registered_again_THEN_the_new_provider_replaces_the_old_one()
      {
         PromptVariableRegistry.Register("bc_court_state", _ => "first");
         PromptVariableRegistry.Register("bc_court_state", _ => "second");

         var result = PromptVariableRegistry.Compose(new PromptVarFacts {NpcId = "any"});

         result["bc_court_state"].Should().Be("second");
      }

      // STAKE: without a working Unregister, a mod could never retract a variable it decided to stop
      // contributing (e.g. its own feature toggled off), and it would linger in every future prompt.
      [Test]
      public void GIVEN_a_registered_provider_WHEN_Unregister_is_called_THEN_it_no_longer_appears_in_Compose()
      {
         PromptVariableRegistry.Register("bc_court_state", _ => "value");

         bool removed = PromptVariableRegistry.Unregister("bc_court_state");
         var result = PromptVariableRegistry.Compose(new PromptVarFacts {NpcId = "any"});

         removed.Should().BeTrue();
         result.Should().NotContainKey("bc_court_state");
      }

      // STAKE: a null/blank name, or a null provider, must be a safe no-op, never a crash that could take down
      // a third-party mod's own load sequence, mirroring GameActionCatalog.Register's own never-throw stance.
      [Test]
      public void GIVEN_a_null_or_blank_name_or_a_null_provider_WHEN_registered_THEN_it_is_ignored_without_throwing()
      {
         Action act = () =>
         {
            PromptVariableRegistry.Register(null, _ => "x");
            PromptVariableRegistry.Register("", _ => "x");
            PromptVariableRegistry.Register("   ", _ => "x");
            PromptVariableRegistry.Register("valid_name", null);
         };

         act.Should().NotThrow();
         PromptVariableRegistry.Compose(new PromptVarFacts {NpcId = "any"}).Should().BeEmpty();
      }

      // STAKE: a third-party provider must never be able to break prompt building for every OTHER registered
      // variable, or one buggy addon bricks every conversation in the game. Compose must isolate the fault and
      // keep composing the rest, the exact guarded-invoke stance CR's own conversation-event dispatch takes.
      [Test]
      public void GIVEN_a_provider_that_throws_WHEN_Compose_runs_THEN_it_is_skipped_and_the_others_still_compose()
      {
         PromptVariableRegistry.Register("good_var", facts => $"ok-{facts.NpcId}");
         PromptVariableRegistry.Register("bad_var", _ => throw new InvalidOperationException("boom"));

         Action act = () => PromptVariableRegistry.Compose(new PromptVarFacts {NpcId = "someid"});
         act.Should().NotThrow();

         var result = PromptVariableRegistry.Compose(new PromptVarFacts {NpcId = "someid"});
         result.Should().NotContainKey("bad_var");
         result["good_var"].Should().Be("ok-someid");
      }

      // STAKE: PromptBuilder.BuildPromptVariables merges Compose's result into an already-built dictionary. A
      // caller that assumes "nothing registered means empty" must actually get an empty dictionary, not null
      // or a throw, or that merge step would itself need defensive null-handling it should never have to carry.
      [Test]
      public void GIVEN_nothing_registered_WHEN_Compose_runs_THEN_it_returns_an_empty_dictionary()
      {
         var result = PromptVariableRegistry.Compose(new PromptVarFacts {NpcId = "any"});

         result.Should().NotBeNull();
         result.Should().BeEmpty();
      }
   }
}

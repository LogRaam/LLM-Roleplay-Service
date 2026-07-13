// Code written by Gabriel Mailhot, 02/07/2026.
// RP with teeth, increment 1: the generic extension surface every future conversation-action verb rides
// on. The host (game mod) composes its own eligibility-gated [ACTION] teaching text and hands it over as
// one string; PromptBuilder only needs to place it in the right spot, and skip it for a Lean model.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ExtraActionTeachingsTests
   {
      private const string Marker = "type: give_influence — UNIQUE_TEST_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The whole point of the extension surface: the host already re-checked every eligibility guard and
      // composed its own [ACTION] text, so PromptBuilder must place it verbatim, not reinterpret or
      // reformat it. If this silently drops or mangles the block, the verb it teaches becomes a hollow
      // promise the engine can never honour.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_extra_action_teachings_are_supplied_THEN_they_are_rendered_verbatim()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, ExtraActionTeachings = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      // The extended verb set is deliberately withheld from Lean: a small / short-context model gets only
      // the minimal built-in action contract, so a host-supplied verb never eats into that tight budget.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_extra_action_teachings_are_supplied_THEN_they_are_omitted()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, ExtraActionTeachings = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(Marker);
      }

      // No-op is the common case, since most conversations have no eligible extra verb this turn: this
      // guards that an absent or blank host block costs zero tokens rather than rendering an empty section.
      [Test]
      public void GIVEN_no_extra_action_teachings_WHEN_building_a_full_prompt_THEN_nothing_extra_is_added()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, ExtraActionTeachings = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Marker);
         promptBlank.Should().NotContain(Marker);
      }
   }
}

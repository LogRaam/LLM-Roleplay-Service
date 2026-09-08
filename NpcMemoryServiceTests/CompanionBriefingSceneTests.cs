// Code written by Gabriel Mailhot, 07/09/2026.
// Companion-briefing pillar, revised per Gabriel's ruling (2026-09-07): the briefing chat must be a REAL
// conversation (the anchor and every present companion answer through the model), never a scripted
// acknowledgment. Without EncounterContext.IsCompanionBriefingScene telling the model what kind of scene this
// is, a companion has no way to know the target of the meeting to come is not standing right there listening,
// and no way to know it may answer honestly rather than merely confirm an order. This file protects that
// framing at the PromptBuilder layer, mirroring CompanionBriefPromptTests' shape.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CompanionBriefingSceneTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Companion",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The load-bearing property: without this, a companion answering in the briefing chat has no way to know
      // the named target is absent, and would plausibly answer as though that person could hear every word.
      [Test]
      public void GIVEN_the_briefing_scene_flag_WHEN_building_a_prompt_THEN_it_states_the_target_is_absent_and_cannot_hear()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full, IsCompanionBriefingScene = true, CompanionBriefingTargetName = "Derthert"
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("Derthert");
         prompt.Should().Contain("is not present and");
         prompt.Should().Contain("cannot hear anything said here");
      }

      // Gabriel's whole reason for insisting on a real conversation rather than a form: the companion must be
      // told it may genuinely disagree, question the plan, or refuse as unsuited, not just "acknowledge" it.
      [Test]
      public void GIVEN_the_briefing_scene_flag_WHEN_building_a_prompt_THEN_the_model_is_told_it_may_push_back_or_plead_unsuited()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full, IsCompanionBriefingScene = true, CompanionBriefingTargetName = "Derthert"
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("push back on the plan");
         prompt.Should().Contain("unsuited");
         prompt.Should().Contain("never a mere acknowledgment of an order");
      }

      // A blank/missing target name (a defensive edge case, the host should always supply one) must not blank
      // out the whole section or leave a dangling "aside from 's hearing" fragment.
      [Test]
      public void GIVEN_the_briefing_scene_flag_WHEN_no_target_name_is_supplied_THEN_it_falls_back_to_a_generic_reference()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, IsCompanionBriefingScene = true};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("someone");
      }

      // An ordinary conversation (the flag left at its default false) must carry no trace of this framing, even
      // when a target name happens to be set (defense in depth: the flag alone must gate the whole section).
      [Test]
      public void GIVEN_the_briefing_scene_flag_is_false_WHEN_building_a_prompt_THEN_nothing_is_added()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionBriefingTargetName = "Derthert"};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain("WHAT THIS CONVERSATION IS");
      }

      // A small local model is the one MOST likely to answer as though the absent target were present; this
      // framing must survive Lean, same as CompanionBrief itself.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_the_briefing_scene_flag_is_set_THEN_the_framing_still_survives()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Lean, IsCompanionBriefingScene = true, CompanionBriefingTargetName = "Derthert"
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("WHAT THIS CONVERSATION IS");
      }
   }
}

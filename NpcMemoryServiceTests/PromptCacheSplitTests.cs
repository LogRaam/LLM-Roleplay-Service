// Code written by Gabriel Mailhot, 01/07/2026.
// The system prompt is split at PromptBuilder.EncounterSectionHeading so NpcChatService can send the part
// before it (identity, persona, format, captive-scene RULES) as a cacheable prefix, and only the dynamic
// tail (encounter description, world state, per-turn scene/witness cues) fresh each turn. If a per-turn
// directive slips ahead of the marker, it busts the prefix cache on every single turn of a long
// conversation (worst case: a multi-turn Hardcore captive scene). These tests pin that everything volatile
// stays strictly after the marker.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PromptCacheSplitTests
   {
      private static NpcProfile Npc(bool isFemale = false) => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         IsFemale = isFemale
      };

      [Test]
      public void GIVEN_a_context_bearing_prompt_WHEN_built_THEN_it_contains_the_encounter_marker()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {Scene = SceneType.Settlement};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(PromptBuilder.EncounterSectionHeading);
      }

      [Test]
      public void GIVEN_a_hardcore_captive_scene_WHEN_built_THEN_the_stage_directive_lands_after_the_marker()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore};
         var context = new EncounterContext {
            Scene = SceneType.Dungeon,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.PersonalDesire,
            SceneStage = CaptiveSceneStage.Initiate
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         int markerIndex = prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal);
         int stageDirectiveIndex = prompt.IndexOf("THE BEAT TO PERFORM THIS TURN", System.StringComparison.Ordinal);

         markerIndex.Should().BeGreaterThan(0);
         stageDirectiveIndex.Should().BeGreaterThan(markerIndex);
      }

      [Test]
      public void GIVEN_a_witness_exchange_turn_WHEN_built_THEN_the_turn_directive_lands_after_the_marker()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            Scene = SceneType.Settlement,
            IsWitnessExchangeTurn = true,
            Witnesses = new[] {
               new WitnessEntry {Name = "Aldric", RelationToNpc = "a rival lord"}
            }
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         int markerIndex = prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal);
         int turnDirectiveIndex = prompt.IndexOf("THIS TURN — A WITNESS HAS JUST SPOKEN", System.StringComparison.Ordinal);

         markerIndex.Should().BeGreaterThan(0);
         turnDirectiveIndex.Should().BeGreaterThan(markerIndex);
         // The witness LIST and reaction teaching are stable, so they stay BEFORE the marker.
         prompt.IndexOf("WITNESSES PRESENT", System.StringComparison.Ordinal).Should().BeLessThan(markerIndex);
      }

      [Test]
      public void GIVEN_a_privacy_request_this_turn_WHEN_built_THEN_the_directive_lands_after_the_marker()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            Scene = SceneType.Settlement,
            PrivacyRequested = true,
            Witnesses = new[] {
               new WitnessEntry {Name = "Aldric", RelationToNpc = "a rival lord"}
            }
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         int markerIndex = prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal);
         int privacyIndex = prompt.IndexOf("HAS JUST REQUESTED A PRIVATE AUDIENCE", System.StringComparison.Ordinal);

         markerIndex.Should().BeGreaterThan(0);
         privacyIndex.Should().BeGreaterThan(markerIndex);
      }

      [Test]
      public void GIVEN_any_prompt_WHEN_built_THEN_current_world_state_lands_after_the_marker()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {Scene = SceneType.Settlement};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 42}, context);

         int markerIndex = prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal);
         int worldStateIndex = prompt.IndexOf("CURRENT WORLD STATE", System.StringComparison.Ordinal);

         markerIndex.Should().BeGreaterThan(0);
         worldStateIndex.Should().BeGreaterThan(markerIndex);
      }
   }
}

// Code written by Gabriel Mailhot, 01/07/2026.
// PromptBuilder.StageIntensity is the pure per-stage textual-intensity curve (1..5) that backs the
// "INTENSITY THIS BEAT" line injected by AppendSceneStageDirective. These tests pin the curve shape:
// it climbs through the escalation up to Climax, then drops for the Aftermath come-down and Conclude.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptiveSceneStageIntensityTests
   {
      [Test]
      public void GIVEN_the_narrative_order_of_stages_WHEN_reading_the_intensity_curve_THEN_it_climbs_monotonically_to_climax()
      {
         PromptBuilder.StageIntensity(CaptiveSceneStage.Intro)
                      .Should().BeLessThan(PromptBuilder.StageIntensity(CaptiveSceneStage.RisingTension));
         PromptBuilder.StageIntensity(CaptiveSceneStage.RisingTension)
                      .Should().BeLessThan(PromptBuilder.StageIntensity(CaptiveSceneStage.Initiate));
         PromptBuilder.StageIntensity(CaptiveSceneStage.Initiate)
                      .Should().BeLessThan(PromptBuilder.StageIntensity(CaptiveSceneStage.Intensify));
         PromptBuilder.StageIntensity(CaptiveSceneStage.Intensify)
                      .Should().BeLessThan(PromptBuilder.StageIntensity(CaptiveSceneStage.Climax));

         PromptBuilder.StageIntensity(CaptiveSceneStage.Climax).Should().Be(5);
      }

      [Test]
      public void GIVEN_the_tail_beats_WHEN_reading_the_intensity_curve_THEN_it_drops_after_climax()
      {
         int climax = PromptBuilder.StageIntensity(CaptiveSceneStage.Climax);

         PromptBuilder.StageIntensity(CaptiveSceneStage.Aftermath).Should().BeLessThan(climax);
         PromptBuilder.StageIntensity(CaptiveSceneStage.Conclude).Should().BeLessThan(climax);
      }

      [Test]
      public void GIVEN_a_hardcore_captive_scene_WHEN_built_THEN_the_intensity_line_is_injected()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore};
         var context = new EncounterContext {
            Scene = SceneType.Dungeon,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.PersonalDesire,
            SceneStage = CaptiveSceneStage.Climax
         };

         var npc = new NpcProfile {Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

         string prompt = builder.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("INTENSITY THIS BEAT: 5/5");
      }
   }
}

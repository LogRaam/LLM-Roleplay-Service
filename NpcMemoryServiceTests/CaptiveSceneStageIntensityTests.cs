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
      // The scene director advances one named beat at a time (Intro through Climax); the intensity
      // cue handed to the model on each beat must escalate in step, or "INTENSITY THIS BEAT" would
      // contradict the very structure the director is walking, undermining the whole point of
      // steering pacing from outside the model instead of trusting it to self-pace.
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

      // Pins the come-down: Aftermath and Conclude must read as lower intensity than Climax, not stay
      // pinned at the peak, so the prose actually settles after the high point instead of staying as
      // graphic through the scene's tail as it was at its most intense beat.
      [Test]
      public void GIVEN_the_tail_beats_WHEN_reading_the_intensity_curve_THEN_it_drops_after_climax()
      {
         int climax = PromptBuilder.StageIntensity(CaptiveSceneStage.Climax);

         PromptBuilder.StageIntensity(CaptiveSceneStage.Aftermath).Should().BeLessThan(climax);
         PromptBuilder.StageIntensity(CaptiveSceneStage.Conclude).Should().BeLessThan(climax);
      }

      // The curve above is pure and tested in isolation; this closes the loop end to end, confirming
      // AppendSceneStageDirective actually threads StageIntensity's output into the assembled prompt
      // text the model reads, not just that the lookup table itself is correct.
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

// Quick-win hardening of the captive-scene prompts (audit 2026-07-17, P4.1–P4.3):
//  - P4.1: the sexualized stage/intensity beats must never be injected into the NON-sexual bandit
//    scenes (Extortion / Intimidation / Revenge) — until now only Reckoning was excluded, so a
//    Climax beat could land in a ransom shakedown.
//  - P4.2: the stale "question" line claimed the player could not interrupt unless the model asked
//    one, which the code contradicts (it pauses after EVERY beat).
//  - P4.3: "let's move on" was taught as in-fiction yielding; it is now read as the PLAYER stepping
//    out of the fantasy, to be honored by closing the scene in character.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptiveScenePromptHardeningTests
   {
      private const string StageDirective = "THE BEAT TO PERFORM THIS TURN";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildCaptivePrompt(CaptiveSceneIntent intent)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            Scene = SceneType.Dungeon,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = intent,
            SceneStage = CaptiveSceneStage.Climax
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // P4.1: the bandit menace scenes are declared NOT sexual ("This is NOT a sexual scene"), so
      // the sexualized beat machine ("reach your satisfaction", "INTENSITY 5/5") must stay out of
      // all three non-sexual bandit intents, exactly as it already did for Reckoning.
      [Test]
      public void GIVEN_a_non_sexual_bandit_intent_WHEN_built_THEN_no_stage_directive_is_injected()
      {
         foreach (CaptiveSceneIntent intent in new[] {
                     CaptiveSceneIntent.Extortion,
                     CaptiveSceneIntent.Intimidation,
                     CaptiveSceneIntent.Revenge,
                     CaptiveSceneIntent.Reckoning
                  })
         {
            string prompt = BuildCaptivePrompt(intent);

            prompt.Should().NotContain(StageDirective, $"intent {intent} is non-sexual and must not walk the beats");
            prompt.Should().NotContain("INTENSITY THIS BEAT", $"intent {intent} is non-sexual and must not carry an intensity cue");
         }
      }

      // The positive control: a sexual intent still walks the stage machine, at full Climax
      // intensity — the exclusion above is scoped, not a blanket removal.
      [Test]
      public void GIVEN_a_sexual_intent_WHEN_built_THEN_the_stage_directive_is_still_injected()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.PersonalDesire);

         prompt.Should().Contain(StageDirective);
         prompt.Should().Contain("INTENSITY THIS BEAT: 5/5");
      }

      // P4.2: the new wording tells the model the truth about the agency — the player ALWAYS gets to
      // answer after a beat (the game offers it whatever the model writes), and a question is an
      // invitation, never a requirement. The stale "cannot interrupt" claim must be gone.
      [Test]
      public void GIVEN_a_captive_scene_with_a_stage_directive_WHEN_built_THEN_the_question_line_tells_the_truth_about_agency()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.PersonalDesire);

         prompt.Should().Contain("The player ALWAYS gets the chance to answer after your");
         prompt.Should().Contain("withholding one never silences the player");
         prompt.Should().NotContain("cannot interrupt");
      }

      // P4.3: a vague in-character yielding ("fine", "as you wish") still reads as the prisoner
      // giving in, but a request to LEAVE the scene is the player stepping out of the fantasy and
      // must close the scene in character. The old "interpret it as yielding and proceed" teaching —
      // the riskiest line in the file — must be gone.
      [Test]
      public void GIVEN_a_personal_desire_captive_scene_WHEN_built_THEN_leaving_the_scene_is_honored_not_read_as_yielding()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.PersonalDesire);

         prompt.Should().Contain("A vague in-character yielding");
         prompt.Should().Contain("the PLAYER stepping out of the");
         prompt.Should().Contain("bringing the scene to its close in character");
         prompt.Should().NotContain("Interpret it as yielding and proceed");
      }
   }
}

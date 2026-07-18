// Code written by Gabriel Mailhot, 18/07/2026.
// Adult-prompt audit C3/M11. "Captive scene" and "sexual scene" are not the same thing, and the mod used to
// re-decide which was which in three places with three different answers: the prompt builder withheld the
// sexualized beat directives from some intents, the chat withheld the sexual scene setup from others, and
// the model switch made no distinction at all, so a ransom was served by the specialised erotic model. This
// is now the one list, and it is load-bearing for a CONTENT boundary: a wrong answer here sends "reach your
// satisfaction, INTENSITY 5/5" into a bandit extortion whose own rules say "This is NOT a sexual scene".

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptiveSceneIntentRulesTests
   {
      // The four confrontations that merely happen to involve a prisoner. Reckoning is a lord settling
      // political accounts; the other three are the bandit menace scenes, whose prompt states outright that
      // they are not sexual. A false here is what keeps the beat machinery away from them.
      [TestCase(CaptiveSceneIntent.Reckoning)]
      [TestCase(CaptiveSceneIntent.Extortion)]
      [TestCase(CaptiveSceneIntent.Intimidation)]
      [TestCase(CaptiveSceneIntent.Revenge)]
      public void GIVEN_a_non_sexual_confrontation_WHEN_classified_THEN_it_is_not_a_sexual_scene(CaptiveSceneIntent intent)
         => CaptiveSceneIntentRules.IsSexual(intent).Should().BeFalse();

      // The scenes the captive machinery exists for. Interrogation and Reward are included deliberately:
      // both escalate physically under the captive rules even though neither starts there.
      [TestCase(CaptiveSceneIntent.Interrogation)]
      [TestCase(CaptiveSceneIntent.PersonalDesire)]
      [TestCase(CaptiveSceneIntent.Domination)]
      [TestCase(CaptiveSceneIntent.Torture)]
      [TestCase(CaptiveSceneIntent.Training)]
      [TestCase(CaptiveSceneIntent.Reward)]
      public void GIVEN_a_captive_scene_intent_WHEN_classified_THEN_it_is_a_sexual_scene(CaptiveSceneIntent intent)
         => CaptiveSceneIntentRules.IsSexual(intent).Should().BeTrue();
   }
}

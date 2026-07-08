// Code written by Gabriel Mailhot, 07/07/2026.
// A brigand captor (a faceless bandit OR a named nemesis of a bandit clan) must speak coarse, not like a lord:
// the coarse-voice overlay is applied in a captive scene when CaptorIsBrigand is set, and withheld otherwise.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class BrigandVoiceOverlayTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Wick the Cutthroat",
         Faction = "looters",
         Clan = "looters"
      };

      private static string BuildCaptivePrompt(bool captorIsBrigand)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.PersonalDesire,
            CaptorIsBrigand = captorIsBrigand
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      [Test]
      public void GIVEN_a_brigand_captor_WHEN_building_the_captive_scene_prompt_THEN_the_coarse_voice_overlay_is_applied()
      {
         BuildCaptivePrompt(captorIsBrigand: true).Should().Contain("YOU ARE A BRIGAND, NOT A LORD");
      }

      [Test]
      public void GIVEN_a_non_brigand_captor_WHEN_building_the_captive_scene_prompt_THEN_the_coarse_voice_overlay_is_absent()
      {
         BuildCaptivePrompt(captorIsBrigand: false).Should().NotContain("YOU ARE A BRIGAND, NOT A LORD");
      }
   }
}

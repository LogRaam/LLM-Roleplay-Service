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

      // The player-reported bug this overlay fixed: a recurring bandit boss was "writing with the
      // eloquence and cold calculation of a lord" (CHANGELOG). The overlay must override whatever the
      // NPC's own profiled traits would otherwise produce, since a nemesis keeps his full profile.
      [Test]
      public void GIVEN_a_brigand_captor_WHEN_building_the_captive_scene_prompt_THEN_the_coarse_voice_overlay_is_applied()
      {
         BuildCaptivePrompt(captorIsBrigand: true).Should().Contain("YOU ARE A BRIGAND, NOT A LORD");
      }

      // The negative case: a captor who is a lord (or any non-brigand) must keep his own distinct
      // voice. Without this guard the overlay could leak onto every captor and flatten every captive
      // scene into the same generic thug, erasing the characterization the profile system builds.
      [Test]
      public void GIVEN_a_non_brigand_captor_WHEN_building_the_captive_scene_prompt_THEN_the_coarse_voice_overlay_is_absent()
      {
         BuildCaptivePrompt(captorIsBrigand: false).Should().NotContain("YOU ARE A BRIGAND, NOT A LORD");
      }

      // ── The bandit captor's knowledge of the player across encounters ────

      private static string BuildBanditPerceptionPrompt(bool captorKnowsPlayer)
      {
         var builder = new PromptBuilder {
            AdultLevel = AdultContentLevel.Hardcore,
            PlayerIsFemale = false,
            PlayerName = "Arwa"
         };
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.Interrogation,
            CaptorIsBandit = true,
            CaptorIsBrigand = true,
            CaptorKnowsPlayer = captorKnowsPlayer
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // On a first capture a brigand has no register of lords to check against (per CHANGELOG); the
      // prompt must actively forbid the model from inventing a name it has no way to know, or the
      // fiction breaks the moment a "stranger" captor greets the player by name.
      [Test]
      public void GIVEN_a_first_encounter_WHEN_building_the_bandit_perception_THEN_the_captor_does_not_know_the_players_name()
      {
         string prompt = BuildBanditPerceptionPrompt(captorKnowsPlayer: false);

         prompt.Should().Contain("You do NOT know their name");
         prompt.Should().NotContain("you know them of old");
      }

      // The mirror fix: "a bandit boss who has met you before now knows your name" (CHANGELOG). A
      // recurring nemesis who already holds memories of the player must not keep pretending to be a
      // stranger just because he is a bandit, or every return encounter reads as amnesia.
      [Test]
      public void GIVEN_a_captor_who_already_dealt_with_the_player_WHEN_building_the_bandit_perception_THEN_he_greets_them_by_name()
      {
         string prompt = BuildBanditPerceptionPrompt(captorKnowsPlayer: true);

         prompt.Should().Contain("you know them of old");
         prompt.Should().Contain("This is Arwa");
         prompt.Should().NotContain("You do NOT know their name");
      }
   }
}

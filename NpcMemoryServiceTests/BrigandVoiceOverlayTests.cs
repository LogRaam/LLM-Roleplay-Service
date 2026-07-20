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

      private static string BuildBanditPerceptionPrompt(bool captorKnowsPlayer, bool captorKnowsName = false)
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
            CaptorKnowsPlayer = captorKnowsPlayer,
            CaptorKnowsPlayerName = captorKnowsName
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

      // A recurring nemesis knows the FACE and the HISTORY, and must not read as an amnesiac stranger each
      // return. He references the score between them and what he sees.
      [Test]
      public void GIVEN_a_captor_who_already_dealt_with_the_player_WHEN_building_the_bandit_perception_THEN_he_remembers_the_history()
      {
         string prompt = BuildBanditPerceptionPrompt(captorKnowsPlayer: true);

         prompt.Should().Contain("NO stranger");
         prompt.Should().Contain("history your memories");
      }

      // Gabriel's rule (2026-07-19): a brigand never learns a noble's NAME, register-less as he is, so even
      // a recurring nemesis must NOT use it. The old branch said "use their name freely", which is how a
      // looter greeted the player as "Arwa". He knows the face and the debt, not the name.
      [Test]
      public void GIVEN_a_recurring_bandit_captor_WHEN_building_the_perception_THEN_he_still_does_not_use_the_players_name()
      {
         string prompt = BuildBanditPerceptionPrompt(captorKnowsPlayer: true);

         prompt.Should().Contain("still do NOT know their NAME");
         prompt.Should().NotContain("This is Arwa");
         prompt.Should().NotContain("Use their name freely");
      }

      // The log (Gabriel, 2026-07-19) showed a first-time captor invent a shared past ("same as when you
      // tried biting down earlier") off nothing but the prisoner's current "teeth clenched" line. A
      // stranger has no history to reach for; the prompt now forbids inventing one outright.
      [Test]
      public void GIVEN_a_first_encounter_WHEN_building_the_bandit_perception_THEN_no_shared_past_may_be_invented()
      {
         string prompt = BuildBanditPerceptionPrompt(captorKnowsPlayer: false);

         prompt.Should().Contain("You have NEVER met this prisoner before this moment");
         prompt.Should().Contain("Invent no shared past");
      }

      // The mirror: a recurring captor who DOES know the player must not be told he has never met them, or
      // he could not reference the very history that makes him a nemesis.
      [Test]
      public void GIVEN_a_captor_who_already_dealt_with_the_player_WHEN_building_the_bandit_perception_THEN_no_never_met_clause_is_added()
      {
         BuildBanditPerceptionPrompt(captorKnowsPlayer: true)
            .Should().NotContain("You have NEVER met this prisoner");
      }

      // Gabriel 2026-07-19: if the prisoner gives the captor their name, he learns it and may use it, and a
      // recurring nemesis keeps it. This flag overrides the register-less silence of the other branches.
      [Test]
      public void GIVEN_the_captor_has_learned_the_name_WHEN_building_the_perception_THEN_he_may_use_it()
      {
         string prompt = BuildBanditPerceptionPrompt(captorKnowsPlayer: false, captorKnowsName: true);

         prompt.Should().Contain("You know this prisoner's name: Arwa");
         prompt.Should().Contain("They gave it up to you");
         prompt.Should().NotContain("still do NOT know their NAME");
      }
   }
}

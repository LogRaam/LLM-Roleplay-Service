// Captivity execution (2026-08-04, player request via the "AI Executes" mod, COUNCIL_ACTIONS.md Partie 10.6): a
// captive under torture wanted the scene able to end their character for real. This is IRREVERSIBLE, so the
// execute_player action must be taught ONLY when EncounterContext.CaptorMayExecutePlayer is explicitly set (the
// mod's own MCM opt-in + Hardcore/Adult gate, re-validated again by the bridge) AND the scene has reached a
// grave enough intent (Torture/Domination) for a killing to be plausible. The default (flag unset) must NEVER
// mention the action at all, so a player who never enabled it can never see it in the prompt, let alone have a
// model emit it as a stray line.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptorExecutionPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildCaptivePrompt(CaptiveSceneIntent intent, bool captorMayExecutePlayer)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            Scene = SceneType.Dungeon,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = intent,
            CaptorMayExecutePlayer = captorMayExecutePlayer
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // STAKE: this is the whole point of the opt-in. The overwhelming majority of players never touch the MCM
      // toggle, so CaptorMayExecutePlayer defaults to false and the prompt must not mention execute_player at
      // all, in ANY captive intent, not even the gravest ones.
      [Test]
      public void GIVEN_the_flag_is_unset_WHEN_built_THEN_execute_player_is_never_mentioned_in_any_intent()
      {
         foreach (CaptiveSceneIntent intent in new[] {
                     CaptiveSceneIntent.Interrogation,
                     CaptiveSceneIntent.PersonalDesire,
                     CaptiveSceneIntent.Domination,
                     CaptiveSceneIntent.Torture,
                     CaptiveSceneIntent.Training,
                     CaptiveSceneIntent.Reward
                  })
         {
            string prompt = BuildCaptivePrompt(intent, captorMayExecutePlayer: false);

            prompt.Should().NotContain("execute_player", $"intent {intent} must never see the action when the MCM opt-in is off");
         }
      }

      // STAKE: when a player HAS opted in, the action must still only surface at the two intents grave enough
      // for a killing to be plausible (Torture, Domination) -- teaching it during, say, Reward would be an
      // absurd non-sequitur and undermines "never a stray line".
      [Test]
      public void GIVEN_the_flag_is_set_WHEN_the_intent_is_Torture_THEN_execute_player_is_taught()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Torture, captorMayExecutePlayer: true);

         prompt.Should().Contain("type: execute_player");
         prompt.Should().Contain("THE PRISONER'S LIFE IS IN YOUR HANDS");
      }

      [Test]
      public void GIVEN_the_flag_is_set_WHEN_the_intent_is_Domination_THEN_execute_player_is_taught()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Domination, captorMayExecutePlayer: true);

         prompt.Should().Contain("type: execute_player");
      }

      // STAKE: even with the flag SET, the four non-lethal-register intents (Interrogation, PersonalDesire,
      // Training, Reward) must never teach the action -- their own framing (extracting information, personal
      // desire, conditioning, favor) would make a sudden killing an incoherent non-sequitur.
      [Test]
      public void GIVEN_the_flag_is_set_WHEN_the_intent_is_not_grave_enough_THEN_execute_player_is_not_taught()
      {
         foreach (CaptiveSceneIntent intent in new[] {
                     CaptiveSceneIntent.Interrogation,
                     CaptiveSceneIntent.PersonalDesire,
                     CaptiveSceneIntent.Training,
                     CaptiveSceneIntent.Reward
                  })
         {
            string prompt = BuildCaptivePrompt(intent, captorMayExecutePlayer: true);

            prompt.Should().NotContain("execute_player", $"intent {intent} is not grave enough to plausibly end in a killing");
         }
      }

      // STAKE: the teaching frames the act as deliberate and final, never a threat to voice lightly and never
      // interchangeable with harm_prisoner (which wounds, never kills).
      [Test]
      public void GIVEN_the_flag_is_set_WHEN_built_THEN_the_teaching_frames_it_as_deliberate_and_final()
      {
         string prompt = BuildCaptivePrompt(CaptiveSceneIntent.Torture, captorMayExecutePlayer: true);

         prompt.Should().Contain("not a threat to voice lightly");
         prompt.Should().Contain("final and irreversible");
         prompt.Should().Contain("use harm_prisoner for");
         prompt.Should().Contain("those instead");
      }
   }
}

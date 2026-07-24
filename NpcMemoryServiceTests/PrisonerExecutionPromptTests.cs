// Code written by Gabriel Mailhot, 22/07/2026.
// Player report (Nexus): a player sentenced a captured noble to death mid conversation, the dialogue was
// "fantastic" and ended with the implication he had been executed, but the noble was still alive in the
// prisoner list afterward. AppendExecutePrisonerRule already existed and the bridge (ExecuteExecutePrisoner)
// already re-validates real custody before honouring it, but the rule was only reached from the dedicated
// PLAYER-AS-CAPTOR scene (AppendPlayerCaptorSceneRules), which an ordinary captured lord never opens (that
// scene is only ever entered for a tracked Nemesis prisoner). The capability existed; the ordinary "Talk"
// conversation with a ranked-and-file captured lord never taught it, so the model could only narrate a deed
// it had no channel to perform. AppendOrdinaryPrisonerExecutionRule now gates the SAME rule text on the real
// custody fact (PlayerStatusVsNpc.NpcIsCaptive, the same fact AppendPrisonerFreedomBargain already used for
// free_prisoner), independent of any scene flag. These tests pin the boundary the fix must not cross: the
// verb reaches an ordinary prisoner conversation, but the captor-scene's own CNC/adult framing does not leak
// into it, and a free NPC gets neither.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PrisonerExecutionPromptTests
   {
      private const string ExecutionRuleHeading = "YOUR LIFE IS THEIRS TO END:";
      private const string ExecutionActionType = "type: execute_prisoner";
      private const string CaptorSceneFramingHeading = "CAPTOR SCENE";
      private const string CncFramingHeading = "SCENE FRAMING: CONSENSUAL NON-CONSENT (CNC):";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The core of the fix: an ordinary conversation with a hero the player actually holds prisoner (no
      // captor scene active) must still teach execute_prisoner, or the bug reproduces exactly as reported.
      [Test]
      public void GIVEN_an_ordinary_conversation_with_the_players_prisoner_WHEN_built_THEN_execute_prisoner_is_taught()
      {
         var context = new EncounterContext {PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive};
         string prompt = new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(ExecutionRuleHeading);
         prompt.Should().Contain(ExecutionActionType);
      }

      // The boundary explicitly ratified for this fix: only the VERB may reach an ordinary prisoner
      // conversation, never the captor scene's own framing (its narrative setup and, at Hardcore, its CNC
      // consent layer). Leaking that into a plain "Talk" with a captured lord would be a worse regression
      // than the bug this fix closes.
      [Test]
      public void GIVEN_an_ordinary_conversation_with_the_players_prisoner_WHEN_built_THEN_captor_scene_framing_is_absent()
      {
         var context = new EncounterContext {PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive};
         string prompt = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(CaptorSceneFramingHeading);
         prompt.Should().NotContain(CncFramingHeading);
      }

      // Without this gate, the rule would print for every NPC regardless of custody, inviting the model to
      // treat "kill" as always on the table. A free hero (or one held by someone else) must never see it.
      [Test]
      public void GIVEN_an_npc_who_is_not_the_players_prisoner_WHEN_built_THEN_execute_prisoner_is_absent()
      {
         var context = new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Free};
         string prompt = new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(ExecutionRuleHeading);
         prompt.Should().NotContain(ExecutionActionType);
      }

      // Regression guard for the refactor itself: AppendPlayerCaptorSceneRules no longer calls
      // AppendExecutePrisonerRule directly (removed to avoid duplication), relying on the new unconditional
      // call reaching the scene too, because the mod always forces PlayerStatus to NpcIsCaptive while a
      // captor scene is active. If that assumption ever broke, the scene would silently lose the rule
      // entirely, which is exactly the class of bug this fix exists to close.
      [Test]
      public void GIVEN_the_dedicated_captor_scene_WHEN_built_THEN_execute_prisoner_still_appears_exactly_once()
      {
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive,
            IsCaptorScene = true,
            CaptiveIntent = CaptiveSceneIntent.Interrogation
         };
         string prompt = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         int occurrences = CountOccurrences(prompt, ExecutionRuleHeading);
         occurrences.Should().Be(1);
      }

      #region private

      private static int CountOccurrences(string haystack, string needle)
      {
         int count = 0;
         int index = 0;
         while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) != -1)
         {
            count++;
            index += needle.Length;
         }

         return count;
      }

      #endregion
   }
}

// Code written by Gabriel Mailhot, 17/11/2026.
// 1Enfant (Nexus, 16/11/2026): "How do I disable the description of the surrounding environment? (purple section in
// the conversation)". There was no switch: the opening turn always asked for a [NARRATION] tableau and every ordinary
// turn taught the block. PromptBuilder.EnableSceneNarration (the mod's "Scene Narration" setting) decides whether an
// ordinary conversation is taught the block at all. If it is wrong one way, the player who turned it off still reads
// the purple descriptions (and pays for them); the other way, a captive scene loses the only channel that carries
// what is done to the prisoner, or a player who kept the default loses the narration they liked.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SceneNarrationSwitchPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(bool sceneNarration, EncounterContext context, AdultContentLevel level = AdultContentLevel.Off)
         => new PromptBuilder {AdultLevel = level, EnableSceneNarration = sceneNarration}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10}, context);

      // STAKE: with the switch off, the opening turn must not ask for the tableau the player turned off, nor teach the
      // block on any turn, or the purple description comes back on the very first reply.
      [Test]
      public void GIVEN_scene_narration_off_WHEN_a_conversation_opens_THEN_no_narration_is_taught_or_asked_for()
      {
         string prompt = Build(false, new EncounterContext {IsConversationOpening = true});

         prompt.Should().NotContain("[/NARRATION]");
         prompt.Should().NotContain("A neutral camera on the SETTING");
         prompt.Should().NotContain("THIS TURN OPENS THE SCENE");
         prompt.Should().NotContain("USE [NARRATION] SPARINGLY");
      }

      // STAKE: with no channel for the setting, a model may recite the room aloud in the character's mouth; it is told
      // where the place may live instead (a gesture), and never in speech.
      [Test]
      public void GIVEN_scene_narration_off_WHEN_built_THEN_the_setting_is_sent_to_the_gestures()
      {
         string prompt = Build(false, new EncounterContext());

         prompt.Should().Contain("NO SCENE DESCRIPTION: write no [NARRATION] block");
         prompt.Should().Contain("describe the room aloud in your spoken words");
      }

      // STAKE: the default is unchanged, so a player who never touched the setting keeps the opening tableau.
      [Test]
      public void GIVEN_scene_narration_on_WHEN_a_conversation_opens_THEN_the_tableau_is_asked_for_as_before()
      {
         string prompt = Build(true, new EncounterContext {IsConversationOpening = true});

         prompt.Should().Contain("THIS TURN OPENS THE SCENE");
         prompt.Should().Contain("A neutral camera on the SETTING");
         prompt.Should().NotContain("NO SCENE DESCRIPTION");
      }

      // STAKE: a captive scene puts what is done to the prisoner in [NARRATION] and nowhere else; turning off the
      // description of ordinary conversations must not silence that.
      [Test]
      public void GIVEN_scene_narration_off_WHEN_the_player_is_held_captive_THEN_the_captive_narration_contract_stays()
      {
         var captive = new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Captive, CaptiveIntent = CaptiveSceneIntent.PersonalDesire};

         string off = Build(false, captive, AdultContentLevel.Hardcore);
         string on = Build(true, captive, AdultContentLevel.Hardcore);

         off.Should().Be(on);
         off.Should().Contain("[NARRATION]");
      }
   }
}

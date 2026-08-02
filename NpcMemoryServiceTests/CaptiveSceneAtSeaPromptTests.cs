// Code written by Gabriel Mailhot, 02/08/2026.
// Player report: captured by Sea Raiders, the captive scene played as an Aserai bandit "in a tent in front
// of a camp fire" - the fact the player was AT SEA never registered, so the model invented a land camp. The
// mod resolves the at-sea verdict from the party actually HOLDING the player captive (EncounterContextBuilder
// .ResolveCaptiveSceneAtSea, mirrored in ChatViewModel), since MobileParty.MainParty.IsCurrentlyAtSea (the
// general AtSea flag) reads stale once the player has no party of their own. This pins the SDK half: when the
// context says the captive scene is at sea, the captive prompt must say so plainly, for both the sexual (CNC)
// branch and the non-sexual bandit-menace branch (Extortion/Intimidation), which is exactly the intent a Sea
// Raiders capture would carry.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptiveSceneAtSeaPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Wick the Cutthroat",
         Faction = "looters",
         Clan = "looters"
      };

      private static string BuildCaptivePrompt(bool atSea, CaptiveSceneIntent intent, bool captorIsBandit = true)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = intent,
            CaptorIsBandit = captorIsBandit,
            CaptiveSceneAtSea = atSea
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // The core fix: a captive scene flagged at sea must tell the model plainly it is aboard a ship, so it
      // stops inventing a tent and a campfire. Checked on a SEXUAL intent (the CNC branch).
      [Test]
      public void GIVEN_a_captive_scene_at_sea_WHEN_building_the_prompt_THEN_it_is_told_the_scene_is_aboard_a_ship()
      {
         string prompt = BuildCaptivePrompt(atSea: true, intent: CaptiveSceneIntent.PersonalDesire);

         prompt.Should().Contain("ABOARD A SHIP");
         prompt.Should().Contain("Never invent a land camp or a fire");
      }

      // Sea Raiders are a bandit captor: the actual player report almost certainly landed on the non-sexual
      // bandit-menace branch (Extortion/Intimidation), which is the one whose FIXED opener elsewhere
      // hardcodes "the bandit camp" (the SDK note must still fire for this branch, not only the CNC one).
      [Test]
      public void GIVEN_a_bandit_menace_scene_at_sea_WHEN_building_the_prompt_THEN_the_at_sea_note_still_fires()
      {
         string prompt = BuildCaptivePrompt(atSea: true, intent: CaptiveSceneIntent.Intimidation);

         prompt.Should().Contain("ABOARD A SHIP");
      }

      // The negative case: an ordinary land captivity must not be told it is at sea, or every land-based
      // capture would grow a nonsensical ship note.
      [Test]
      public void GIVEN_a_captive_scene_on_land_WHEN_building_the_prompt_THEN_no_at_sea_note_is_added()
      {
         string prompt = BuildCaptivePrompt(atSea: false, intent: CaptiveSceneIntent.PersonalDesire);

         prompt.Should().NotContain("ABOARD A SHIP");
      }
   }
}

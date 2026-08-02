// Code written by Gabriel Mailhot, 01/08/2026.
// Player report: NPCs over-referenced the player's soldiers in INDOOR scenes (a city, a keep) where the
// army would not physically be present ("THE FORCE AT THE PLAYER'S BACK" said riders/guards were "at
// their back" even inside a settlement, keep, tavern, or dungeon the player walked into, possibly alone
// or under guard. The block is now scene-aware: Outdoor/Battlefield/Unknown keep the original "visible
// force" wording (PlayerForcePromptTests already pins those bands); Settlement/Keep/Tavern/Dungeon get a
// quieter STANDING note that states the force's SIZE without placing any soldiers in the room. These
// tests pin that split and that the solo "RIDES ALONE" wording is unaffected by scene.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PlayerForceScenePromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(int troopCount, SceneType scene)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {PlayerPartyTroopCount = troopCount, Scene = scene});

      // The root bug: a keep is walls the player walked through, not a road their army rides. The old
      // "at their back" / "rides"/"marches" wording put two hundred soldiers inside the keep's hall.
      [TestCase(SceneType.Keep)]
      [TestCase(SceneType.Settlement)]
      [TestCase(SceneType.Tavern)]
      [TestCase(SceneType.Dungeon)]
      public void GIVEN_an_indoor_scene_WHEN_the_prompt_is_built_THEN_the_force_is_not_placed_at_their_back(SceneType scene)
      {
         string prompt = Build(200, scene);

         prompt.Should().NotContain("at their back");
         prompt.Should().Contain("are not");
         prompt.Should().Contain("here");
         prompt.Should().Contain("beyond these walls");
      }

      // The replacement still tells the model the player commands a real force (a fact worth knowing), it
      // simply refuses to seat it in the room.
      [TestCase(SceneType.Keep)]
      [TestCase(SceneType.Settlement)]
      [TestCase(SceneType.Tavern)]
      [TestCase(SceneType.Dungeon)]
      public void GIVEN_an_indoor_scene_WHEN_the_prompt_is_built_THEN_the_standing_note_states_the_force_size(SceneType scene)
      {
         string prompt = Build(200, scene);

         prompt.Should().Contain("THE PLAYER'S STANDING");
         prompt.Should().Contain("The player commands a warband of well over a hundred soldiers");
         prompt.Should().Contain("Do not place their soldiers in this scene");
      }

      // Outdoors and on a battlefield the force is genuinely present: this must render EXACTLY as before
      // (PlayerForcePromptTests pins the bands), so a scene split cannot silently regress the open-road case.
      [TestCase(SceneType.Outdoor)]
      [TestCase(SceneType.Battlefield)]
      public void GIVEN_an_outdoor_scene_WHEN_the_prompt_is_built_THEN_the_force_still_rides_at_their_back(SceneType scene)
      {
         string prompt = Build(200, scene);

         prompt.Should().Contain("at their back");
         prompt.Should().Contain("THE FORCE AT THE PLAYER'S BACK");
         prompt.Should().NotContain("THE PLAYER'S STANDING");
      }

      // Unknown scene (no scene resolved) is the old default and must keep the original behaviour too, so
      // a caller that never sets Scene sees no change from this fix.
      [Test]
      public void GIVEN_an_unresolved_scene_WHEN_the_prompt_is_built_THEN_the_force_still_rides_at_their_back()
      {
         string prompt = Build(200, SceneType.Unknown);

         prompt.Should().Contain("at their back");
         prompt.Should().NotContain("THE PLAYER'S STANDING");
      }

      // The lone-traveller wording is untouched by this fix: a party of one indoors is still stated as
      // riding alone, never folded into the new standing note (which only applies to > 1).
      [TestCase(SceneType.Keep)]
      [TestCase(SceneType.Settlement)]
      public void GIVEN_a_party_of_one_in_an_indoor_scene_WHEN_the_prompt_is_built_THEN_the_rides_alone_wording_still_renders(SceneType scene)
      {
         string prompt = Build(1, scene);

         prompt.Should().Contain("THE PLAYER RIDES ALONE");
         prompt.Should().NotContain("THE PLAYER'S STANDING");
      }
   }
}

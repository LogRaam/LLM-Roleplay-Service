// Code written by Gabriel Mailhot, 16/08/2026.
// The captive scene's BE INVENTIVE block invites the captor to improvise, but a female captor reached for an
// invented "strap-on" / harness that nothing in the scene had established, conjured from nowhere on a battlefield
// capture (player report, rp_bench scene 4). The GROUND WHAT YOU USE clause keeps the improvisation anchored: the
// captor acts with their own body and authority and what is plausibly at hand, and commands those who can when
// they want the prisoner penetrated, rather than summoning an implement they would not actually have. These tests
// pin that the clause is taught in a captive scene and absent from an ordinary conversation.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptivePropGroundingPromptTests
   {
      // The exact report: an unestablished strap-on conjured mid-scene. The captive rules must teach the captor to
      // ground what they use and NOT summon an implement (a toy, a strap-on) the scene never set up.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_building_the_prompt_THEN_it_grounds_implements_and_forbids_conjuring_a_strap_on()
      {
         string prompt = Build(PlayerStatusVsNpc.Captive);

         prompt.Should().Contain("GROUND WHAT YOU USE");
         prompt.Should().Contain("no strap-on");
         prompt.Should().Contain("COMMAND those who can");
      }

      // The clause belongs to the captive-scene rules only: an ordinary conversation never enters that block, so it
      // must not carry this instruction (it would be nonsense outside a captor holding a prisoner).
      [Test]
      public void GIVEN_an_ordinary_conversation_WHEN_building_the_prompt_THEN_the_grounding_clause_is_absent()
      {
         string prompt = Build(PlayerStatusVsNpc.Free);

         prompt.Should().NotContain("GROUND WHAT YOU USE");
      }

      #region private

      private static string Build(PlayerStatusVsNpc status)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = false};
         var context = new EncounterContext
         {
            LeanLevel = LeanPromptLevel.Full,
            PlayerStatus = status,
            CaptiveIntent = CaptiveSceneIntent.PersonalDesire
         };

         var npc = new NpcProfile {Id = "npc_test", Name = "Susada", Faction = "Empire", Clan = "Osticos"};

         return builder.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      #endregion
   }
}

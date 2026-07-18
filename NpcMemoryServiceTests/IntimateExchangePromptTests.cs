// The consensual intimate scene is an EXCHANGE, not a performance (audit 2026-07-17, P1 — the
// audit's central defect: nothing stopped the model from acting, speaking, deciding, and concluding
// in the player's place). The block is taught right after "DURING PHYSICAL INTIMACY", gated
// AdultLevel >= Explicit like that block, and never in a captive scene (which returns earlier with
// its own contract). These tests pin the gate on all four faces.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class IntimateExchangePromptTests
   {
      private const string ExchangeHeading = "AN INTIMATE SCENE IS AN EXCHANGE, NOT A PERFORMANCE:";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         // The consent rules (and everything after them) return early for a profile with no
         // romantic layer or an incompatible orientation — the same gate "DURING PHYSICAL
         // INTIMACY" sits under.
         Romantic = new RomanticProfile {Orientation = SexualOrientation.Bisexual}
      };

      private static string BuildLordPrompt(AdultContentLevel level)
         => new PromptBuilder {AdultLevel = level}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext());

      // The block's whole point: the player's reactions, pleasure, and willingness belong to the
      // player; the model escalates ONE step per turn and never concludes alone.
      [Test]
      public void GIVEN_explicit_level_WHEN_built_THEN_the_exchange_block_is_present()
      {
         string prompt = BuildLordPrompt(AdultContentLevel.Explicit);

         prompt.Should().Contain(ExchangeHeading);
         prompt.Should().Contain("Never act, speak, or decide FOR the player");
         prompt.Should().Contain("Escalate ONE step per turn");
      }

      // Hardcore is above Explicit, so a consensual (non-captive) scene at Hardcore carries the same
      // exchange rules.
      [Test]
      public void GIVEN_hardcore_level_without_a_captive_scene_WHEN_built_THEN_the_exchange_block_is_present()
      {
         BuildLordPrompt(AdultContentLevel.Hardcore).Should().Contain(ExchangeHeading);
      }

      // Below Explicit there is no physical-intimacy teaching at all — the block must stay out.
      [Test]
      public void GIVEN_off_or_mature_level_WHEN_built_THEN_the_exchange_block_is_absent()
      {
         BuildLordPrompt(AdultContentLevel.Off).Should().NotContain(ExchangeHeading);
         BuildLordPrompt(AdultContentLevel.Mature).Should().NotContain(ExchangeHeading);
      }

      // A captive scene has its OWN voice/pacing contract and returns from the consent rules before
      // this block: the consensual exchange teaching must not leak into it even at Hardcore.
      [Test]
      public void GIVEN_a_captive_scene_at_hardcore_WHEN_built_THEN_the_exchange_block_is_absent()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true};
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.PersonalDesire
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(ExchangeHeading);
      }
   }
}

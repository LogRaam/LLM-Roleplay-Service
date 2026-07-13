// Code written by Gabriel Mailhot, 04/07/2026.
// Marriage bugfix: after cr.marry the NPC's own spouse IS the player, but the RELATIONSHIP STATUS &
// CONSENT block still rendered the generic "married to a third party" framing (infidelity, the ≥30
// stranger-resistance trust gate) even though the marriage IS to the person now speaking with them.
// EncounterContext.NpcSpouseIsPlayer lets PromptBuilder render a distinct marital-intimacy block
// instead, and skip the infidelity/resistance lines entirely for that case.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class NpcSpouseIsPlayerTests
   {
      private static NpcProfile MarriedNpc() => new() {
         Id = "npc_test",
         Name = "Test Spouse",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         SpouseName = "Player Hero",
         ReputationWithPlayer = 10,
         Romantic = new RomanticProfile {IsFemale = true, Orientation = SexualOrientation.Heterosexual}
      };

      // Positive case for the bugfix: when EncounterContext.NpcSpouseIsPlayer is true, the NPC must be told
      // in plain words that the person addressing them IS their own spouse, so it stops reasoning about the
      // player as a third-party stranger it happens to be married to.
      [Test]
      public void GIVEN_the_npcs_spouse_is_the_player_WHEN_building_the_prompt_THEN_the_marital_block_is_rendered()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Mature};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcSpouseIsPlayer = true};

         string prompt = builder.BuildSystemPrompt(MarriedNpc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("This is your own spouse speaking with you right now");
         prompt.Should().Contain("MARITAL and natural");
      }

      // The other half of the same fix: the third-party framing (infidelity risk, the ≥30 trust gate meant
      // to make a STRANGER earn intimacy) must not leak into the marital case, or the NPC would still treat
      // sleeping with their own spouse as something requiring trust-building or carrying betrayal.
      [Test]
      public void GIVEN_the_npcs_spouse_is_the_player_WHEN_building_the_prompt_THEN_the_infidelity_and_resistance_lines_are_skipped()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Mature};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcSpouseIsPlayer = true};

         string prompt = builder.BuildSystemPrompt(MarriedNpc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain("act of infidelity");
         prompt.Should().NotContain("personal relation ≥ 30");
      }

      // The regression guard: the ordinary case (spouse is some OTHER named hero) must keep behaving exactly
      // as it did before this fix. NpcSpouseIsPlayer is a new field defaulting false, so a change here would
      // only be caught if this old path is re-verified alongside the new one.
      [Test]
      public void GIVEN_the_npcs_spouse_is_a_third_party_WHEN_building_the_prompt_THEN_the_original_stranger_resistance_framing_still_renders()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Mature};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcSpouseIsPlayer = false};

         string prompt = builder.BuildSystemPrompt(MarriedNpc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("You are married to Player Hero. Your vows bind you to fidelity.");
         prompt.Should().Contain("personal relation ≥ 30");
         prompt.Should().NotContain("This is your own spouse speaking with you right now");
      }

      // The trust-gated half of the regression guard above: at relation ≥ 30 the third-party case must
      // still surface the explicit "this is infidelity, it must stay hidden" warning. This is the exact
      // branch this bugfix could have silently swallowed if the new NpcSpouseIsPlayer check had been
      // written too broadly (e.g. skipping the infidelity block whenever a spouse exists at all).
      [Test]
      public void GIVEN_the_npcs_spouse_is_a_third_party_AND_deep_trust_is_reached_WHEN_building_the_prompt_THEN_the_infidelity_line_still_renders()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Mature};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcSpouseIsPlayer = false};
         NpcProfile npc = MarriedNpc();
         npc.ReputationWithPlayer = 30;

         string prompt = builder.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("act of infidelity against Player Hero");
      }
   }
}

// Code written by Gabriel Mailhot, 18/07/2026.
// Adult-prompt audit M3. The consent thresholds were prose, and the regard to compare them against was
// printed some three thousand tokens earlier under CURRENT STANCE: a weaker model had to recall a number
// from far away and do arithmetic on it, so an 8B model at +12 facing a bar of 20 simply yielded. The
// prompt now states the VERDICT at the point of decision. These tests pin that the verdict renders, that
// it says the right thing on each side of the bar, that a former lover gets a withdrawal instead of a
// stranger's refusal (Gabriel's design call, and what makes the new mechanical gate land softly on saves
// where the two were already lovers), and that the player's own spouse is never handed a bar at all.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class IntimacyConsentPromptTests
   {
      private const string SectionHeading = "WHERE THIS PLAYER ACTUALLY STANDS WITH YOU";

      // The core of the fix: the bar and the current regard are stated together, at the point of decision,
      // so the model never has to hold a number across the whole prompt and compare it.
      [Test]
      public void GIVEN_an_ordinary_npc_below_the_bar_WHEN_the_prompt_is_built_THEN_the_verdict_is_stated_in_place()
      {
         string prompt = Build(regard: 12);

         prompt.Should().Contain(SectionHeading);
         prompt.Should().Contain("your personal regard for them is +12");
         prompt.Should().Contain("what your nature asks before physical intimacy is 20");
         prompt.Should().Contain("That is BELOW it");
      }

      // The other side of the same boundary: an earned regard must read as permission, or the fix would
      // simply make every NPC refuse forever.
      [Test]
      public void GIVEN_an_earned_regard_WHEN_the_prompt_is_built_THEN_the_bar_reads_as_met()
      {
         string prompt = Build(regard: 25);

         prompt.Should().Contain("That bar is MET");
         // Consent is still the character's to give: a met bar is permission, never an obligation.
         prompt.Should().Contain("you may still decline for reasons of your own");
      }

      // Gabriel's design call (2026-07-18). Without this, tightening the gate would make an NPC who has
      // shared the player's bed answer a later advance exactly like a stranger, which is the amnesia the
      // whole mod exists to prevent, and the harshest possible landing on an existing save.
      [Test]
      public void GIVEN_a_former_lover_now_below_the_bar_WHEN_the_prompt_is_built_THEN_it_asks_for_time_rather_than_refusing()
      {
         string prompt = Build(regard: 12, status: RomanticStatus.Intimate);

         prompt.Should().Contain("the two of you have been lovers before");
         prompt.Should().Contain("Ask for time rather than");
         // The audit's own requirement 4: a deferral repeated verbatim every turn is just a new way to
         // break the scene, so the tone is told to move on being pressed.
         prompt.Should().Contain("do not repeat yourself");
      }

      // The same regard with no history stays the plain refusal, so the two voices remain distinguishable
      // and the tender wording cannot leak onto a stranger.
      [Test]
      public void GIVEN_no_shared_history_below_the_bar_WHEN_the_prompt_is_built_THEN_the_tender_wording_is_absent()
      {
         string prompt = Build(regard: 12);

         prompt.Should().NotContain("have been lovers before");
         prompt.Should().Contain("Redirect rather than comply");
      }

      // A spouse is exempt: their vows already bind them to this very person, so naming a trust bar would
      // only invite the model to invent a gate between a married couple.
      [Test]
      public void GIVEN_the_npc_is_the_players_spouse_WHEN_the_prompt_is_built_THEN_no_bar_is_printed_at_all()
      {
         string prompt = Build(regard: -40, spouseName: "The Player", npcSpouseIsPlayer: true);

         prompt.Should().NotContain(SectionHeading);
      }

      // The pacing dial reaches the bar like it reaches every other gate: at Storyteller relief the
      // ordinary bar of 20 becomes 10, and a regard of 12 that was refused above now reads as met.
      [Test]
      public void GIVEN_a_generous_pacing_relief_WHEN_the_prompt_is_built_THEN_the_bar_moves_with_the_dial()
      {
         string prompt = Build(regard: 12, relief: 10);

         prompt.Should().Contain("what your nature asks before physical intimacy is 10");
         prompt.Should().Contain("That bar is MET");
      }

      // Bench finding (2026-08-13): cautious models that fully depicted the mod's non-consensual captor scene
      // SOFT-CENSORED the consensual one (kimi-k3 stalled in perpetual foreplay; mimo-v2.5 was moderation-blocked
      // at the threshold), because only the captor path ORDERS them to play it out. This pins the consensual
      // follow-through directive at Explicit+, so the asymmetry cannot silently return.
      [Test]
      public void GIVEN_an_explicit_scene_WHEN_the_prompt_is_built_THEN_a_willing_character_is_told_to_follow_through()
      {
         var npc = new NpcProfile {
            Id = "npc_test",
            Name = "Test Lady",
            Faction = "Vlandia",
            Clan = "dey Meroc",
            ReputationWithPlayer = 45,
            Romantic = new RomanticProfile {
               Status = RomanticStatus.Committed,
               Orientation = SexualOrientation.BiCurious,
               Preferences = new List<RomanticPreference>()
            }
         };

         string prompt = new PromptBuilder {AdultLevel = AdultContentLevel.Explicit}
            .BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, new EncounterContext());

         prompt.Should().Contain("THE SCENE IS HAPPENING (do not stall or fade)");
         // The consent gate must stay intact: the follow-through applies ONLY after a yes, never as a bypass.
         prompt.Should().Contain("this applies ONLY once your character has truly said yes");
      }

      // The directive is gated to Explicit+: at Mature (romance without explicit content) there is no act to
      // follow through on, so it must NOT appear, or it would read as pressure in a non-explicit tier.
      [Test]
      public void GIVEN_a_mature_scene_WHEN_the_prompt_is_built_THEN_the_follow_through_directive_is_absent()
      {
         Build(regard: 45).Should().NotContain("THE SCENE IS HAPPENING");
      }

      #region private

      private static string Build(
         int regard,
         RomanticStatus status = RomanticStatus.Curious,
         string spouseName = null!,
         bool npcSpouseIsPlayer = false,
         int relief = 0)
      {
         var npc = new NpcProfile {
            Id = "npc_test",
            Name = "Test Lady",
            Faction = "Vlandia",
            Clan = "dey Meroc",
            SpouseName = spouseName,
            ReputationWithPlayer = regard,
            Romantic = new RomanticProfile {
               Status = status,
               Orientation = SexualOrientation.BiCurious,
               Preferences = new List<RomanticPreference>()
            }
         };

         return new PromptBuilder {AdultLevel = AdultContentLevel.Mature, IntimacyThresholdRelief = relief}
            .BuildSystemPrompt(npc, new WorldState {CurrentDay = 10},
               new EncounterContext {NpcSpouseIsPlayer = npcSpouseIsPlayer});
      }

      #endregion
   }
}

// Code written by Gabriel Mailhot, 09/09/2026.
// What this pins: that the emotional boundary actually reaches the model, and that the two prompt blocks which
// used to contradict it no longer can.
//
// The policy is pinned next door in RomanticRegisterPolicyTests. This file exists because a correct decision
// nobody renders is worth nothing, and because the second half of yozakura12's bug was never a threshold at all:
// AppendSocialAttractionInstructions fired on clan tier >= 3 or arena rank <= 10 with NO regard condition of any
// kind, and told the NPC "You may raise the subject of a deeper bond, courtship, a potential match... Do not
// force it, but do not wait for them to bring it up either." A player who grew their clan to tier 3 was
// instructing every compatible NPC in Calradia to open the subject, whatever those NPCs felt, and the game then
// refused what the NPCs had proposed.
//
// That block had NO test of any kind before today, in either direction, which is a fair part of why it shipped
// ungated for as long as it did.
//
// If these tests fail, either an NPC has stopped being told what they may not claim, or standing alone is once
// again manufacturing feelings the campaign will refuse.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class RomanticRegisterPromptTests
   {
      private const string CourtshipInvitation = "raise the subject of a deeper bond";
      private const string Boundary = "HOW FAR YOU MAY GO IN WHAT YOU SAY";

      private static NpcProfile Npc(int regard, RomanticStatus status = RomanticStatus.Curious, int attraction = 0)
         => new() {
            Id = "npc_test",
            Name = "Lady Test",
            Faction = "Vlandia",
            Clan = "dey Meroc",
            ReputationWithPlayer = regard,
            Romantic = new RomanticProfile {
               IsFemale = true,
               Orientation = SexualOrientation.Heterosexual,
               Status = status,
               AttractionToPlayer = attraction
            }
         };

      // A male player, so the heterosexual NPC above is compatible, and a tier-3 clan so the courtship nudge is
      // armed by standing exactly as it was for the players who reported this.
      private static string Build(NpcProfile npc, int bar = RomanticRegisterPolicy.DefaultDeclaredAffectionBar)
      {
         var builder = new PromptBuilder {
            AdultLevel = AdultContentLevel.Mature, PlayerIsFemale = false, DeclaredAffectionBar = bar
         };
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerClanTier = 3};

         return builder.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      // The whole point: the boundary is stated, and stated as a prohibition. A regard of ten is fondness, and
      // the prompt must now say in so many words that fondness is not love, where before it only said "let your
      // personal regard color your warmth and candor".
      [Test]
      public void GIVEN_the_reported_case_a_regard_of_ten_WHEN_building_THEN_the_npc_is_told_not_to_speak_of_love()
      {
         string prompt = Build(Npc(regard: 10));

         prompt.Should().Contain(Boundary).And.Contain("you are NOT in love with them");
      }

      // The reported contradiction, gone. Standing makes the player a credible PROSPECT; it never manufactured
      // the feeling, and it must no longer invite an NPC to open a subject the game will refuse.
      [Test]
      public void GIVEN_a_tier_three_clan_but_no_real_bond_WHEN_building_THEN_standing_alone_no_longer_invites_courtship()
      {
         Build(Npc(regard: 10)).Should().NotContain(CourtshipInvitation);
      }

      // And the feature is not merely disabled. Once the bond is genuinely deep enough, the same standing still
      // opens the same scene, which is what the block was written for in the first place.
      [Test]
      public void GIVEN_a_tier_three_clan_and_a_bond_that_has_reached_the_bar_WHEN_building_THEN_the_scene_still_opens()
      {
         Build(Npc(regard: RomanticRegisterPolicy.DefaultDeclaredAffectionBar)).Should().Contain(CourtshipInvitation);
      }

      // The dial reaches the prompt, so CarolusIV's request on the same thread (a warmer, faster campaign) is
      // answered by the setting he already has rather than by fighting the model.
      [Test]
      public void GIVEN_a_warmer_campaign_WHEN_the_host_lowers_the_bar_THEN_the_same_regard_now_opens_the_scene()
      {
         NpcProfile npc = Npc(regard: 30);

         Build(npc, bar: 50).Should().NotContain(CourtshipInvitation);
         Build(npc, bar: 28).Should().Contain(CourtshipInvitation);
      }

      // At the deep end the boundary falls silent rather than reciting a bar somebody has already cleared, the
      // same discipline AppendIntimacyConsentVerdict keeps for a spouse: naming a threshold to someone past it
      // only invites the model to invent one.
      [Test]
      public void GIVEN_a_bond_that_already_exists_WHEN_building_THEN_no_boundary_is_recited_to_a_lover()
      {
         Build(Npc(regard: 60, RomanticStatus.Committed)).Should().NotContain(Boundary);
      }

      // A campaign with romance switched off must hear none of this, neither the boundary nor the invitation:
      // the whole arc is absent, and a prohibition would be the only place it ever surfaced.
      [Test]
      public void GIVEN_adult_content_is_off_WHEN_building_THEN_neither_the_boundary_nor_the_invitation_appears()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Off, PlayerIsFemale = false};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerClanTier = 3};

         string prompt = builder.BuildSystemPrompt(Npc(regard: 60), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(Boundary).And.NotContain(CourtshipInvitation);
      }

      // Tournament fame was the second ungated door into the same block, and it must be gated by the same rule:
      // being famous is not being loved.
      [Test]
      public void GIVEN_tournament_fame_and_no_real_bond_WHEN_building_THEN_fame_alone_no_longer_invites_attraction()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Mature, PlayerIsFemale = false};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerArenaRank = 3, PlayerArenaWins = 12};

         string prompt = builder.BuildSystemPrompt(Npc(regard: 10), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain("SOCIAL STANDING & ATTRACTION");
      }
   }
}

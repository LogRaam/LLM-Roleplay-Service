// Code written by Gabriel Mailhot, 22/07/2026.
// Audit of "what the fiction can reach that the machinery cannot, in this particular mode": every offer
// section in PromptBuilder returns early when the player is this NPC's prisoner (consort, secret lover,
// deliver prisoner, give item, lord recruitment, marriage, scheme recruitment, scheme warning, private
// audience). AppendRecruitment and AppendMercenaryOffer were the only two without that guard. This matters
// because PlayerStatusVsNpc.Captive is NOT scene-only: PlayerStandingPolicy derives it from the raw engine
// fact (Hero.MainHero.IsPrisoner && PartyBelongedToAsPrisoner.LeaderHero == npc), with no adult gate, so an
// ordinary conversation with the lord who holds you in his dungeon reached the mercenary contract offer.
// These tests pin both halves: the offers vanish for a captive player, and they still appear for a free one
// (an over-broad guard would silently delete hiring and mercenary service from the whole mod).

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptivePlayerOfferExclusionTests
   {
      // Matched on the em-dash-free tail of the real heading, so the constant carries no character Gabriel strips.
      private const string RecruitmentHeading = "YOU CAN BE HIRED";
      private const string MercenaryHeading = "MERCENARY SERVICE";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The reported shape of the bug: the captor lord's own kingdom can extend a contract, so the section
      // rendered while the player sat in his cells. Offering paid service to your own prisoner is incoherent,
      // and the bridge would enroll the clan for real while the player is still held.
      [Test]
      public void GIVEN_the_player_is_this_lords_prisoner_WHEN_built_THEN_the_mercenary_contract_is_not_offered()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            MercenaryOfferKingdom = "Vlandia"
         });

         prompt.Should().NotContain(MercenaryHeading);
      }

      // Guards the fix against over-reach: the offer must survive intact for a free player, which is the only
      // situation it was ever written for.
      [Test]
      public void GIVEN_a_free_player_WHEN_built_THEN_the_mercenary_contract_is_still_offered()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            MercenaryOfferKingdom = "Vlandia"
         });

         prompt.Should().Contain(MercenaryHeading);
      }

      // Same family, thinner path (a captive player is rarely face to face with a hireable wanderer), but the
      // guard is the consistency that makes the rule auditable: no offer section may reach a prisoner.
      [Test]
      public void GIVEN_the_player_is_a_prisoner_WHEN_built_THEN_the_hire_negotiation_is_not_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CompanionAskingPrice = 400
         });

         prompt.Should().NotContain(RecruitmentHeading);
      }

      // The other half of that boundary: hiring a wanderer is a core feature and must be untouched.
      [Test]
      public void GIVEN_a_free_player_WHEN_built_THEN_the_hire_negotiation_is_still_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            CompanionAskingPrice = 400
         });

         prompt.Should().Contain(RecruitmentHeading);
      }
   }
}

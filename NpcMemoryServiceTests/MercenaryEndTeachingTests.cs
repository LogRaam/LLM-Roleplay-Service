// Code written by Gabriel Mailhot, 04/08/2026.
// end_mercenary fix (player report, 2026-08-04): "The King agreed to free me from my mercenary contract.
// Nothing happens." join_as_mercenary (AppendMercenaryOffer) already had a real action wired to it; ending
// the very same contract did not, so the promise never fired. These tests pin the SYMMETRIC conditional
// teaching: end_mercenary is taught only when EncounterContext.PlayerIsMercenary is true, exactly the same
// contract AppendMercenaryOffer keeps for join_as_mercenary via MercenaryOfferKingdom, and it is never taught
// otherwise (a model must not invent an action the game bridge cannot honor).

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class MercenaryEndTeachingTests
   {
      // Matched on the em-dash-free tail of the real heading, so the constant carries no character Gabriel strips.
      private const string EndMercenaryHeading = "THE CONTRACT CAN BE DISSOLVED";
      private const string EndMercenaryActionType = "type: end_mercenary";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The reported bug's fix: a player genuinely under mercenary service must be taught the action that
      // actually dissolves it, or the King's spoken release stays an empty promise exactly as reported.
      [Test]
      public void GIVEN_the_player_is_under_mercenary_service_WHEN_built_THEN_end_mercenary_is_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            PlayerIsMercenary = true
         });

         prompt.Should().Contain(EndMercenaryHeading);
         prompt.Should().Contain(EndMercenaryActionType);
      }

      // The other half of the conditional-teaching contract: when the player owes no mercenary contract at
      // all, the model must never be handed an action the bridge would only refuse (or worse, be free to
      // invent a fictional contract to dissolve).
      [Test]
      public void GIVEN_the_player_is_not_under_mercenary_service_WHEN_built_THEN_end_mercenary_is_not_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            PlayerIsMercenary = false
         });

         prompt.Should().NotContain(EndMercenaryHeading);
         prompt.Should().NotContain(EndMercenaryActionType);
      }

      // Symmetric with AppendMercenaryOffer's own captive carve-out (CaptivePlayerOfferExclusionTests): a
      // captor holding the player prisoner is not renegotiating the player's clan's mercenary contract this
      // exchange, even if the underlying contract fact happens to be true.
      [Test]
      public void GIVEN_the_player_is_a_captive_WHEN_built_THEN_end_mercenary_is_not_taught_even_if_under_service()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            PlayerIsMercenary = true
         });

         prompt.Should().NotContain(EndMercenaryHeading);
      }
   }
}

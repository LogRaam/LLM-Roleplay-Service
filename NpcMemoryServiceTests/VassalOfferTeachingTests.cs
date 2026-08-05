// Code written by Gabriel Mailhot, 05/08/2026.
// join_as_vassal fix (player report, 2026-08-05): "asked the legitimate ruler to become her vassal, the LLM
// narrated acceptance and threw 6000 denars, but the game only ever registered the player as a mercenary" -
// join_as_mercenary (AppendMercenaryOffer) already had a real action wired to it; a permanent oath of
// vassalage did not, the same missing-executor class as end_mercenary. These tests pin the conditional
// teaching: join_as_vassal is taught only when EncounterContext.VassalOfferKingdom is set (the host already
// confirmed the NPC truly rules that kingdom and the player's clan is not already a full vassal), and never
// otherwise - a model must not invent an action the game bridge cannot honor.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class VassalOfferTeachingTests
   {
      // Matched on the em-dash-free tail of the real heading, so the constant carries no character Gabriel strips.
      private const string VassalOfferHeading = "YOU CAN SWEAR THE PLAYER'S CLAN";
      private const string VassalOfferActionType = "type: join_as_vassal";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The reported bug's fix: a lord confirmed by the host to truly rule the kingdom must be taught the
      // action that actually swears the player's clan in, or the narrated oath stays an empty promise exactly
      // as reported.
      [Test]
      public void GIVEN_the_npc_can_offer_vassalage_WHEN_built_THEN_join_as_vassal_is_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            VassalOfferKingdom = "Vlandia"
         });

         prompt.Should().Contain(VassalOfferHeading);
         prompt.Should().Contain(VassalOfferActionType);
      }

      // The other half of the conditional-teaching contract: when the host never resolved a ruling kingdom
      // (NPC is not the ruler, or the player already owes a vassal oath), the model must never be handed an
      // action the bridge would only refuse.
      [Test]
      public void GIVEN_the_npc_cannot_offer_vassalage_WHEN_built_THEN_join_as_vassal_is_not_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            VassalOfferKingdom = null
         });

         prompt.Should().NotContain(VassalOfferHeading);
         prompt.Should().NotContain(VassalOfferActionType);
      }

      // Symmetric with AppendMercenaryOffer's own captive carve-out: a captor holding the player prisoner is
      // not swearing them into vassalage this exchange, even if the underlying rulership fact happens to hold.
      [Test]
      public void GIVEN_the_player_is_a_captive_WHEN_built_THEN_join_as_vassal_is_not_taught_even_if_offerable()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            VassalOfferKingdom = "Vlandia"
         });

         prompt.Should().NotContain(VassalOfferHeading);
      }
   }
}

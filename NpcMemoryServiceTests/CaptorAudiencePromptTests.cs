// Code written by Gabriel Mailhot, 01/11/2026.
// A lord's talk with his prisoner runs at every content level (Windmolen, 26/10/2026; Gabriel: "il faudra d'autres scenarios
// que de la maltraitance"). The prompt must reach the model at Off, carry none of the Hardcore scene's rules, and teach only the
// ways out the host declared possible. If it is wrong, a Standard game gets a scene with no instructions, or worse the wrong
// ones, or the captor promises a release the game cannot make real.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptorAudiencePromptTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_test", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};

      private static string Prompt(CaptiveSceneIntent intent, AdultContentLevel level = AdultContentLevel.Off, int price = 0, int paroleDays = 0, string exchange = null, bool persuasion = false)
      {
         var builder = new PromptBuilder {AdultLevel = level};
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive, CaptiveIntent = intent, CaptorRansomPrice = price, CaptorParoleDays = paroleDays,
            CaptorParoleRealm = "Vlandia", CaptorExchangeCaptive = exchange, CaptorAllowsPersuasionRelease = persuasion
         };

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // STAKE: these scenes exist for levels that had none; at Off the intimacy section returns early, and a talk sent there
      // would reach the model with no word of what the scene is.
      [TestCase(CaptiveSceneIntent.Courtesy)]
      [TestCase(CaptiveSceneIntent.Bargain)]
      [TestCase(CaptiveSceneIntent.Parole)]
      [TestCase(CaptiveSceneIntent.TornFriend)]
      public void GIVEN_content_off_WHEN_a_captor_sends_for_the_player_THEN_the_model_knows_what_the_scene_is(CaptiveSceneIntent intent)
      {
         Prompt(intent).Should().Contain("YOU HAVE SENT FOR THEM TO TALK");
      }

      // STAKE: Windmolen's complaint itself: the talk must not pick up the Hardcore scene's framing, even at Hardcore.
      [Test]
      public void GIVEN_hardcore_WHEN_a_captor_sends_for_the_player_THEN_none_of_the_abuse_rules_come_with_it()
      {
         string prompt = Prompt(CaptiveSceneIntent.Bargain, AdultContentLevel.Hardcore);

         prompt.Should().Contain("YOU HAVE SENT FOR THEM TO TALK");
         prompt.Should().NotContain("The power in this encounter belongs to you entirely");
         prompt.Should().NotContain("BRUTE FORCE IS A TOOL OF TERROR");
      }

      // STAKE: the mod declares what is possible before the model speaks: an untaught way out cannot be promised.
      [Test]
      public void GIVEN_nothing_on_offer_WHEN_the_talk_opens_THEN_no_way_out_is_taught()
      {
         string prompt = Prompt(CaptiveSceneIntent.Bargain);

         prompt.Should().NotContain("type: ransom_player");
         prompt.Should().NotContain("type: parole_player");
         prompt.Should().NotContain("type: exchange_for_player");
         prompt.Should().NotContain("type: release_player");
      }

      // STAKE: the price is the game's: a figure the model invents is a figure the gold will not match.
      [Test]
      public void GIVEN_a_price_WHEN_the_talk_opens_THEN_the_model_names_that_sum()
      {
         string prompt = Prompt(CaptiveSceneIntent.Bargain, price: 2400);

         prompt.Should().Contain("2400 denars");
         prompt.Should().Contain("type: ransom_player");
      }

      // STAKE: the oath's terms are what the game holds the player to, so the model must state them as they are.
      [Test]
      public void GIVEN_an_oath_on_offer_WHEN_the_talk_opens_THEN_its_realm_and_term_are_stated()
      {
         string prompt = Prompt(CaptiveSceneIntent.Parole, paroleDays: 60);

         prompt.Should().Contain("not to attack Vlandia for 60 days");
         prompt.Should().Contain("type: parole_player");
      }

      // STAKE: the exchange must name a captive the player truly holds, or the model bargains over someone who is free.
      [Test]
      public void GIVEN_a_captive_to_exchange_WHEN_the_talk_opens_THEN_he_is_named()
      {
         string prompt = Prompt(CaptiveSceneIntent.TornFriend, exchange: "Erdurand");

         prompt.Should().Contain("The prisoner holds Erdurand");
         prompt.Should().Contain("type: exchange_for_player");
      }

      // STAKE: a persuadable captor keeps the plain release the Hardcore scenes already teach.
      [Test]
      public void GIVEN_a_captor_open_to_persuasion_WHEN_the_talk_opens_THEN_he_may_simply_let_them_go()
      {
         Prompt(CaptiveSceneIntent.Courtesy, persuasion: true).Should().Contain("type: release_player");
      }
   }
}

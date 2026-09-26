// Code written by Gabriel Mailhot, 25/09/2026.
// The envoy's own prompt (prisoner exchange, increment 3). fkasad (Nexus, 25/09/2026) asked that a lord wanting a
// captive back send a trusted agent to negotiate rather than a letter with Accept or Refuse; Gabriel ruled: always an
// envoy, never the lord in person, and anonymous. These tests pin what the envoy is told, because each line is a
// promise the bridge must be able to keep: he speaks for the lord and not as him, he may settle only the two trades
// the lord authorised and within their bounds, and a deal only spoken of must not read as done.

#region

using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class EnvoyPromptBuilderTests
   {
      private static EnvoyPromptInput Input(string exchange = null, int gold = 0)
         => new() {
            EnvoyName = "Envoy of Derthert", LordName = "Derthert", LordRealm = "Vlandia", PlayerName = "Arwa",
            WantedCaptiveName = "Lord Garios", WantedCaptiveStake = "who is Derthert's own nephew",
            RansomCeiling = 1800, ExchangeCaptiveName = exchange, ExchangeGoldToPlayer = gold
         };

      // Gabriel's ruling: never the lord in person. The model must not play Derthert, or the anonymous envoy is the
      // lord himself under another name.
      [Test]
      public void GIVEN_an_envoy_WHEN_his_prompt_is_built_THEN_he_speaks_for_the_lord_and_never_as_him()
      {
         string prompt = EnvoyPromptBuilder.Build(Input());

         prompt.Should().StartWith("YOU ARE ENVOY OF DERTHERT: AN ENVOY SENT BY DERTHERT OF VLANDIA");
         prompt.Should().Contain("You are not Derthert, and you never speak as Derthert");
         prompt.Should().Contain("You give no name of your own");
      }

      // The ransom ceiling is what the lord authorised; an envoy offering more would be promising coin nobody sent.
      [Test]
      public void GIVEN_a_ransom_ceiling_WHEN_the_prompt_is_built_THEN_the_envoy_is_bound_by_it()
      {
         EnvoyPromptBuilder.Build(Input()).Should().Contain("A RANSOM of up to 1800 denars for Lord Garios").And.Contain("never go above it");
      }

      // An exchange is only on the table when the lord holds someone to give: otherwise the envoy must not offer one
      // the bridge would then refuse.
      [Test]
      public void GIVEN_the_lord_holds_nobody_to_trade_WHEN_the_prompt_is_built_THEN_no_exchange_is_offered_or_taught()
      {
         string prompt = EnvoyPromptBuilder.Build(Input(exchange: null));

         prompt.Should().NotContain("AN EXCHANGE").And.NotContain("exchange_prisoners");
      }

      // With someone to trade, the envoy knows who, and what evens it, and the block names both captives the way
      // exchange_prisoners reads them (give: the player's captive, receive: the lord's).
      [Test]
      public void GIVEN_the_lord_holds_someone_to_trade_WHEN_the_prompt_is_built_THEN_the_exchange_and_its_block_are_taught()
      {
         string prompt = EnvoyPromptBuilder.Build(Input(exchange: "Borcha", gold: 400));

         prompt.Should().Contain("AN EXCHANGE: Derthert holds Borcha, and would trade Borcha for Lord Garios. Derthert would add 400 denars");
         prompt.Should().Contain("type: exchange_prisoners").And.Contain("give: Lord Garios").And.Contain("receive: Borcha");
      }

      // An envoy carries a narrow mandate; promising an alliance or a marriage would be a deed no bridge carries out,
      // which reads to the player as the model breaking its word.
      [Test]
      public void GIVEN_an_envoy_WHEN_the_prompt_is_built_THEN_he_may_promise_nothing_beyond_the_trade()
      {
         EnvoyPromptBuilder.Build(Input()).Should().Contain("You may promise nothing else: no alliance, no marriage, no land");
      }

      // The settlement moves the coin itself; a separate gold action would make one side pay twice.
      [Test]
      public void GIVEN_an_envoy_WHEN_the_prompt_is_built_THEN_he_never_moves_gold_on_the_side()
      {
         EnvoyPromptBuilder.Build(Input()).Should().Contain("never give_gold or take_gold");
         EnvoyPromptBuilder.AllowedActions.Should().BeEquivalentTo("buy_prisoner", "exchange_prisoners");
      }

      // The envoy opens the audience, since he asked for it; without the cue the first reply waits on the player.
      [Test]
      public void GIVEN_the_opening_turn_WHEN_the_prompt_is_built_THEN_the_envoy_states_his_errand()
      {
         EnvoyPromptBuilder.Build(new EnvoyPromptInput {
            EnvoyName = "Envoy of Derthert", LordName = "Derthert", PlayerName = "Arwa", WantedCaptiveName = "Lord Garios",
            RansomCeiling = 1800, IsOpener = true
         }).Should().Contain("state your errand");
      }
   }
}

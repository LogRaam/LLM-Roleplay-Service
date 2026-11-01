// Code written by Gabriel Mailhot, 01/11/2026.
// What these pin: that a character knows what things are worth and what it can pay BEFORE it names a sum.
//
// RaveN017Mod (Nexus, 01/11/2026): Rhagaea offered 20,000 denars, nothing happened, then she sent 1,000. Gabriel, same day:
// "Il faudrait que le modele connaisse la valeur des choses avant de proposer un montant", and the problem to settle is
// "l'incoherence entre ce que le LLM dit et ce que le joueur recoit". His standing rule: the mod declares what is possible
// before the model speaks, it never refutes it after. If these fail, a character promises a sum the game cannot pay.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class CoinInHandPromptTests
   {
      // STAKE: the report. Told what it holds and would give, a character has no reason to offer 20,000 from a gift.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_a_character_with_a_purse_WHEN_the_prompt_is_built_THEN_it_knows_what_it_holds_and_would_give(LeanPromptLevel level)
      {
         string prompt = Build(new EncounterContext {LeanLevel = level, Purse = 50000, GiftCeiling = 500});

         prompt.Should().Contain("your purse holds 50,000 denars").And.Contain("you would give 500 at the very most");
      }

      // STAKE: a hostile character still has a purse; it must not read as a figure it would hand over.
      [Test]
      public void GIVEN_a_character_who_would_give_nothing_WHEN_the_prompt_is_built_THEN_it_is_told_so()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, Purse = 50000, GiftCeiling = 0})
            .Should().Contain("you would give nothing");
      }

      // STAKE: a gift moves both figures, so they must not sit in the cached prefix (fkasad, 15/09/2026, the same failure).
      [Test]
      public void GIVEN_a_gift_during_the_talk_WHEN_the_next_turn_is_built_THEN_the_cached_prefix_is_unchanged()
      {
         string? before = PromptCacheSplit.StablePrefixOf(Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, Purse = 50000, GiftCeiling = 500}));

         PromptCacheSplit.StablePrefixOf(Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, Purse = 49500, GiftCeiling = 0})).Should().Be(before);
      }

      // STAKE: nothing says the sale runs through its own verb otherwise, and a sale improvised as a gift stops at the gift's ceiling.
      [Test]
      public void GIVEN_a_purse_WHEN_the_prompt_is_built_THEN_a_captive_goes_for_its_worth_and_not_as_a_gift()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, Purse = 50000, GiftCeiling = 500})
            .Should().Contain("goes for its worth through its own action");
      }

      // STAKE: the table is what lets a character name a fair sum; a full prompt reads the whole of it.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_it_is_built_THEN_the_character_reads_the_price_table()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, PriceReference = "[table]", PriceReferenceCompact = "[line]"})
            .Should().Contain("[table]").And.NotContain("[line]");
      }

      // STAKE: a small model pays for every character: it gets the line, never the table.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_it_is_built_THEN_the_character_reads_the_one_line_form()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Lean, PriceReference = "[table]", PriceReferenceCompact = "[line]"})
            .Should().Contain("[line]").And.NotContain("[table]");
      }

      // STAKE: the table does not change turn to turn: left out of the cache it would be paid for on every one.
      [Test]
      public void GIVEN_the_price_table_WHEN_the_prompt_is_built_THEN_it_sits_in_the_cached_prefix()
      {
         PromptCacheSplit.StablePrefixOf(Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, PriceReference = "[table]"})).Should().Contain("[table]");
      }

      // STAKE: 3chmidt's rule for a sale, now with its ceiling: a buyer never names what the game will not pay.
      [Test]
      public void GIVEN_an_item_held_out_WHEN_the_prompt_is_built_THEN_the_buyer_knows_the_most_it_fetches()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, ItemOnOffer = "a dagger", ItemOnOfferValue = 200, ItemOnOfferMost = 600})
            .Should().Contain("No buyer pays more than 600 for it");
      }

      // STAKE: a prompt that does not know the figures must not print a line about them.
      [Test]
      public void GIVEN_no_figures_WHEN_the_prompt_is_built_THEN_no_coin_line_is_written()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full}).Should().NotContain("YOUR COIN");
      }

      #region private

      private static string Build(EncounterContext context)
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"}, new WorldState {CurrentDay = 12}, context);

      #endregion
   }
}

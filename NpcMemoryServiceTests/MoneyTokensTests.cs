// Code written by Gabriel Mailhot, 01/11/2026.
// What these pin: that a sum the model writes as a token reaches every channel as the one figure the game set.
//
// RaveN017Mod (Nexus, 01/11/2026): Rhagaea offered 20,000 denars, then sent 1,000. Gabriel, same day: the LLM proposes with a
// placeholder and code fills it, and "le code fixe le montant qui est reinjecte ensuite dans la memoire du NPC". If these
// fail, a player reads braces, a memory keeps a different sum from the one spoken, or an action cannot read its amount.

#region

using System;
using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class MoneyTokensTests
   {
      private static readonly Dictionary<string, int> Table = new(StringComparer.OrdinalIgnoreCase) {
         ["gift"] = 500, ["fair:Monchug"] = 4900, ["most:Monchug"] = 14000
      };

      // STAKE: Gabriel's design: one fill, so what is spoken, remembered and paid cannot differ.
      [Test]
      public void GIVEN_a_token_in_every_channel_WHEN_the_reply_is_read_THEN_all_of_them_carry_the_same_sum()
      {
         string raw = "[DIALOGUE]I will pay {fair:Monchug} denars.[/DIALOGUE]\n[NARRATION]She counts out {fair:Monchug}.[/NARRATION]\n"
                    + "[EVENT]\ntype: agreement\nsummary: I offered {fair:Monchug} denars for Monchug.\n[/EVENT]\n"
                    + "[ACTION]\ntype: buy_prisoner\ntarget: Monchug\nprice: {fair:Monchug}\n[/ACTION]";

         ParsedResponse reply = new SectionResponseParser().Parse(MoneyTokens.Fill(raw, Table));

         reply.Dialogue.Should().Contain("4900 denars");
         reply.Narration.Should().Contain("4900");
         reply.NewEventData!.Summary.Should().Contain("4900 denars");
         reply.Actions[0].Parameters["price"].Should().Be("4900");
      }

      // STAKE: give_gold's amount must parse: a thousands separator would refuse the payment after the sum was spoken.
      [Test]
      public void GIVEN_a_large_sum_WHEN_it_is_filled_THEN_it_is_plain_digits()
      {
         MoneyTokens.Fill("amount: {most:Monchug}", Table).Should().Be("amount: 14000");
      }

      // STAKE: a model writes the name its own way; a change of case or a space must not lose the sum.
      [TestCase("{FAIR:monchug}")]
      [TestCase("{ fair : Monchug }")]
      public void GIVEN_a_token_written_loosely_WHEN_it_is_filled_THEN_the_sum_is_found(string token)
      {
         MoneyTokens.Fill(token, Table).Should().Be("4900");
      }

      // STAKE: a token the table does not hold must never reach the screen, and no figure may be invented for it.
      [Test]
      public void GIVEN_a_token_nobody_holds_WHEN_it_is_filled_THEN_no_figure_is_invented()
      {
         MoneyTokens.Fill("I offer {fair:Nobody}.", Table).Should().Be("I offer " + MoneyTokens.NoFigure + ".");
      }

      // STAKE: a path with no table (a letter, an envoy) still must not show braces: the parser clears them.
      [Test]
      public void GIVEN_a_token_where_no_table_exists_WHEN_the_reply_is_parsed_THEN_the_player_never_reads_braces()
      {
         new SectionResponseParser().Parse("[DIALOGUE]Take {gift}.[/DIALOGUE]").Dialogue.Should().Be("Take " + MoneyTokens.NoFigure + ".");
      }

      // STAKE: a sum the player named stays the model's to repeat, and ordinary braces in prose are not money.
      [Test]
      public void GIVEN_figures_and_other_braces_WHEN_filled_THEN_they_are_left_alone()
      {
         MoneyTokens.Fill("I accept 7000, {said she}.", Table).Should().Be("I accept 7000, {said she}.");
      }

      // STAKE: the model must see each figure to react to it, or it writes "a paltry" around a fortune (Gabriel's approval).
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_tokens_for_this_turn_WHEN_the_prompt_is_built_THEN_the_model_reads_each_with_its_figure(LeanPromptLevel level)
      {
         string prompt = Build(level, Table);

         prompt.Should().Contain("{gift} = 500").And.Contain("{fair:Monchug} = 4,900");
      }

      // STAKE: a sum moves within a talk; left in the cached prefix it would rewrite it on every gift.
      [Test]
      public void GIVEN_a_token_that_moved_WHEN_the_next_turn_is_built_THEN_the_cached_prefix_is_unchanged()
      {
         string? before = PromptCacheSplit.StablePrefixOf(Build(LeanPromptLevel.Full, Table));
         var after = new Dictionary<string, int>(Table, StringComparer.OrdinalIgnoreCase) {["gift"] = 0};

         PromptCacheSplit.StablePrefixOf(Build(LeanPromptLevel.Full, after)).Should().Be(before);
      }

      // STAKE: a letter has no fill on its road: it must not be asked for tokens.
      [Test]
      public void GIVEN_no_tokens_WHEN_the_prompt_is_built_THEN_the_model_is_not_asked_for_any()
      {
         Build(LeanPromptLevel.Full, null).Should().NotContain("{gift}");
      }

      // STAKE: a small model pays for every character on every turn: a single captive stays a few short lines.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_tokens_are_taught_THEN_they_cost_under_400_characters()
      {
         (Build(LeanPromptLevel.Lean, Table).Length - Build(LeanPromptLevel.Lean, null).Length).Should().BeLessThan(400);
      }

      #region private

      private static string Build(LeanPromptLevel level, IReadOnlyDictionary<string, int>? tokens)
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"}, new WorldState {CurrentDay = 12},
            new EncounterContext {LeanLevel = level, Purse = 50000, GiftCeiling = 500, MoneyTokens = tokens});

      #endregion
   }
}

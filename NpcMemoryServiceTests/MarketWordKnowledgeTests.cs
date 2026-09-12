// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 8. What these pin: that somebody who trades for a living knows their own
// market, and that they know it in a trader's terms rather than as a price list.
//
// Why it exists. Gabriel, 2026-09-12: "on va aussi modeliser le commerce pour les marchands." The audit had
// already named trade as the one category with NO representation at all, and a grep of EncounterContext
// confirmed it: nothing for trade, price, market, goods or caravan, the only match being CompanionAskingPrice,
// which is a hiring fee. So a town merchant, whose entire life is what things cost and where they come from,
// knew no more of it than a passing soldier - and asked what was dear this season, he would invent.
//
// If these fail, either a merchant has stopped knowing his own market, or he has started quoting denars a
// player can disprove by walking into it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class MarketWordKnowledgeTests
   {
      private static readonly MarketWordPack Pack = new();

      private static string Render(MarketWordFacts market, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {MarketWord = market}, lean);

         return sb.ToString();
      }

      private static MarketWordFacts Pravend()
         => new() {
            PlaceName = "Pravend",
            Produces = new List<string> {"wine", "olives"},
            Goods = new List<MarketGood> {
               new() {Name = "iron", Price = PriceBand.Dear},
               new() {Name = "grain", Price = PriceBand.Cheap},
               new() {Name = "hides", Price = PriceBand.Ordinary}
            }
         };

      // ── what a trader actually knows ─────────────────────────────────────

      // The most durable trade fact there is, and the one a merchant states without thinking: what the place
      // he lives in makes.
      [Test]
      public void GIVEN_a_merchant_of_a_wine_town_WHEN_he_speaks_THEN_he_knows_what_his_town_makes()
      {
         string said = Render(Pravend());

         said.Should().Contain("Pravend makes wine and olives");
         said.Should().Contain("not news to you");
      }

      // The question he would actually be asked, and the one he used to invent an answer to.
      [Test]
      public void GIVEN_iron_is_scarce_here_WHEN_he_speaks_THEN_he_says_so_rather_than_guessing()
      {
         Render(Pravend()).Should().Contain("iron is dear");
      }

      // Both directions matter: a trader who only ever reports scarcity is a trader nobody would buy from.
      [Test]
      public void GIVEN_grain_is_going_cheap_WHEN_he_speaks_THEN_that_is_worth_saying_too()
      {
         Render(Pravend()).Should().Contain("Grain is going cheap");
      }

      // A market where nothing stands out has no news in it. This pack is read in every conversation with
      // every merchant in the game, so an ordinary price must cost nothing.
      [Test]
      public void GIVEN_a_good_at_its_ordinary_worth_WHEN_rendered_THEN_no_words_are_spent_on_it()
      {
         Render(Pravend()).Should().NotContain("hides");
      }

      // ── the rule that keeps it from becoming a price list ────────────────

      // The sharpest version of "bands, not numbers" in the whole pillar: a denar figure is exactly what a
      // player can check by walking into the same market, and it is the number a model would most enjoy doing
      // arithmetic with.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_rendered_THEN_he_is_forbidden_to_name_a_price_in_denars()
      {
         string said = Render(Pravend());

         said.Should().Contain("name no figure in denars");
         said.Should().NotContainAny("0", "1", "2", "3", "4", "5", "6", "7", "8", "9");
      }

      // Compact keeps the facts and drops the manner, like every pack.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_the_market_survives_and_the_coaching_does_not()
      {
         string lean = Render(Pravend(), true);

         lean.Should().Contain("iron is dear").And.Contain("makes wine");
         lean.Should().NotContain("name no figure");
      }

      // A trader talks about what stands out, not about a ledger. Otherwise the cost of a merchant grows with
      // the size of his market.
      [Test]
      public void GIVEN_a_market_full_of_news_WHEN_rendered_THEN_only_what_stands_out_is_named()
      {
         var busy = new MarketWordFacts {
            PlaceName = "Pravend",
            Goods = Enumerable.Range(1, 7)
                              .Select(i => new MarketGood {Name = $"good{i}", Price = PriceBand.Dear})
                              .ToList()
         };

         string said = Render(busy);

         said.Should().Contain("good1").And.Contain("good3");
         said.Should().NotContain("good4");
      }

      // ── composition: this one CAN refuse, and that is the point ──────────

      // Unlike the packs everyone carries, this is a real rule about the world that can be false. A lord knows
      // his lands make wine and has no idea what wine fetched this week; knowing that is exactly what separates
      // a merchant from a man who buys things.
      [Test]
      public void GIVEN_whose_living_it_is_WHEN_composing_THEN_a_trader_carries_it_and_a_lord_does_not()
      {
         Pack.CarriedBy(new KnowledgeBearer {IsNotable = true, TradesForALiving = true}).Should().BeTrue();

         Pack.CarriedBy(new KnowledgeBearer {IsLord = true, HasHouse = true, HoldsASeat = true}).Should().BeFalse();
         Pack.CarriedBy(new KnowledgeBearer {IsNotable = true}).Should().BeFalse();
         Pack.CarriedBy(null).Should().BeFalse();
      }

      // A market always has something to say, so an empty reading on somebody whose living this is means
      // nobody asked the engine - the fkasad class of fault, in a new profession.
      [Test]
      public void GIVEN_a_trader_with_nothing_supplied_WHEN_swept_THEN_it_counts_as_a_fault()
      {
         Pack.RequiresContent.Should().BeTrue();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {MarketWord = new MarketWordFacts()}).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {MarketWord = Pravend()}).Should().BeTrue();
      }

      // And the discipline that closed the "what the player carries" candidate without a line being written:
      // seat_standing already tells a notable how his town prospers and whether its roads are safe. Saying it
      // again here would be a second source for one fact.
      [Test]
      public void GIVEN_this_pack_WHEN_read_THEN_it_never_repeats_what_the_seat_pack_already_says()
      {
         string said = Render(Pravend()).ToLowerInvariant();

         said.Should().NotContain("prosper").And.NotContain("granar").And.NotContain("garrison");
         said.Should().NotContain("roads");
      }
   }
}

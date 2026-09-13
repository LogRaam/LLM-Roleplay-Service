// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 12. What these pin: that somebody knows what they own, and does not know
// how it is doing.
//
// The last gap the role audit named. Gabriel raised caravans unprompted - "les caravanes, etc." - and checking
// found Hero.OwnedCaravans and Hero.OwnedWorkshops reaching the context nowhere at all.
//
// WHY NOT PART OF market_word, which was the obvious home: a trader's market is where he STANDS, his caravans
// are where he is NOT. And the carry rules refuse different people, which is the real test of whether two
// packs are one. A lord who owns three caravans is not a merchant and must never be handed the price of
// grain; a merchant with no caravan has a market and no ventures.
//
// THE INVENTION IT HAS TO CLOSE is not the ventures but their FORTUNES - the granary lesson in a merchant's
// coat. A man who knows he owns three caravans and is asked how they are doing will make up a profit unless
// somebody tells him he would have to wait for word.
//
// If these fail, either a man has forgotten his own workshop, or he has started quoting its takings.

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
   public sealed class OwnVenturesKnowledgeTests
   {
      private static readonly OwnVenturesPack Pack = new();

      private static KnowledgeBearer Owner() => new() {IsLord = true, HasHouse = true, OwnsVentures = true};

      private static string Render(OwnVenturesFacts mine, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {OwnVentures = mine, Bearer = Owner()}, lean);

         return sb.ToString();
      }

      // A man knows how many caravans he owns the way he knows how many children he has. This is one of the
      // few places the pillar states a NUMBER, because the rule was never "never be precise" - it was "never
      // invent what you have not counted".
      [Test]
      public void GIVEN_caravans_on_the_road_WHEN_he_speaks_THEN_he_knows_how_many_are_his()
      {
         Render(new OwnVenturesFacts {Caravans = 3}).Should().Contain("3 caravans of your own");
         Render(new OwnVenturesFacts {Caravans = 1}).Should().Contain("a caravan of your own");
      }

      // A workshop is a place and a trade, and both are how its owner would name it.
      [Test]
      public void GIVEN_a_workshop_WHEN_he_speaks_THEN_he_names_the_trade_and_the_town()
      {
         Render(new OwnVenturesFacts {
            Workshops = new List<OwnedWorkshop> {new() {Trade = "tannery", PlaceName = "Pravend"}}
         }).Should().Contain("a tannery in Pravend");
      }

      // THE INVENTION THIS PACK EXISTS TO CLOSE. Owning it is his; how it FARES is word he is waiting on, and
      // without this line he invents a profit - the granary lesson in a merchant's coat.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_rendered_THEN_he_owns_them_and_does_not_know_their_takings()
      {
         string said = Render(new OwnVenturesFacts {Caravans = 2});

         said.Should().Contain("What they are EARNING");
         said.Should().Contain("waiting on word");
      }

      // Owning is not news, the same discipline that stops a governor greeting his own post as a revelation.
      [Test]
      public void GIVEN_his_own_ventures_WHEN_rendered_THEN_they_are_never_received_as_news()
      {
         Render(new OwnVenturesFacts {Caravans = 1}).Should().Contain("not news to you");
      }

      // A man with nine workshops must not read out a ledger, and must not sound like a man with three.
      [Test]
      public void GIVEN_more_workshops_than_anyone_would_list_WHEN_rendered_THEN_the_rest_are_still_counted()
      {
         var many = Enumerable.Range(1, 6)
                              .Select(i => new OwnedWorkshop {Trade = "smithy", PlaceName = "Town" + i})
                              .ToList();

         string said = Render(new OwnVenturesFacts {Workshops = many});

         said.Should().Contain("Town1").And.Contain("Town3");
         said.Should().NotContain("Town4");
         said.Should().Contain("3 others");
      }

      // Compact keeps what he owns and drops the coaching, like every pack.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_the_ventures_survive_and_the_caution_does_not()
      {
         string lean = Render(new OwnVenturesFacts {Caravans = 2}, true);

         lean.Should().Contain("2 caravans");
         lean.Should().NotContain("What they are EARNING");
      }

      // ── composition: two packs because two rules refuse different people ──

      // The test of whether this should have been part of market_word, made mechanical: a lord may own
      // caravans without being a merchant, and a merchant may have a market and no ventures.
      [Test]
      public void GIVEN_an_owner_and_a_trader_WHEN_composing_THEN_neither_pack_answers_for_the_other()
      {
         var lordWithCaravans = new KnowledgeBearer {IsLord = true, HasHouse = true, OwnsVentures = true};
         var merchantWithNone = new KnowledgeBearer {IsNotable = true, TradesForALiving = true};

         Pack.CarriedBy(lordWithCaravans).Should().BeTrue();
         new MarketWordPack().CarriedBy(lordWithCaravans).Should().BeFalse();

         Pack.CarriedBy(merchantWithNone).Should().BeFalse();
         new MarketWordPack().CarriedBy(merchantWithNone).Should().BeTrue();
      }

      // Owning nothing is the common case and must cost nothing.
      [Test]
      public void GIVEN_somebody_who_owns_nothing_WHEN_composing_THEN_the_pack_never_reaches_them()
      {
         Pack.CarriedBy(new KnowledgeBearer {IsLord = true, HasHouse = true}).Should().BeFalse();
         Pack.CarriedBy(null).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {OwnVentures = new OwnVenturesFacts()}).Should().BeFalse();
      }
   }
}

// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 7: what a man in a cell no longer knows.
//
// Gabriel ruled on 2026-09-11 that knowledge is CURRENT, and gave the reason from the world rather than from
// the code: "considerant qu'il y a un systeme de courrier, je pencherais pour que le Seigneur soit au courant
// de ce qui se passe." Couriers ride, so news travels, so a lord on campaign hears how his house fares.
//
// He ruled the corollary in on 2026-09-12: "le captif ne porte pas le pack." The same reasoning run backwards
// - a man who receives no couriers hears nothing.
//
// THE PART THAT MATTERS ARCHITECTURALLY: this does not make knowledge stale, it makes it ABSENT. A captive
// does not carry a months-old copy of his house's ledger; he does not carry it at all. That keeps staleness
// out of the design entirely, which was the whole point of the current-knowledge ruling, and it is why no pack
// anywhere needs a freshness axis.
//
// And the line it runs along is the one already drawn when house_means was split out of house_standing:
// DURABLE things survive the cell, TODAY'S things do not.
//
// If these fail, a prisoner is either reporting this morning's grain stores from inside a dungeon, or has
// forgotten where his own family's lands are.
//
// 16/11/2026 (Schmidt, Discord: "even if they were locked up the same day, they have no idea what happened outside";
// Gabriel: "oui vas-y"): the news stops at the cell door rather than vanishing. When the host vouches that it stopped
// the news on the capture day (KnowledgeBearer.NewsStoppedOnCaptureDay), the realm news and the war line come back,
// told as what he last heard. Live state (seat, means, streets) stays cut, so still no pack keeps a stale copy.

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
   public sealed class CutOffKnowledgeTests
   {
      private static KnowledgeBearer CaptiveLord()
         => new() {IsLord = true, HasHouse = true, HoldsASeat = true, IsCutOff = true};

      private static KnowledgeBearer FreeLord()
         => new() {IsLord = true, HasHouse = true, HoldsASeat = true};

      // ── what the cell takes away ─────────────────────────────────────────

      // The sharpest case, and the one that would read as absurd in play: a man in a dungeon reporting how his
      // granaries stand this morning. He has not seen the place in months and nobody is riding to tell him.
      [Test]
      public void GIVEN_a_lord_in_a_cell_WHEN_composing_THEN_he_no_longer_answers_for_his_seat()
      {
         new SeatStandingPack().CarriedBy(CaptiveLord()).Should().BeFalse();
         new SeatStandingPack().CarriedBy(FreeLord()).Should().BeTrue();
      }

      // Coin, muster and court standing are all TODAY'S, which is exactly why they were split into their own
      // pack in the first place. The cell is what that split was anticipating.
      [Test]
      public void GIVEN_a_lord_in_a_cell_WHEN_composing_THEN_he_does_not_know_what_his_house_can_bring_to_bear()
      {
         new HouseMeansPack().CarriedBy(CaptiveLord()).Should().BeFalse();
         new HouseMeansPack().CarriedBy(FreeLord()).Should().BeTrue();
      }

      // ── what the cell leaves him ─────────────────────────────────────────

      // Where a house holds land does not change from one month to the next, so a captive still knows it. The
      // durable half survives; only the news is gone.
      [Test]
      public void GIVEN_a_lord_in_a_cell_WHEN_composing_THEN_he_still_knows_where_his_family_holds_land()
      {
         new HouseStandingPack().CarriedBy(CaptiveLord()).Should().BeTrue();
      }

      // And within that pack the same line runs again: the lands stay, the wars go. Who his realm is fighting
      // THIS WEEK is precisely what a courier would have had to bring him.
      [Test]
      public void GIVEN_a_captive_WHEN_his_house_is_described_THEN_the_lands_remain_and_the_wars_do_not()
      {
         var facts = new HouseStandingFacts {
            HouseName = "Leonipardes", Fiefs = new List<string> {"Hargendorf"},
            AtWarWith = new List<string> {"Sturgia"}
         };

         string captive = Render(facts, CaptiveLord());
         string free = Render(facts, FreeLord());

         captive.Should().Contain("Hargendorf");
         captive.Should().NotContain("Sturgia");

         free.Should().Contain("Hargendorf").And.Contain("Sturgia");
      }

      // He is standing in the weather and in his own body whatever else is true of him, and his own life is
      // his own. A cell takes news, not senses and not memory.
      [Test]
      public void GIVEN_a_lord_in_a_cell_WHEN_composing_THEN_the_room_his_body_and_his_own_people_remain()
      {
         new HereAndNowPack().CarriedBy(CaptiveLord()).Should().BeTrue();
         new OwnBodyPack().CarriedBy(CaptiveLord()).Should().BeTrue();
         new PersonalBondsPack().CarriedBy(CaptiveLord()).Should().BeTrue();
      }

      // ── the shape of the ruling ──────────────────────────────────────────

      // The architectural point, asserted so it cannot quietly be undone: being cut off REMOVES packs, it never
      // hands over an out-of-date one. No pack has a freshness axis and none should acquire one.
      [Test]
      public void GIVEN_the_cut_off_rule_WHEN_composing_THEN_it_removes_knowledge_rather_than_ageing_it()
      {
         IReadOnlyList<KnowledgePack> free = NpcKnowledgeFactory.For(FreeLord());
         IReadOnlyList<KnowledgePack> captive = NpcKnowledgeFactory.For(CaptiveLord());

         captive.Count.Should().BeLessThan(free.Count);
         captive.Select(p => p.Name).Should().BeSubsetOf(free.Select(p => p.Name));
      }

      // A free lord loses nothing to this change. Stated because a rule that quietly narrowed the ordinary case
      // would be a regression wearing a ruling's clothes.
      [Test]
      public void GIVEN_a_lord_who_is_not_held_WHEN_composing_THEN_nothing_he_had_before_is_taken_away()
      {
         NpcKnowledgeFactory.For(FreeLord()).Select(p => p.Name)
                            .Should().Contain(new[] {"seat_standing", "house_means", "house_standing"});
      }

      // THE AUDIT'S FINDING (15/09/2026). house_means and seat_standing honoured the cell from the day the
      // flag existed; realm_news and underworld never looked at it, and nothing here pinned them. So a man in
      // a dungeon was being handed the CURRENT fortunes of his realm — which is the whole of what this rule
      // exists to stop, since reach is exactly what a cell takes away.
      [Test]
      public void GIVEN_a_captive_WHEN_the_realm_news_pack_is_weighed_THEN_no_word_reaches_the_cell()
      {
         new RealmNewsPack().DepthFor(CaptiveLord()).Should().Be(KnowledgeDepth.None);
         new RealmNewsPack().DepthFor(FreeLord()).Should().NotBe(KnowledgeDepth.None,
            "a free lord keeps couriers and a court; the rule is about the cell, not about lords");
      }

      // The quieter half of the same finding: a crime rating is CURRENT standing with the law and the
      // streets, and a captive hears neither. What he knew before the door shut is the ordinary history's to
      // carry, not this pack's.
      [Test]
      public void GIVEN_a_captive_WHEN_the_underworld_pack_is_weighed_THEN_he_does_not_know_who_is_wanted_today()
      {
         new UnderworldPack().DepthFor(CaptiveLord()).Should().Be(KnowledgeDepth.None);
         new UnderworldPack().DepthFor(FreeLord()).Should().NotBe(KnowledgeDepth.None);
      }

      // THE RULE AS A RULE, rather than four packs checked one by one. Every pack whose subject is REACH —
      // what a person hears from beyond the room they are shut in — must go silent in a cell. Written this
      // way because the defect was not that one pack was wrong; it was that nothing stated the rule, so each
      // new pack had to rediscover it, and two did not.
      [Test]
      public void GIVEN_a_captive_WHEN_every_pack_about_reach_is_weighed_THEN_all_of_them_go_silent()
      {
         var aboutReach = new KnowledgePack[] {
            new RealmNewsPack(), new UnderworldPack(), new HouseMeansPack(), new SeatStandingPack()
         };

         foreach (KnowledgePack pack in aboutReach)
            pack.DepthFor(CaptiveLord()).Should().Be(KnowledgeDepth.None,
               $"{pack.Name} reports what a character HEARS from outside, and a cell is where hearing stops");
      }

      // ── news stops at the cell door (16/11/2026) ──────────────────────────

      private static KnowledgeBearer CaptiveLordWithNewsOfHisCaptureDay()
         => new() {IsLord = true, HasHouse = true, HoldsASeat = true, IsCutOff = true, NewsStoppedOnCaptureDay = true};

      // STAKE: Schmidt's lord, taken this morning, had never heard of the war he was taken in. With his news stopped on
      // the day he was taken, he hears at the reach his station had, and it is told as what he last heard.
      [Test]
      public void GIVEN_a_captive_whose_news_stops_on_his_capture_day_WHEN_the_realm_news_is_rendered_THEN_he_tells_it_as_what_he_last_heard()
      {
         new RealmNewsPack().DepthFor(CaptiveLordWithNewsOfHisCaptureDay()).Should().Be(KnowledgeDepth.Knowing);

         var sb = new StringBuilder();
         new RealmNewsPack().Render(sb, new EncounterContext {
            Bearer = CaptiveLordWithNewsOfHisCaptureDay(),
            RealmNews = new RealmNewsFacts {Headline = "- Vlandia declared war on Sturgia.", Tidings = new List<Tiding> {new() {Text = "Pravend was besieged"}}}
         }, false);

         string text = sb.ToString();
         text.Should().Contain("WHEN YOU WERE TAKEN").And.Contain("Vlandia declared war on Sturgia").And.Contain("Pravend was besieged");
         text.Should().NotContain("great news of the moment");
      }

      // STAKE: the wars he names are those of the day he was taken, said as such, so he does not speak of them as today's.
      [Test]
      public void GIVEN_a_captive_whose_news_stops_on_his_capture_day_WHEN_his_house_is_described_THEN_its_wars_are_those_of_that_day()
      {
         var facts = new HouseStandingFacts {HouseName = "Leonipardes", Fiefs = new List<string> {"Hargendorf"}, AtWarWith = new List<string> {"Sturgia"}};

         string captive = Render(facts, CaptiveLordWithNewsOfHisCaptureDay());

         captive.Should().Contain("When you were taken").And.Contain("Sturgia").And.Contain("no word since");
      }

      // STAKE: what nobody keeps a copy of stays out of a cell: his seat's state, his house's means, the streets.
      [Test]
      public void GIVEN_a_captive_whose_news_stops_on_his_capture_day_WHEN_live_state_packs_are_weighed_THEN_they_stay_silent()
      {
         foreach (KnowledgePack pack in new KnowledgePack[] {new UnderworldPack(), new HouseMeansPack(), new SeatStandingPack()})
            pack.DepthFor(CaptiveLordWithNewsOfHisCaptureDay()).Should().Be(KnowledgeDepth.None, pack.Name);
      }

      #region private

      private static string Render(HouseStandingFacts facts, KnowledgeBearer bearer)
      {
         var sb = new StringBuilder();
         new HouseStandingPack().Render(sb, new EncounterContext {HouseStanding = facts, Bearer = bearer}, false);

         return sb.ToString();
      }

      #endregion
   }
}

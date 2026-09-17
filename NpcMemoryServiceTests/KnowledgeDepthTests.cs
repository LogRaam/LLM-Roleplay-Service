// Code written by Gabriel Mailhot, 12/09/2026.
// What these pin: that knowing a subject is a matter of DEGREE, and that a shallower degree means fewer facts
// rather than vaguer ones.
//
// Gabriel, 2026-09-12: "un pack pourrait avoir des niveaux requis de connaissance pour un sujet. Un niveau
// peu, normal, connaissant, très connaissant permettrait de filtrer ce qu'on envoie du pack au LLM. Donc un
// habitant de village pourrait être peu connaissant du reste du monde, mais un Lord pourrait être très
// connaissant."
//
// The turn that made it fit without bending anything: depth belongs to the PAIR of a person and a SUBJECT, not
// to a person. A villager is slight on the realm and deep on his own village; a lord is the reverse about the
// price of grain. A pack already IS a subject, so the question has a natural home.
//
// THE CONSTRAINT THAT KEEPS IT HONEST, and it is the whole pillar's lesson applied to its newest axis: a
// shallower depth must render FEWER FACTS, never vaguer ones. "You know a little about the wars" is an
// invitation to invent the rest - which is exactly what a governor did with a granary nobody had described to
// him. Every level says specific things; the shallow ones just say less.
//
// If these fail, either the composition rule and the depth have drifted apart, or a shallow level has started
// gesturing at knowledge instead of withholding it.

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
   public sealed class KnowledgeDepthTests
   {
      // ONE SOURCE OF TRUTH. Carrying a pack is derived from the depth rather than answered separately: two
      // predicates that must agree are two predicates that will one day disagree, which is precisely the fault
      // that let "he would know this" and "somebody told him" drift apart in the first place.
      [Test]
      public void GIVEN_any_pack_WHEN_asked_both_questions_THEN_carrying_it_is_simply_knowing_it_at_all()
      {
         var people = new[] {
            new KnowledgeBearer(),
            new KnowledgeBearer {IsLord = true, HasHouse = true, HoldsASeat = true},
            new KnowledgeBearer {IsNotable = true, HoldsASeat = true, TradesForALiving = true},
            new KnowledgeBearer {IsLord = true, HasHouse = true, HoldsASeat = true, IsCutOff = true}
         };

         foreach (KnowledgePack pack in NpcKnowledgeFactory.All)
         foreach (KnowledgeBearer person in people)
            pack.CarriedBy(person)
                .Should().Be(pack.DepthFor(person) != KnowledgeDepth.None, pack.Name);
      }

      // Nobody knows a subject they do not have. Stated so a future pack cannot return a depth for somebody it
      // then refuses to render for.
      [Test]
      public void GIVEN_somebody_a_pack_refuses_WHEN_asked_how_well_they_know_it_THEN_the_answer_is_not_at_all()
      {
         new MarketWordPack().DepthFor(new KnowledgeBearer {IsLord = true}).Should().Be(KnowledgeDepth.None);
         new HouseMeansPack().DepthFor(new KnowledgeBearer()).Should().Be(KnowledgeDepth.None);

         foreach (KnowledgePack pack in NpcKnowledgeFactory.All)
            pack.DepthFor(null).Should().Be(KnowledgeDepth.None, pack.Name);
      }

      // ── the first real distinction, and it is Gabriel's example turned around ──

      // A headman can tell you what is in the granary this week because he walks past it. His count, three
      // towns away, knows how the place FARES and would have to send for the tally - which is exactly the
      // answer the bench taught a governor to give about a garrison.
      [Test]
      public void GIVEN_a_holder_who_lives_elsewhere_WHEN_he_speaks_of_his_town_THEN_he_does_not_know_this_weeks_grain()
      {
         string resident = Render(SeatRole.Notable);
         string absentee = Render(SeatRole.Owner);

         resident.Should().Contain("granaries are thin");
         absentee.Should().NotContain("granaries");
      }

      // And the governor is a RESIDENT, whatever his rank. Asserted because the first draft of this rule read
      // depth off the bearer, where a governor and an absentee owner are indistinguishable - both merely "hold
      // a seat" - and it wrongly demoted the very character the pillar was built for.
      [Test]
      public void GIVEN_a_governor_WHEN_he_speaks_of_the_town_he_administers_THEN_he_knows_it_as_a_resident_does()
      {
         Render(SeatRole.Governor).Should().Contain("granaries are thin");
      }

      // THE CONSTRAINT. What the absentee loses is a FACT, not a level of confidence: he is told nothing about
      // the granary rather than told vaguely about it. A hedge would invite him to fill the gap, which is the
      // failure this whole pillar exists to close.
      [Test]
      public void GIVEN_the_shallower_view_WHEN_rendered_THEN_it_withholds_the_fact_rather_than_blurring_it()
      {
         string absentee = Render(SeatRole.Owner).ToLowerInvariant();

         absentee.Should().NotContain("roughly").And.NotContain("something of")
                 .And.NotContain("vaguely").And.NotContain("more or less");

         // and what he DOES know is still stated flatly
         absentee.Should().Contain("garrison is under strength");
      }

      // ── the axis as a whole ──────────────────────────────────────────────

      // Everyone knows their own body and their own people better than anyone else does, and nobody has to
      // rank that: it is the top of the scale by definition.
      [Test]
      public void GIVEN_what_is_most_intimately_theirs_WHEN_asked_THEN_nobody_knows_it_better()
      {
         var anyone = new KnowledgeBearer();

         new OwnBodyPack().DepthFor(anyone).Should().Be(KnowledgeDepth.Deep);
         new PersonalBondsPack().DepthFor(anyone).Should().Be(KnowledgeDepth.Deep);
      }

      // A trader's market is his life's work; a lord's house is his business but not his craft. The scale has
      // to be able to say that, or it is a boolean with extra words.
      [Test]
      public void GIVEN_two_subjects_of_one_person_WHEN_compared_THEN_the_scale_actually_distinguishes_them()
      {
         var merchant = new KnowledgeBearer {IsNotable = true, HoldsASeat = true, TradesForALiving = true};
         var lord = new KnowledgeBearer {IsLord = true, HasHouse = true, HoldsASeat = true};

         new MarketWordPack().DepthFor(merchant).Should().Be(KnowledgeDepth.Deep);
         new HouseStandingPack().DepthFor(lord).Should().Be(KnowledgeDepth.Knowing);

         // AIMED AT THE RENDER, not at DepthFor. The rule is real and is honoured — a place somebody LIVES in
         // is described more finely than one they merely hold — but seat_standing decides that per SEAT, in
         // LivesThere, and its DepthFor magnitude is read by nothing at all (only CarriedBy, which asks None
         // or not, plus realm_news and underworld reading their OWN). Asserting the unread number pinned the
         // implementation: it would have gone red for a change no player could notice, and stayed green for
         // one they would (audit, 15/09/2026, which reported the two as rival authorities).
         Render(SeatRole.Notable).Length
               .Should().BeGreaterThan(Render(SeatRole.Owner).Length,
                                       "somebody who lives in a place knows it better than its absentee holder");
      }

      #region private

      private static string Render(SeatRole role)
      {
         var sb = new StringBuilder();
         new SeatStandingPack().Render(sb, new EncounterContext {
            SeatStanding = new SeatStandingFacts {
               Seats = new List<SeatFacts> {
                  new() {
                     PlaceName = "Hargendorf", Role = role, Kind = SettlementKind.Town,
                     FoodStores = FoodBand.Thin, FoodDirection = FoodTrend.Falling,
                     Garrison = GarrisonBand.UnderStrength, Loyalty = LoyaltyBand.Restless
                  }
               }
            }
         }, false);

         return sb.ToString();
      }

      #endregion
   }
}

// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 4. What these pin: that a character who answers for a place knows how that
// place is actually doing, and that he says so in the terms he was given and invents nothing finer.
//
// Why it exists, and the source of every expectation below is a transcript rather than the code. The first live
// cr.memory_bench run (2026-09-12) passed all six probes, and the two inventions worth chasing were inside the
// PASSES. Given two fief names and nothing else, the character volunteered "Pravend is the larger, with decent
// trade flowing through it, though the roads have grown less certain", and then a harvest, grain lost to rot in
// the northern fields, granary stores sufficient for winter, and a garrison "at strength". None of it was in
// her prompt.
//
// So the frontier of ignorance is a slope, not a wall: it holds where the character has no business knowing
// (she refused the player's purse outright) and gives way exactly where he obviously WOULD know and was never
// told. And these particular inventions are catchable: the game HAS these numbers, so a player can open the
// town screen and find the lie.
//
// If these fail, either a governor has stopped being told how his own town fares, or he has started quoting
// figures he cannot have counted.

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
   public sealed class SeatStandingKnowledgeTests
   {
      private static readonly SeatStandingPack Pack = new();

      private static string Render(bool lean, params SeatFacts[] seats)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {
            SeatStanding = new SeatStandingFacts {Seats = new List<SeatFacts>(seats)}
         }, lean);

         return sb.ToString();
      }

      private static string Render(params SeatFacts[] seats) => Render(false, seats);

      /// <summary>The seat fkasad's governor actually holds, as the engine would read it.</summary>
      private static SeatFacts Hargendorf(SeatRole role = SeatRole.Governor)
         => new() {
            PlaceName = "Hargendorf", Role = role, Kind = SettlementKind.Town, IsPlayerHolding = true,
            Prosperity = ProsperityBand.Comfortable, FoodStores = FoodBand.Thin, FoodDirection = FoodTrend.Falling,
            Garrison = GarrisonBand.UnderStrength, Loyalty = LoyaltyBand.Restless, Security = SecurityBand.Uneasy
         };

      // ── the inventions, one test each ────────────────────────────────────

      // "the granaries hold sufficient stores to see us through winter" - invented, and wrong: they were thin
      // and falling. A player who checks the town screen catches this one in two clicks.
      [Test]
      public void GIVEN_a_thin_and_emptying_granary_WHEN_he_speaks_of_his_seat_THEN_that_is_what_he_says()
      {
         string said = Render(Hargendorf());

         said.Should().Contain("granaries are thin").And.Contain("emptying");
      }

      // "The garrison is at strength" - invented, on a garrison that was under it. Both halves of this matter:
      // the true reading reaches him, and the false one is contradicted rather than merely absent.
      [Test]
      public void GIVEN_a_garrison_under_strength_WHEN_he_speaks_of_his_seat_THEN_he_does_not_call_it_at_strength()
      {
         string said = Render(Hargendorf());

         said.Should().Contain("garrison is under strength");
         said.Should().NotContain("garrison is strong");
      }

      // "the roads have grown less certain since the wars began" - she happened to be right by luck. Security
      // is a live engine reading, so it can be right on purpose.
      [Test]
      public void GIVEN_uneasy_country_WHEN_he_speaks_of_his_seat_THEN_the_roads_are_named_as_uncertain()
      {
         Render(Hargendorf()).Should().Contain("roads around it are no longer certain");
         Render(new SeatFacts {
            PlaceName = "Pravend", Role = SeatRole.Owner, Kind = SettlementKind.Town, Security = SecurityBand.Settled
         }).Should().Contain("roads around it are safe");
      }

      // "The people are weary... But they trust this house" - invented too, on a town whose people were
      // restless. Temper is the reading a governor would actually lead with.
      [Test]
      public void GIVEN_restless_people_WHEN_he_speaks_of_his_seat_THEN_he_is_not_told_they_adore_him()
      {
         string said = Render(Hargendorf());

         said.Should().Contain("people are restless");
         said.Should().NotContain("devoted");
      }

      // "Pravend is the larger, with decent trade flowing through it." Prosperity has no natural ceiling, so it
      // is said as a lord would say it - against other places of its kind - rather than as a score.
      [Test]
      public void GIVEN_a_wealthy_town_WHEN_he_speaks_of_it_THEN_it_is_placed_against_others_of_its_kind()
      {
         Render(new SeatFacts {
            PlaceName = "Pravend", Role = SeatRole.Owner, Kind = SettlementKind.Town, Prosperity = ProsperityBand.Rich
         }).Should().Contain("among the wealthiest");

         Render(new SeatFacts {
            PlaceName = "Pravend", Role = SeatRole.Owner, Kind = SettlementKind.Town, Prosperity = ProsperityBand.Poor
         }).Should().Contain("among the poorer");
      }

      // ── the rule that keeps the pack from becoming a new source of invention ──

      // THE POINT OF THE INCREMENT. Bands were chosen over figures deliberately (a number invites the model to
      // do arithmetic with it, and it is the digits a player can check). A pack that itself printed a number
      // would hand back the very failure it was built to close, so the whole rendering is digit-free.
      [Test]
      public void GIVEN_anything_at_all_WHEN_rendered_THEN_not_one_figure_appears_anywhere_in_it()
      {
         string said = Render(Hargendorf(), new SeatFacts {
            PlaceName = "Pravend", Role = SeatRole.Owner, Kind = SettlementKind.Castle,
            Prosperity = ProsperityBand.Rich, FoodStores = FoodBand.Full, FoodDirection = FoodTrend.Rising,
            Garrison = GarrisonBand.Strong, Loyalty = LoyaltyBand.Devoted, Security = SecurityBand.Settled,
            IsUnderSiege = true
         });

         said.Should().NotBeEmpty();
         said.Should().NotContainAny("0", "1", "2", "3", "4", "5", "6", "7", "8", "9");
      }

      // A prohibition obeyed by silence would be its own regression: the purse probe read well because she had
      // somewhere to GO ("You keep your own purse, not I"), not because she stopped talking. So he is told both
      // what he may not do and what to do instead.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_rendered_THEN_he_is_forbidden_figures_and_handed_a_way_to_refuse_them()
      {
         string said = Render(Hargendorf());

         said.Should().Contain("Never give a figure");
         said.Should().Contain("steward");
      }

      // The other half of the same rule, and the one the bench actually caught: it is not only numbers he
      // invents, it is circumstance ("grain lost to rot in the northern fields").
      [Test]
      public void GIVEN_the_full_prompt_WHEN_rendered_THEN_he_is_told_not_to_invent_detail_beyond_what_he_was_given()
      {
         Render(Hargendorf()).Should().Contain("never invent a detail");
      }

      // And the case that matters most for a pack read in every landed conversation: knowing the NAME of a
      // place is not knowing how it fares. Saying "You govern Hargendorf." and stopping is an invitation to
      // fill the silence, which is precisely how this bug happened the first time.
      [Test]
      public void GIVEN_a_place_with_nothing_read_about_it_WHEN_rendered_THEN_the_pack_stays_silent()
      {
         Render(new SeatFacts {PlaceName = "Hargendorf", Role = SeatRole.Governor, Kind = SettlementKind.Town})
            .Should().BeEmpty();
      }

      // ── the room comes before the ledger ─────────────────────────────────

      // A character who opens with the harvest while an army is at his walls has not understood the room. The
      // urgent readings come first in the sentence, and Lean keeps ONLY them.
      [Test]
      public void GIVEN_a_siege_WHEN_he_speaks_of_his_seat_THEN_the_army_at_the_walls_comes_before_the_granary()
      {
         var besieged = new SeatFacts {
            PlaceName = "Hargendorf", Role = SeatRole.Governor, Kind = SettlementKind.Town,
            FoodStores = FoodBand.Thin, IsUnderSiege = true
         };

         string said = Render(besieged);

         said.IndexOf("under siege").Should().BeLessThan(said.IndexOf("granaries"));
      }

      // A sacked village that has not recovered is the state the escort quest's village is in, so it has to be
      // sayable by the headman who lives there.
      [Test]
      public void GIVEN_a_village_that_was_sacked_WHEN_its_headman_speaks_THEN_he_knows_it_has_not_recovered()
      {
         string said = Render(new SeatFacts {
            PlaceName = "Zeonica", Role = SeatRole.Notable, Kind = SettlementKind.Village, IsLooted = true
         });

         said.Should().Contain("sacked").And.Contain("not recovered");
         said.Should().Contain("lived there all your life");
      }

      // A village keeps no garrison. Telling its headman it has a skeleton one would be a fact the player can
      // disprove by looking - the exact class of failure this pack exists to close, reintroduced by the cure.
      [Test]
      public void GIVEN_a_village_WHEN_rendered_THEN_nothing_is_said_about_a_garrison_it_does_not_have()
      {
         // The sentence about the village, not the closing rule below it - which names "garrison" precisely
         // because it is one of the things he must never put a number to.
         string aboutTheVillage = Render(new SeatFacts {
            PlaceName = "Zeonica", Role = SeatRole.Notable, Kind = SettlementKind.Village,
            Garrison = GarrisonBand.Skeleton, Loyalty = LoyaltyBand.Content
         }).Split('\n')[0];

         aboutTheVillage.Should().Contain("Zeonica").And.Contain("content enough");
         aboutTheVillage.Should().NotContain("garrison");
      }

      // ── cost: this is read in every conversation with a landed character ──

      // A great lord holds a dozen places. Describing all of them would grow the prompt with the map, and
      // house_standing already NAMES them, so nothing is lost by describing only the closest.
      [Test]
      public void GIVEN_a_lord_who_holds_many_places_WHEN_rendered_THEN_only_the_closest_held_are_described()
      {
         SeatFacts[] many = Enumerable.Range(1, 6)
                                      .Select(i => new SeatFacts {
                                         PlaceName = $"Place{i}", Role = SeatRole.Owner,
                                         Kind = SettlementKind.Town, Loyalty = LoyaltyBand.Content
                                      })
                                      .ToArray();

         string said = Render(many);

         said.Should().Contain("Place1").And.Contain("Place2");
         said.Should().NotContain("Place3");
      }

      // Lean keeps the seat and drops the coaching, like every pack. It also drops the bands: in a Compact
      // prompt the difference between "comfortable" and "middling" changes nothing anyone would notice, while a
      // siege changes everything. The 2026-09-11 cut from 22,500 to 15,150 chars is what this protects.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_only_the_nearest_seat_and_only_what_is_urgent()
      {
         var burning = new SeatFacts {
            PlaceName = "Hargendorf", Role = SeatRole.Governor, Kind = SettlementKind.Town,
            Prosperity = ProsperityBand.Comfortable, Loyalty = LoyaltyBand.Restless, IsUnderSiege = true
         };

         string lean = Render(true, burning, Hargendorf(SeatRole.Owner));

         lean.Should().Contain("under siege");
         lean.Should().NotContain("comfortably");
         lean.Should().NotContain("Never give a figure");
         lean.Trim().Split('\n').Should().HaveCount(1);
      }

      // ── composition ──────────────────────────────────────────────────────

      // Rank is the wrong axis and this is where it shows. fkasad's governor is a COMPANION, no lord at all,
      // and he knows that town better than the count who owns it. A village headman likewise knows his own
      // granary better than anyone above him. A landless wanderer has no place to answer for.
      [Test]
      public void GIVEN_who_answers_for_a_place_WHEN_composing_THEN_it_follows_the_seat_and_not_the_rank()
      {
         Pack.CarriedBy(new KnowledgeBearer {IsPlayerCompanion = true, HoldsASeat = true}).Should().BeTrue();
         Pack.CarriedBy(new KnowledgeBearer {IsLord = true, HasHouse = true, HoldsASeat = true}).Should().BeTrue();
         Pack.CarriedBy(new KnowledgeBearer {IsNotable = true, HoldsASeat = true}).Should().BeTrue();

         Pack.CarriedBy(new KnowledgeBearer {IsLord = true, HasHouse = true}).Should().BeFalse();

         // A notable with nowhere to call home answers for nothing, and reporting that as a missing fact would
         // teach everyone to ignore the sweep - the lesson personal_bonds cost on the sweep's first live run.
         Pack.CarriedBy(new KnowledgeBearer {IsNotable = true}).Should().BeFalse();
         Pack.CarriedBy(new KnowledgeBearer()).Should().BeFalse();
         Pack.CarriedBy(null).Should().BeFalse();
      }

      // A place always has a name and always has a state, so a carried-but-empty pack means nobody read the
      // engine - which IS the fkasad bug, and is what the live sweep is for.
      [Test]
      public void GIVEN_a_seat_holder_with_nothing_supplied_WHEN_swept_THEN_it_counts_as_a_fault()
      {
         Pack.RequiresContent.Should().BeTrue();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {SeatStanding = new SeatStandingFacts()}).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {
            SeatStanding = new SeatStandingFacts {Seats = new List<SeatFacts> {Hargendorf()}}
         }).Should().BeTrue();
      }

      // The registry has to stay coherent as it grows: the sweep and the prompt both walk it by name.
      [Test]
      public void GIVEN_the_registry_WHEN_the_new_pack_joins_it_THEN_it_is_named_once_and_says_what_it_covers()
      {
         NpcKnowledgeFactory.All.Should().Contain(p => p.Name == "seat_standing");
         NpcKnowledgeFactory.All.Select(p => p.Name).Should().OnlyHaveUniqueItems();
         NpcKnowledgeFactory.For(new KnowledgeBearer {IsNotable = true, HoldsASeat = true})
                            .Should().Contain(p => p.Name == "seat_standing");
      }
   }
}

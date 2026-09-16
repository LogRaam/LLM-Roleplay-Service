// Code written by Gabriel Mailhot, 16/09/2026.
// What these pin: that a seat at the council table knows what it HOLDS.
//
// The audit of 15/09/2026 found the council carrying no knowledge at all. Read further and it was worse than
// the audit said: a CouncilMemberInput was a name, a sex, an age, one archetype word, a culture and a regard
// number. No memory, no relationships, and no STATION — so a seated governor did not know he governed and a
// seated king did not know he ruled.
//
// That is the Derthert/Garios failure StationPack was written to end, and it was still live in a whole
// conversation mode, invisible to the completeness sweep, because the table had built its own and emptier
// idea of a person. It is also where "many of them seem to talk the same way" bites hardest: a councillor
// had nothing but culture and archetype, which is the recipe for interchangeable voices.
//
// The clause lives on StationPack beside the paragraphs rather than in a council-side helper, so the table
// and the private word are two renderings of ONE fact set. These tests hold that line: whatever the pack
// states at length, it must be able to state briefly.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class CouncilSeatStationTests
   {
      // THE REPORTED CLASS, in its council form. A king at his own table must know he is king.
      [Test]
      public void GIVEN_a_seated_ruler_WHEN_described_briefly_THEN_the_crown_is_stated_with_its_own_title()
      {
         StationPack.DescribeBriefly(new EncounterContext {
            RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King"
         }).Should().Be("rules Vlandia as its King");
      }

      // The title is READ, never assumed: the player who reported this wrote "Kings AND Emperors", and one
      // word cannot serve both. A realm whose title failed to resolve still keeps its crown.
      [Test]
      public void GIVEN_a_ruler_whose_title_is_unknown_WHEN_described_briefly_THEN_he_still_rules()
      {
         StationPack.DescribeBriefly(new EncounterContext {RuledRealmName = "Vlandia"})
                    .Should().Be("rules Vlandia as its ruler");
      }

      // Rulership stands ALONE, exactly as it does in the long form, where an early return stops a ruler also
      // being told he heads a clan. A crown that reads as "rules Vlandia, heads the dey Meroc clan" makes the
      // throne sound like one office among several.
      [Test]
      public void GIVEN_a_ruler_who_also_heads_a_house_WHEN_described_briefly_THEN_only_the_crown_is_named()
      {
         StationPack.DescribeBriefly(new EncounterContext {
            RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King",
            LeadsOwnClan = true, SpeakerClanName = "dey Meroc"
         }).Should().NotContain("dey Meroc");
      }

      // THE OTHER HALF OF THE AUDIT'S FINDING: fkasad's governor, seated. The fact was in the engine all
      // along, used to gate an action and never to state a truth.
      [Test]
      public void GIVEN_a_seated_governor_WHEN_described_briefly_THEN_the_town_he_keeps_is_named()
      {
         StationPack.DescribeBriefly(new EncounterContext {GovernedSettlementName = "Sargot"})
                    .Should().Be("governs Sargot");
      }

      // Stations STACK below the crown, as they do in the long form: a man may head his house, keep a town
      // and carry the player's ledger, and all three are true at once.
      [Test]
      public void GIVEN_a_seat_holding_several_posts_WHEN_described_briefly_THEN_all_of_them_are_named()
      {
         string? line = StationPack.DescribeBriefly(new EncounterContext {
            LeadsOwnClan = true, SpeakerClanName = "dey Meroc",
            GovernedSettlementName = "Sargot", PartyPostHeld = "quartermaster"
         });

         line.Should().Contain("heads the dey Meroc clan");
         line.Should().Contain("governs Sargot");
         line.Should().Contain("your quartermaster");
      }

      // A host that forgot the NAME must cost the name, not the RANK — the same ruling the long form carries.
      // Losing the whole line is the king-denies-his-crown failure in miniature.
      [Test]
      public void GIVEN_a_clan_head_whose_house_name_is_missing_WHEN_described_briefly_THEN_the_rank_survives()
      {
         StationPack.DescribeBriefly(new EncounterContext {LeadsOwnClan = true})
                    .Should().Be("heads their own clan");
      }

      // A retainer is not a member of the household, and that distinction is the whole point of the line.
      [Test]
      public void GIVEN_a_seated_retainer_WHEN_described_briefly_THEN_he_is_not_read_as_one_of_the_household()
      {
         StationPack.DescribeBriefly(new EncounterContext {RidesAsRetainer = true})
                    .Should().Contain("of their own house still");
      }

      // MOST PEOPLE HOLD NOTHING, and that is a life rather than a gap (the pack's own RequiresContent=false
      // ruling). A commoner at the table must not be given an empty clause to read as a rank.
      [Test]
      public void GIVEN_a_seat_holding_no_station_WHEN_described_briefly_THEN_nothing_is_claimed()
      {
         StationPack.DescribeBriefly(new EncounterContext()).Should().BeNull();
         StationPack.DescribeBriefly(null).Should().BeNull();
      }

      // THE GUARD THAT KEEPS THE TWO IN STEP. Anything the long form states must have a short form, or the
      // next rank added reaches a private word and silently never reaches the table — which is exactly the
      // shape of the defect being fixed here.
      [Test]
      public void GIVEN_any_station_the_long_form_states_WHEN_described_briefly_THEN_the_short_form_states_it_too()
      {
         foreach (EncounterContext context in new[] {
                     new EncounterContext {RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King"},
                     new EncounterContext {LeadsOwnClan = true, SpeakerClanName = "dey Meroc"},
                     new EncounterContext {GovernedSettlementName = "Sargot"},
                     new EncounterContext {PartyPostHeld = "scout"},
                     new EncounterContext {RidesAsRetainer = true}
                  })
         {
            new StationPack().IsSupplied(context).Should().BeTrue("this fixture is only meaningful on a real station");
            StationPack.DescribeBriefly(context).Should().NotBeNullOrWhiteSpace(
               "every station the pack renders at length must also render in one clause, or the council loses it");
         }
      }
   }
}

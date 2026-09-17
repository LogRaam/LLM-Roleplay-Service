// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 5. What these pin: that a member of a house knows what that house is worth
// and what it has under arms, and that he says so without ever naming a figure.
//
// Why it exists. fkasad, 2026-09-11, about a companion governing his city: "I feel like companions should have
// a better understanding of the clan current status, territories, armies, treasury, wars etc." house_standing
// answered territories and wars the same day. This is the rest of that sentence.
//
// The second reason, from the bench a day later: a character handed a fact he plainly WOULD know, and never
// told it, does not fall silent - he fills it in. So the gap between "a lord obviously knows his own coffers"
// and "nobody ever told him" is not a harmless omission, it is an invitation to invent.
//
// If these fail, either a lord has stopped knowing what his own house is worth, or he has started quoting a
// number for it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;
using System.Text;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class HouseMeansKnowledgeTests
   {
      private static readonly HouseMeansPack Pack = new();

      private static string Render(HouseMeansFacts means, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {HouseMeans = means}, lean);

         return sb.ToString();
      }

      // ── the two things fkasad asked for ──────────────────────────────────

      // "treasury". A lord who cannot say whether his house is rich or broke cannot hold a conversation about
      // hiring, ransoming, or paying anyone - all of which the mod lets a player propose.
      [Test]
      public void GIVEN_strained_coffers_WHEN_he_speaks_of_his_house_THEN_he_knows_money_is_short()
      {
         Render(new HouseMeansFacts {Treasury = TreasuryBand.Strained}).Should().Contain("coffers are strained");
         Render(new HouseMeansFacts {Treasury = TreasuryBand.Deep}).Should().Contain("coffers are deep");
      }

      // "armies". And the case that matters most is the empty one: a lord with nobody in the field is a lord
      // who cannot send you troops, which is precisely the promise the mod must not let him make.
      [Test]
      public void GIVEN_no_one_in_the_field_WHEN_he_speaks_of_his_house_THEN_he_says_so_rather_than_omitting_it()
      {
         Render(new HouseMeansFacts {Muster = MusterBand.Nothing}).Should().Contain("no one in the field");
      }

      // Men on the map and men on campaign are not the same thing, and the difference is what a lord would
      // actually volunteer when asked whether he can come.
      [Test]
      public void GIVEN_his_men_ride_with_an_army_WHEN_he_speaks_of_them_THEN_that_is_part_of_the_answer()
      {
         string said = Render(new HouseMeansFacts {Muster = MusterBand.Respectable, RidingWithAnArmy = true});

         said.Should().Contain("respectable force").And.Contain("ride with an army");
      }

      // A debt to the crown is a fact a lord FEELS, and the engine keeps it explicitly rather than leaving it
      // to be inferred from a thin treasury - so it is read rather than guessed.
      [Test]
      public void GIVEN_a_debt_to_the_crown_WHEN_he_speaks_of_his_house_THEN_he_carries_it()
      {
         Render(new HouseMeansFacts {Treasury = TreasuryBand.Sound, OwesTheCrown = true})
            .Should().Contain("owes the crown");
      }

      // Court standing changes the register a lord speaks in, which is why it belongs with means rather than
      // with standing: it is what his word is worth today, not what his family has always held.
      [Test]
      public void GIVEN_a_house_whose_word_carries_WHEN_he_speaks_THEN_he_knows_how_far_it_carries()
      {
         Render(new HouseMeansFacts {Influence = InfluenceBand.Commanding}).Should().Contain("few voices carry further");
         Render(new HouseMeansFacts {Influence = InfluenceBand.Negligible}).Should().Contain("counts for very little");
      }

      // ── the rule that stops the cure becoming the disease ────────────────

      // Same discipline as seat_standing, and for the same reason: the digits are what a player can check, and
      // a pack that printed one would hand back the failure it was built to close.
      [Test]
      public void GIVEN_everything_known_WHEN_rendered_THEN_not_one_figure_appears_anywhere_in_it()
      {
         string said = Render(new HouseMeansFacts {
            Treasury = TreasuryBand.Deep, OwesTheCrown = true, Muster = MusterBand.Formidable,
            RidingWithAnArmy = true, Influence = InfluenceBand.Commanding
         });

         said.Should().NotBeEmpty();
         said.Should().NotContainAny("0", "1", "2", "3", "4", "5", "6", "7", "8", "9");
      }

      // And he is given the way out rather than only the prohibition, because a rule obeyed by silence is its
      // own regression - the lesson of the purse probe, which read well because she had somewhere to GO.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_rendered_THEN_figures_are_forbidden_and_a_way_to_decline_is_offered()
      {
         string said = Render(new HouseMeansFacts {Treasury = TreasuryBand.Sound});

         said.Should().Contain("Never name a sum");
         said.Should().Contain("keeps the books");
      }

      // Sentences, not an inventory. Influence supplies its own "and", so the joining must not double it: a
      // prompt that reads "sound and, and at court" teaches the model that this section is machine output.
      [Test]
      public void GIVEN_several_things_known_WHEN_rendered_THEN_it_reads_as_a_sentence_and_never_doubles_a_conjunction()
      {
         string said = Render(new HouseMeansFacts {
            Treasury = TreasuryBand.Sound, Muster = MusterBand.Small, Influence = InfluenceBand.Modest
         });

         said.Should().NotContain("and, and");
         said.Should().NotContain(" and and ");
         said.Should().Contain("coffers are sound, it keeps only a small force in the field, and at court");
      }

      // ── cost and composition ─────────────────────────────────────────────

      // Lean keeps coin and men - both change what a character can agree to - and drops court standing, which
      // only changes how he sounds. Compact was cut from 22,500 to 15,150 chars on 2026-09-11.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_it_keeps_what_he_can_do_and_drops_how_he_sounds()
      {
         string lean = Render(new HouseMeansFacts {
            Treasury = TreasuryBand.Empty, Muster = MusterBand.Nothing, Influence = InfluenceBand.Commanding
         }, true);

         lean.Should().Contain("coffers are all but empty").And.Contain("no one in the field");
         lean.Should().NotContain("court");
         lean.Should().NotContain("Never name a sum");
      }

      // fkasad's actual ask, stated as composition: a companion is not a lord, and their house IS the player's,
      // which is what makes them knowing the player's treasury ordinary rather than a leak. A landless wanderer
      // has no house to know about.
      [Test]
      public void GIVEN_who_belongs_to_a_house_WHEN_composing_THEN_a_companion_knows_it_and_a_wanderer_does_not()
      {
         Pack.CarriedBy(new KnowledgeBearer {IsPlayerCompanion = true, HasHouse = true}).Should().BeTrue();
         Pack.CarriedBy(new KnowledgeBearer {IsOfPlayerClan = true, HasHouse = true}).Should().BeTrue();
         Pack.CarriedBy(new KnowledgeBearer {IsLord = true, HasHouse = true}).Should().BeTrue();

         Pack.CarriedBy(new KnowledgeBearer {IsLord = true}).Should().BeFalse();
         Pack.CarriedBy(new KnowledgeBearer {IsNotable = true, HasHouse = true}).Should().BeFalse();
         Pack.CarriedBy(null).Should().BeFalse();
      }

      // A house always has coffers and always has a muster, so a carried-but-empty reading means nobody asked
      // the engine - which is the fkasad class of bug, and what the live sweep exists to catch.
      [Test]
      public void GIVEN_a_house_member_with_nothing_supplied_WHEN_swept_THEN_it_counts_as_a_fault()
      {
         Pack.RequiresContent.Should().BeTrue();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {HouseMeans = new HouseMeansFacts()}).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {HouseMeans = new HouseMeansFacts {Muster = MusterBand.Nothing}})
             .Should().BeTrue();
      }

      // "No one in the field" is a READING, not an absence, and the two must never collapse into each other:
      // that collapse is exactly what RequiresContent was invented to keep visible.
      [Test]
      public void GIVEN_a_house_with_nobody_under_arms_WHEN_swept_THEN_it_is_supplied_and_not_missing()
      {
         var empty = new EncounterContext {HouseMeans = new HouseMeansFacts {Muster = MusterBand.Nothing}};

         Pack.IsSupplied(empty).Should().BeTrue();
         Render(new HouseMeansFacts {Muster = MusterBand.Nothing}).Should().NotBeEmpty();
      }

      // SUPPLIED BUT MUTE, the worst state a pack can be in: it reports itself answered, so the completeness
      // sweep sees nothing wrong, and it says nothing at all. HasAnyReading counts RidingWithAnArmy on its
      // own, while the muster clause returned null whenever the BAND was unknown — taking the army with it
      // (audit, 15/09/2026). A missing band must cost the SIZE of the muster, never the fact that the men are
      // in the field, which is the more consequential of the two.
      [Test]
      public void GIVEN_an_unknown_muster_but_men_in_the_field_WHEN_rendered_THEN_the_army_is_still_reported()
      {
         var facts = new HouseMeansFacts {Muster = MusterBand.Unknown, RidingWithAnArmy = true};

         facts.HasAnyReading.Should().BeTrue("the army alone is a reading, which is why the silence was invisible");
         Render(facts).Should().Contain("ride with an army");
      }

      // A house whose ONLY reading is its influence produced a sentence that began "and at court its word
      // counts for very little." The Influence clauses carry a leading "and" so they read as a continuation,
      // which is right after something and wrong after nothing. The old test passed because it asserted a
      // SUBSTRING, and a dangling conjunction survives a substring check untouched.
      [Test]
      public void GIVEN_influence_as_the_only_reading_WHEN_rendered_THEN_the_sentence_does_not_open_with_a_conjunction()
      {
         string line = Render(new HouseMeansFacts {Influence = InfluenceBand.Negligible}).Trim();

         // The clause follows the pack's own lead-in and its colon, so it stays lower-case; what must go is
         // the conjunction, which had nothing to join.
         line.Should().Contain(": at court its word counts for very little");
         line.Should().NotContain(": and ");
      }

      // And the conjunction must SURVIVE when something really does precede it, or fixing the opening would
      // have cost the reading of every multi-clause line.
      [Test]
      public void GIVEN_influence_after_another_reading_WHEN_rendered_THEN_it_still_reads_as_a_continuation()
      {
         Render(new HouseMeansFacts {Treasury = TreasuryBand.Strained, Influence = InfluenceBand.Negligible})
            .Should().Contain("and at court its word counts for very little");
      }
   }
}

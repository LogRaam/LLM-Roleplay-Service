// Code written by Gabriel Mailhot, 11/09/2026.
// Increment 1 of the knowledge-composition pillar. What these pin is Gabriel's (b): that a profile composes the
// RIGHT memory, stated as rules about the world rather than about the code that produces them.
//
// Why it exists. fkasad, 2026-09-11: "I feel like companions should have a better understanding of the clan
// current status, territories, armies, treasury, wars etc..." - written after finding that his own governor did
// not know he governed. The single-field fixes had run out: what a character knows had become implicit in which
// of 170 flat context properties the host happened to fill.
//
// NOTE WHAT THESE TESTS CANNOT DO, because it is the reason a second guard exists. They build their own
// context. They can prove that a companion WOULD carry his house's standing and that a merchant would not; they
// cannot prove the host ever supplied it, since they supply it themselves. That half is cr.testall's, sweeping
// real heroes, and it is the half that would have caught the governor.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class HouseStandingKnowledgeTests
   {
      private static readonly HouseStandingPack Pack = new();

      private static KnowledgeBearer Companion() => new() {IsPlayerCompanion = true, HasHouse = true};
      private static KnowledgeBearer ForeignLord() => new() {IsLord = true, HasHouse = true};
      private static KnowledgeBearer Merchant() => new() {IsNotable = true, HasHouse = false};
      private static KnowledgeBearer Wanderer() => new() {HasHouse = false};

      private static string Render(HouseStandingFacts facts, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {HouseStanding = facts}, lean);

         return sb.ToString();
      }

      // ── the composition rule: who knows this at all ──────────────────────

      // THE REPORT. A companion's house IS the player's house, so the player's holdings are his own house's
      // business and not a leak. This is the rule fkasad was asking for.
      [Test]
      public void GIVEN_a_companion_of_the_players_house_WHEN_composing_THEN_he_carries_its_standing()
      {
         Pack.CarriedBy(Companion()).Should().BeTrue();
         Pack.CarriedBy(new KnowledgeBearer {IsOfPlayerClan = true, HasHouse = true}).Should().BeTrue();
      }

      // A lord of another house knows HIS house's standing, which is why the pack is about the bearer's own
      // house rather than about the player's. Both halves fall out of one rule instead of two permissions.
      [Test]
      public void GIVEN_a_lord_of_another_house_WHEN_composing_THEN_he_carries_his_own_standing()
      {
         Pack.CarriedBy(ForeignLord()).Should().BeTrue();
      }

      // The guard that keeps this fix from becoming a worse bug than the one it repairs: a merchant must not be
      // handed a house's affairs, or every stranger in Calradia would suddenly discuss your holdings.
      [Test]
      public void GIVEN_a_town_merchant_WHEN_composing_THEN_he_carries_no_house_standing_at_all()
      {
         Pack.CarriedBy(Merchant()).Should().BeFalse();
      }

      // A landless wanderer has no house whose standing there could be. Structural ignorance, not an instruction
      // the model is asked to respect.
      [Test]
      public void GIVEN_a_landless_wanderer_WHEN_composing_THEN_there_is_no_standing_for_him_to_carry()
      {
         Pack.CarriedBy(Wanderer()).Should().BeFalse();
         Pack.CarriedBy(null).Should().BeFalse();
      }

      // The factory is the thing the rest of the mod calls, so the rule must hold THROUGH it and not only on the
      // pack: composing a merchant must yield nothing to render.
      [Test]
      public void GIVEN_the_factory_WHEN_composing_each_kind_of_person_THEN_it_answers_as_the_rule_does()
      {
         NpcKnowledgeFactory.For(Companion()).Should().Contain(p => p.Name == "house_standing");
         NpcKnowledgeFactory.For(Merchant()).Should().NotContain(p => p.Name == "house_standing");
         NpcKnowledgeFactory.For(null).Should().BeEmpty();
      }

      // ── what is actually said ────────────────────────────────────────────

      [Test]
      public void GIVEN_a_house_with_lands_WHEN_rendered_THEN_the_lands_are_named()
      {
         string said = Render(new HouseStandingFacts {
            HouseName = "Aelindrath", IsPlayerHouse = true, Fiefs = new List<string> {"Hargendorf", "Pravend"}
         });

         said.Should().Contain("Hargendorf").And.Contain("Pravend");
      }

      // The instruction that answers the word the model itself chose when it had not been told: "revealed".
      [Test]
      public void GIVEN_his_own_houses_lands_WHEN_rendered_THEN_he_is_told_they_are_not_news_to_him()
      {
         Render(new HouseStandingFacts {HouseName = "Aelindrath", Fiefs = new List<string> {"Hargendorf"}})
            .Should().Contain("not news");
      }

      // A house that holds nine towns must not sound like a house that holds four. Truncating in silence would
      // teach the character something false, which is worse than teaching him nothing.
      [Test]
      public void GIVEN_more_lands_than_are_worth_listing_WHEN_rendered_THEN_the_rest_are_owned_up_to()
      {
         string said = Render(new HouseStandingFacts {
            HouseName = "dey Meroc",
            Fiefs = new List<string> {"A", "B", "C", "D", "E", "F"}
         });

         said.Should().Contain("2 others");
      }

      [Test]
      public void GIVEN_a_house_at_war_WHEN_rendered_THEN_the_enemies_are_named()
      {
         Render(new HouseStandingFacts {HouseName = "dey Meroc", AtWarWith = new List<string> {"Sturgia"}})
            .Should().Contain("at war with Sturgia");
      }

      // A house at peace holding nothing must produce no sentence about lands or wars, rather than an empty
      // "your house holds ." that reads as a bug to anyone who sees it.
      [Test]
      public void GIVEN_a_house_with_no_lands_and_no_wars_WHEN_rendered_THEN_nothing_is_claimed_about_either()
      {
         string said = Render(new HouseStandingFacts {HouseName = "Aelindrath"});

         said.Should().Contain("Aelindrath");
         said.ToLowerInvariant().Should().NotContain("holds");
         said.ToLowerInvariant().Should().NotContain("at war");
      }

      // ── the budget, which the design says must be carried from increment one ──

      // Lean gets the FACTS and not the conduct. The Compact prompt was cut from 22,500 to 15,150 characters on
      // 2026-09-11 and composition is exactly the thing that could spend it back in one release.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_it_carries_the_facts_without_the_coaching()
      {
         var facts = new HouseStandingFacts {
            HouseName = "Aelindrath", Fiefs = new List<string> {"Hargendorf"}, AtWarWith = new List<string> {"Sturgia"}
         };

         string lean = Render(facts, true);
         string full = Render(facts);

         lean.Should().Contain("Hargendorf").And.Contain("Sturgia");
         lean.Length.Should().BeLessThan(full.Length);
      }

      // ── the two questions the pack must answer separately ────────────────

      // Carried and supplied are different questions, and the gap between them is where the governor bug lived.
      // A pack that conflated them could not be swept for completeness at all.
      [Test]
      public void GIVEN_a_context_the_host_never_filled_WHEN_asked_THEN_the_pack_reports_it_as_unsupplied()
      {
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {HouseStanding = new HouseStandingFacts()}).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {HouseStanding = new HouseStandingFacts {HouseName = "Aelindrath"}})
             .Should().BeTrue();
      }

      // Enumerability is the property the whole design rests on: it is what lets a sweep, and Gabriel's audit,
      // ask about packs nobody has thought to ask about yet.
      [Test]
      public void GIVEN_the_factory_WHEN_enumerated_THEN_every_pack_can_describe_itself_for_review()
      {
         NpcKnowledgeFactory.All.Should().NotBeEmpty();
         NpcKnowledgeFactory.All.Select(p => p.Name).Should().OnlyHaveUniqueItems();
         NpcKnowledgeFactory.All.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Covers));
      }
   }
}

// Code written by Gabriel Mailhot, 12/09/2026.
// What these pin: that a small model loses the LEAST valuable thing a character knows, and not merely the
// thing that happens to be said last.
//
// THE MEASUREMENT THAT FORCED THEM. The Compact allowance was set at 560 characters when there were seven
// packs. At thirteen, a maximally-supplied character measured like this:
//
//   budget=560 spent=505 :: station=200 | here_and_now=78 | own_body=72 | house_standing=136 |
//   house_means=85 | seat_standing=74 | party_standing=131 DROPPED | own_ventures=152 DROPPED |
//   underworld=155 DROPPED | realm_news=118 DROPPED | personal_bonds=60 |
//
// The drop was first-fit down the registry, and the registry is the order things are SAID. So the character
// kept his personal grudges - sixty characters, last in the list - and lost that the player was badly WANTED,
// that a war had been declared, and that his own company was short of food. Three facts that decide what he
// may say or agree to, traded for one that decides how it sounds.
//
// Nothing was broken. Every pack worked, the allowance held, the tests were green: the registry had simply
// become a priority list without anybody ever writing it as one, which is the same failure as the magnitude
// numbers chosen as ranks - a value quietly doing a second job nobody declared.
//
// If these fail, either the two orderings have been welded back together, or somebody has given colour a
// higher claim on a small model's prompt than consequence.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PackDropPriorityTests
   {
      private static KnowledgePack Pack(string name) => NpcKnowledgeFactory.All.Single(p => p.Name == name);

      private static IReadOnlyList<string> Affordable(IEnumerable<KnowledgePack> carried,
                                                      IDictionary<KnowledgePack, int> cost)
         => NpcKnowledgeFactory.AffordableInCompact(carried.ToList(), p => cost[p]).Select(p => p.Name).ToList();

      // ── the regression itself, at the measured numbers ──

      // The exact character who broke it, at the exact costs. What he loses now is his grudges; what he keeps
      // is that the player is wanted and that his own company is short of food.
      [Test]
      public void GIVEN_the_character_who_broke_the_registry_WHEN_compact_THEN_consequence_outlives_colour()
      {
         var cost = new Dictionary<KnowledgePack, int> {
            [Pack("station")] = 200,
            [Pack("here_and_now")] = 78,
            [Pack("own_body")] = 72,
            [Pack("house_standing")] = 136,
            [Pack("house_means")] = 85,
            [Pack("seat_standing")] = 74,
            [Pack("party_standing")] = 131,
            [Pack("own_ventures")] = 152,
            [Pack("underworld")] = 155,
            [Pack("realm_news")] = 118,
            [Pack("personal_bonds")] = 60
         };

         IReadOnlyList<string> kept = Affordable(cost.Keys, cost);

         // The three that used to be lost, and each one changes what he may agree to.
         kept.Should().Contain("underworld", "he must know the player is wanted");
         kept.Should().Contain("party_standing", "he must know his own company is short of food");
         kept.Should().Contain("own_body", "a man barely on his feet must not promise to ride out");

         // Something still has to go - the allowance binds, that is the whole point of it - and what goes is
         // the colour. Losing a cousin's name plays the same scene slightly flatter.
         kept.Should().NotContain("personal_bonds");
         kept.Count.Should().BeLessThan(cost.Count);
      }

      // The old behaviour, stated as the thing that must never come back.
      [Test]
      public void GIVEN_a_cheap_pack_said_last_WHEN_the_budget_binds_THEN_it_does_not_outlive_a_costly_one_said_first()
      {
         var cost = new Dictionary<KnowledgePack, int> {
            [Pack("underworld")] = NpcKnowledgeFactory.LeanBudgetChars,
            [Pack("personal_bonds")] = 1
         };

         Affordable(cost.Keys, cost).Should().Equal("underworld");
      }

      // ── the two orderings really are two ──

      // Priority decides what SURVIVES; the caller still says what survives in the order it handed over. A
      // prompt whose sections reshuffle by cost would break the cache split as well as the prose.
      [Test]
      public void GIVEN_packs_in_narrative_order_WHEN_some_are_dropped_THEN_the_survivors_keep_that_order()
      {
         List<KnowledgePack> narrative =
            new[] {"personal_bonds", "realm_news", "underworld", "own_body"}.Select(Pack).ToList();

         Dictionary<KnowledgePack, int> cost = narrative.ToDictionary(p => p, _ => 10);

         Affordable(narrative, cost).Should().Equal("personal_bonds", "realm_news", "underworld", "own_body");
      }

      // Same input, same answer, every time. A prompt that differs run to run cannot be cached and cannot be
      // read back out of a log.
      [Test]
      public void GIVEN_the_same_character_twice_WHEN_the_budget_binds_THEN_the_same_packs_are_dropped()
      {
         List<KnowledgePack> all = NpcKnowledgeFactory.All.ToList();
         Dictionary<KnowledgePack, int> cost = all.ToDictionary(p => p, _ => 100);

         Affordable(all, cost).Should().Equal(Affordable(all, cost));
      }

      // ── what may never be dropped ──

      // The king-denies-his-crown bug guarded one level down: who a character IS is not budgeted, so it can
      // neither be crowded out however expensive it is, nor crowd out what he knows.
      [Test]
      public void GIVEN_a_constitutive_pack_larger_than_the_whole_allowance_WHEN_compact_THEN_it_is_still_carried()
      {
         var cost = new Dictionary<KnowledgePack, int> {
            [Pack("station")] = NpcKnowledgeFactory.LeanBudgetChars * 3,
            [Pack("own_body")] = 50
         };

         Affordable(cost.Keys, cost).Should().Equal("station", "own_body");
      }

      // ── the declaration itself ──

      // A tie is not a decision, it is two authors who never met. Distinct numbers force whoever adds the
      // fourteenth pack to say out loud what it is worth against the thirteen.
      [Test]
      public void GIVEN_every_pack_WHEN_asked_what_it_is_worth_THEN_no_two_answer_the_same()
      {
         NpcKnowledgeFactory.All
                            .Where(p => p.Kind == PackKind.Contingent)
                            .Select(p => p.DropPriority)
                            .Should()
                            .OnlyHaveUniqueItems();
      }

      // The ordering principle as an assertion rather than a comment: what changes what a character may SAY
      // OR DO outranks what colours how he says it.
      [Test]
      public void GIVEN_the_declared_order_WHEN_read_THEN_consequence_outranks_colour()
      {
         int Rank(string name) => Pack(name).DropPriority;

         Rank("own_body").Should().BeLessThan(Rank("personal_bonds"));
         Rank("party_standing").Should().BeLessThan(Rank("here_and_now"));
         Rank("underworld").Should().BeLessThan(Rank("realm_news"));
         Rank("house_means").Should().BeLessThan(Rank("own_ventures"));
         Rank("seat_standing").Should().BeLessThan(Rank("house_standing"));
      }

      // Refusing rather than guessing, the shape every other entry point here already has.
      [Test]
      public void GIVEN_nothing_to_weigh_WHEN_asked_THEN_it_answers_with_nothing_rather_than_throwing()
      {
         NpcKnowledgeFactory.AffordableInCompact(null, p => 0).Should().BeEmpty();
         NpcKnowledgeFactory.AffordableInCompact(new List<KnowledgePack>(), null).Should().BeEmpty();
         NpcKnowledgeFactory.AffordableInCompact(new List<KnowledgePack>(), p => 0).Should().BeEmpty();
      }
   }
}

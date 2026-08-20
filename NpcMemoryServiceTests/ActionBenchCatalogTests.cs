// Code written by Gabriel Mailhot, 17/08/2026.
// The extraction bench only proves anything if it actually exercises every action. These pure tests pin the
// corpus's shape: every dispatchable verb has at least one positive case (the coverage contract, and the forcing
// function that stays red until the corpus is filled), negative cases exist as a first-class part of the bench, and
// each case is well-formed enough to run (real prose, a real catalog verb, an expectation on the right side).

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ActionBenchCatalogTests
   {
      // The coverage contract: testing a verb on ONE phrasing proves only that grok handles that sentence, not the
      // deed. The prose model writes the same deed many ways (blunt, flowery, oblique), so every verb needs at least
      // THREE distinct positive phrasings; that is what tells a hard deed apart from a merely hard sentence. This is
      // the forcing function of the multi-composition round: it stays red until every verb carries three variants.
      [Test]
      public void GIVEN_the_bench_WHEN_inspected_THEN_every_catalog_verb_has_at_least_three_positive_phrasings()
      {
         Dictionary<string, int> positivesByVerb = ActionBenchCatalog.All
            .Where(c => !c.IsNegative)
            .GroupBy(c => c.Verb)
            .ToDictionary(g => g.Key, g => g.Count());

         foreach (string verb in GameActionCatalog.Types)
         {
            positivesByVerb.TryGetValue(verb, out int count);
            (count >= 3).Should().BeTrue($"'{verb}' has {count} positive phrasing(s); it needs at least 3 to test wording robustness");
         }
      }

      // Negatives are not an afterthought: the interpreter fails hardest by emitting a look-alike, so the bench must
      // carry withholding cases too. This guards that the negative half is never quietly dropped to zero.
      [Test]
      public void GIVEN_the_bench_WHEN_inspected_THEN_it_carries_negative_withholding_cases()
      {
         ActionBenchCatalog.All.Count(c => c.IsNegative).Should().BeGreaterThan(1);
      }

      // A case the harness cannot run, or cannot score, is dead weight that still counts toward coverage. Every case
      // needs a real prose payload, real context, a unique id, a verb that is actually in the catalog, and an
      // expectation on exactly one side (an expected type for a positive, a forbidden type for a negative).
      [Test]
      public void GIVEN_the_bench_WHEN_inspected_THEN_every_case_is_well_formed()
      {
         ActionBenchCatalog.All.Select(c => c.Id).Should().OnlyHaveUniqueItems();

         foreach (ActionBenchCase c in ActionBenchCatalog.All)
         {
            c.Id.Should().NotBeNullOrWhiteSpace();
            c.Prose.Should().NotBeNullOrWhiteSpace();
            c.ContextFacts.Should().NotBeNullOrWhiteSpace();
            GameActionCatalog.Types.Should().Contain(c.Verb, $"case '{c.Id}' names a verb not in the catalog");

            if (c.IsNegative)
               c.ForbiddenType.Should().NotBeNullOrWhiteSpace($"negative case '{c.Id}' must name a forbidden type");
            else
               c.Expected.Should().NotBeEmpty($"positive case '{c.Id}' must expect at least one action");
         }
      }

      // The focus slice (cr.action_bench focus) exists to cut the token bill of a refinement loop. It is a real
      // subset of the corpus, drawn by id: a typo'd or renamed id would be SILENTLY skipped, quietly shrinking the
      // slice below what the editor believed they were watching. This pins that every focus id resolves to a real
      // case, so a stale id is caught here instead of costing a blind, under-covered run in game.
      [Test]
      public void GIVEN_the_focus_slice_WHEN_inspected_THEN_it_is_a_non_empty_subset_whose_every_id_resolves()
      {
         HashSet<string> allIds = ActionBenchCatalog.All.Select(c => c.Id).ToHashSet();

         ActionBenchCatalog.Focus.Should().NotBeEmpty("the focus slice must run something");
         ActionBenchCatalog.Focus.Count.Should().BeLessThan(ActionBenchCatalog.All.Count, "focus is a cheap SUBSET, not the whole corpus");
         ActionBenchCatalog.Focus.Select(c => c.Id).Should().OnlyHaveUniqueItems();

         // The real guard: every DECLARED id must resolve. If one is stale, Focus (which silently drops it) is
         // smaller than the declared set, and the slice quietly under-covers what the editor believed it watched.
         List<string> stale = ActionBenchCatalog.DeclaredFocusIds.Where(id => !allIds.Contains(id)).ToList();
         stale.Should().BeEmpty("these focus ids resolve to no case (renamed or typo'd): " + string.Join(", ", stale));
         ActionBenchCatalog.Focus.Count.Should().Be(ActionBenchCatalog.DeclaredFocusIds.Count, "every declared focus id must resolve to exactly one case");
      }
   }
}

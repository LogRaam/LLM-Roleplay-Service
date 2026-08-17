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
      // The coverage contract: an extraction bench that skips a verb proves nothing about that verb. Every
      // dispatchable action must have at least one POSITIVE case (a reply where it genuinely should fire), or a
      // regression in how it is taught would sail through unmeasured. This is the migration's forcing function: it
      // stays red until every verb has a case.
      [Test]
      public void GIVEN_the_bench_WHEN_inspected_THEN_every_catalog_verb_has_at_least_one_positive_case()
      {
         HashSet<string> covered = ActionBenchCatalog.All
            .Where(c => !c.IsNegative)
            .Select(c => c.Verb)
            .ToHashSet();

         foreach (string verb in GameActionCatalog.Types)
            covered.Should().Contain(verb, $"'{verb}' has no positive extraction case");
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
               c.ExpectedType.Should().NotBeNullOrWhiteSpace($"positive case '{c.Id}' must name an expected type");
         }
      }
   }
}

// Code written by Gabriel Mailhot, 14/08/2026.
// Unified Action Catalog, Stage 1: GameActionCatalog is the single source of truth the Action Interpreter is
// taught from, and the in-game action_catalog_parity self-test asserts it stays in lockstep with the mod's
// engine-bound BannerlordGameStateBridge.HandledActionTypes. Neither guard helps if the catalog itself is
// malformed (a duplicate key silently shadowing a real verb, a blank description teaching nothing). These pure
// tests pin the catalog's own well-formedness, the one thing a pure SDK test CAN reach.

#region

using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GameActionCatalogTests
   {
      // A duplicate Type would mean two GameActionSpec entries answer to the same [ACTION] "type:" key: whichever
      // the interpreter or a lookup finds first silently shadows the other's description and parameters, and the
      // shadowed verb is effectively never taught correctly.
      [Test]
      public void GIVEN_the_catalog_WHEN_inspected_THEN_every_type_id_is_unique()
      {
         GameActionCatalog.All.Should().OnlyHaveUniqueItems(spec => spec.Type);
      }

      // A blank Type would mean an [ACTION] block the interpreter cannot even name, and a blank Description would
      // teach the LLM nothing about what the deed actually is (the whole point of the catalog: the LLM only ever
      // knows a verb through this description). Either would silently defeat the catalog's purpose for that entry.
      [Test]
      public void GIVEN_the_catalog_WHEN_inspected_THEN_every_spec_has_a_non_empty_type_and_description()
      {
         foreach (GameActionSpec spec in GameActionCatalog.All)
         {
            spec.Type.Should().NotBeNullOrWhiteSpace();
            spec.Description.Should().NotBeNullOrWhiteSpace();
         }
      }

      // GameActionCatalog.Types is the parity surface the in-game action_catalog_parity self-test compares against
      // BannerlordGameStateBridge.HandledActionTypes. If it silently dropped or duplicated an entry relative to
      // All, that parity check would compare the wrong set and could pass while a real verb drifted untaught.
      [Test]
      public void GIVEN_the_catalog_WHEN_inspected_THEN_types_matches_all_one_for_one()
      {
         GameActionCatalog.Types.Should().HaveCount(GameActionCatalog.All.Count);

         foreach (GameActionSpec spec in GameActionCatalog.All)
            GameActionCatalog.Types.Should().Contain(spec.Type);
      }

      // Every parameter the catalog lists is rendered straight into the interpreter prompt as "name=meaning": a
      // blank name or meaning would render an empty or malformed hint, worse than omitting the parameter entirely.
      [Test]
      public void GIVEN_the_catalog_WHEN_inspected_THEN_every_parameter_has_a_non_empty_name_and_meaning()
      {
         foreach (GameActionSpec spec in GameActionCatalog.All)
         foreach (GameActionParam param in spec.Parameters)
         {
            param.Name.Should().NotBeNullOrWhiteSpace();
            param.Meaning.Should().NotBeNullOrWhiteSpace();
         }
      }
   }
}

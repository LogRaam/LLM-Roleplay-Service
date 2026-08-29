// Code written by Gabriel Mailhot, 28/08/2026.
// Increment 1 of a public "register an external action" API: a third-party mod (the BC-CR Bridge today) currently
// injects its own action tokens by casting GameActionCatalog.All to a List<GameActionSpec> and mutating it, which
// only works because the backing object happens to be a List. GameActionCatalog.Register gives it a real,
// supported registration door instead. These tests pin the registration channel's contract in isolation from the
// built-in catalog: registered externals must be taught (appear in All/Types) without ever touching, shadowing, or
// duplicating a built-in, and static registration state must never leak between tests.

#region

using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GameActionCatalogRegistrationTests
   {
      [SetUp]
      public void SetUp()
      {
         GameActionCatalog.ClearRegistered();
      }

      [TearDown]
      public void TearDown()
      {
         GameActionCatalog.ClearRegistered();
      }

      // STAKE: the interpreter prompt is built from GameActionCatalog.All. If a registered action never appears
      // there, the LLM is never taught it, and the third-party mod's own verb can never actually fire.
      [Test]
      public void GIVEN_a_fresh_external_spec_WHEN_registered_THEN_it_appears_in_All_and_Types_after_the_built_ins()
      {
         int builtInCount = GameActionCatalog.BuiltIn.Count;
         var spec = new GameActionSpec("bc_bridge_summon_caravan", "The bridge's own mod summons a caravan escort.",
            null, null, null);

         bool result = GameActionCatalog.Register(spec);

         result.Should().BeTrue();
         GameActionCatalog.All.Should().HaveCount(builtInCount + 1);
         GameActionCatalog.All[builtInCount].Should().BeSameAs(spec);
         GameActionCatalog.Types.Should().HaveCount(builtInCount + 1);
         GameActionCatalog.Types.Should().Contain("bc_bridge_summon_caravan");
         GameActionCatalog.BuiltIn.Should().HaveCount(builtInCount);
         GameActionCatalog.BuiltInTypes.Should().HaveCount(builtInCount);
      }

      // STAKE: a shadowed built-in would be taught twice under one Type key and could be silently overridden by a
      // third-party definition the bridge does not actually dispatch that way.
      [Test]
      public void GIVEN_a_type_that_already_exists_as_a_built_in_WHEN_registered_THEN_it_is_rejected_and_not_duplicated()
      {
         int builtInCount = GameActionCatalog.BuiltIn.Count;
         string existingType = GameActionCatalog.BuiltIn[0].Type;
         var impostor = new GameActionSpec(existingType, "A third-party redefinition of a built-in verb.", null, null,
            null);

         bool result = GameActionCatalog.Register(impostor);

         result.Should().BeFalse();
         GameActionCatalog.All.Should().HaveCount(builtInCount);
         GameActionCatalog.All.Should().OnlyHaveUniqueItems(s => s.Type);
      }

      // STAKE: a duplicate external Type would teach the LLM the same action twice, wasting prompt budget and
      // creating an ambiguous [ACTION] key with two competing descriptions.
      [Test]
      public void GIVEN_an_external_type_already_registered_WHEN_registered_again_THEN_the_second_registration_is_rejected()
      {
         var first = new GameActionSpec("bc_bridge_summon_caravan", "First registration.", null, null, null);
         var second = new GameActionSpec("bc_bridge_summon_caravan", "Second, competing registration.", null, null,
            null);

         bool firstResult = GameActionCatalog.Register(first);
         bool secondResult = GameActionCatalog.Register(second);

         firstResult.Should().BeTrue();
         secondResult.Should().BeFalse();
         GameActionCatalog.All.Should().OnlyHaveUniqueItems(s => s.Type);
         GameActionCatalog.All.Should().Contain(s => s.Type == "bc_bridge_summon_caravan" && s.Description == "First registration.");
      }

      // STAKE: a mod that passes a null spec, or a spec it forgot to give a Type, must be safely ignored rather than
      // crash the mod's own load sequence or corrupt the catalog.
      [Test]
      public void GIVEN_a_null_spec_or_a_spec_with_no_type_WHEN_registered_THEN_it_is_rejected_without_throwing()
      {
         int builtInCount = GameActionCatalog.BuiltIn.Count;
         var blankType = new GameActionSpec("", "No type at all.", null, null, null);
         var nullType = new GameActionSpec(null, "Null type.", null, null, null);

         GameActionCatalog.Register(null).Should().BeFalse();
         GameActionCatalog.Register(blankType).Should().BeFalse();
         GameActionCatalog.Register(nullType).Should().BeFalse();
         GameActionCatalog.All.Should().HaveCount(builtInCount);
      }

      // STAKE: without a clean reset, static registration state would leak across a mod reload, or across test
      // runs sharing the same process, and silently accumulate stale or duplicate actions over time.
      [Test]
      public void GIVEN_registered_externals_WHEN_ClearRegistered_is_called_THEN_All_and_Types_return_to_the_built_in_set()
      {
         int builtInCount = GameActionCatalog.BuiltIn.Count;
         GameActionCatalog.Register(new GameActionSpec("bc_bridge_summon_caravan", "Desc.", null, null, null));
         GameActionCatalog.All.Should().HaveCount(builtInCount + 1);

         GameActionCatalog.ClearRegistered();

         GameActionCatalog.All.Should().HaveCount(builtInCount);
         GameActionCatalog.Types.Should().HaveCount(builtInCount);
         GameActionCatalog.All.Should().BeEquivalentTo(GameActionCatalog.BuiltIn);
      }
   }
}

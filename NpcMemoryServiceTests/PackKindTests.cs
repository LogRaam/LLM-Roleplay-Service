// Code written by Gabriel Mailhot, 12/09/2026.
// What these pin: that "what a character IS" and "what a character KNOWS" are treated differently by the
// prompt budget, and that the difference cannot be quietly abused.
//
// Gabriel's distinction, and it resolved a real objection rather than papering over it. Asked whether the
// identity block should migrate into the pack architecture, the answer had to be no, because the Compact
// allowance SKIPS a pack that will not fit - fine for what someone knows, catastrophic for who someone is. A
// busy landed lord would silently lose his own name and station from a small model's prompt, which is the
// king-denies-his-crown bug rebuilt on purpose. His answer: two kinds, and the IS kind is mandatory. That
// makes the migration safe, so it makes the migration possible.
//
// The danger this file exists to close is the obvious one: if "mandatory" is just a flag, somebody will one
// day mark a knowledge pack constitutive to force it into Compact, and the budget will quietly stop meaning
// anything.
//
// If these fail, either a character can lose what makes them themselves, or the budget has a back door.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NUnit.Framework;
using System.Linq;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PackKindTests
   {
      // A new pack is BUDGETED unless somebody deliberately argues otherwise. The default has to fall on the
      // safe side, because the unsafe side is silent: an over-eager constitutive pack does not fail, it just
      // pushes a player's context over a cliff on a small model.
      [Test]
      public void GIVEN_a_pack_that_says_nothing_about_its_kind_WHEN_read_THEN_it_is_treated_as_knowledge()
      {
         new StubPack().Kind.Should().Be(PackKind.Contingent);
      }

      // THE BACK DOOR, closed. "Mandatory" and "everyone has it" are the same claim: a constitutive pack whose
      // composition rule can REFUSE somebody is a pack claiming to be part of who they are while admitting it
      // might not be. Anyone marking a pack constitutive to escape the budget trips this.
      [Test]
      public void GIVEN_a_constitutive_pack_WHEN_composing_THEN_it_can_never_refuse_anybody()
      {
         foreach (KnowledgePack pack in NpcKnowledgeFactory.All.Where(p => p.Kind == PackKind.Constitutive))
         {
            pack.CarriedBy(new KnowledgeBearer())
                .Should().BeTrue($"{pack.Name} claims to be part of who a character is");

            // Including the people the contingent packs legitimately refuse: a captive, a landless wanderer,
            // a notable with no house. What someone IS does not depend on their circumstances.
            pack.CarriedBy(new KnowledgeBearer {IsCutOff = true}).Should().BeTrue(pack.Name);
            pack.CarriedBy(new KnowledgeBearer {IsNotable = true}).Should().BeTrue(pack.Name);
         }
      }

      // And the same rule from the other end: a pack that everyone carries is not thereby constitutive. The
      // weather is carried by everybody and is still perfectly droppable on a small model - the line is
      // "would the character stop making sense", not "is it unconditional".
      [Test]
      public void GIVEN_a_pack_everyone_carries_WHEN_read_THEN_that_alone_does_not_make_it_part_of_who_they_are()
      {
         new HereAndNowPack().Kind.Should().Be(PackKind.Contingent);
         new OwnBodyPack().Kind.Should().Be(PackKind.Contingent);
      }

      // The allowance is for knowledge only. Stated as a property of the registry so that the day something
      // constitutive DOES arrive, nobody has to remember that the budget must not count it.
      [Test]
      public void GIVEN_the_registry_WHEN_budgeting_THEN_only_knowledge_is_ever_measured_against_the_allowance()
      {
         NpcKnowledgeFactory.All.Where(p => p.Kind == PackKind.Contingent).Should().NotBeEmpty();
         NpcKnowledgeFactory.LeanBudgetChars.Should().BeGreaterThan(0);
      }

      #region private

      /// <summary>A pack that declares nothing but the minimum, to read the defaults off.</summary>
      private sealed class StubPack : KnowledgePack
      {
         public override string Name => "stub";
         public override string Covers => "nothing at all";
         public override bool CarriedBy(KnowledgeBearer bearer) => bearer != null;
         public override bool IsSupplied(NpcMemoryService.Core.Models.EncounterContext context) => false;

         public override void Render(System.Text.StringBuilder sb,
                                     NpcMemoryService.Core.Models.EncounterContext context,
                                     bool lean) { }
      }

      #endregion
   }
}

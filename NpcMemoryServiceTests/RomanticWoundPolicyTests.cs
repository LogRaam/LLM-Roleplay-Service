// Code written by Gabriel Mailhot, 17/07/2026.
// Romance audit M-J5: when the player betrays a partner, the jealousy system wounded their regard but left their
// romantic STATUS untouched, so a betrayed spouse read as "Committed" as if nothing happened. This rule maps an
// active bond to Estranged ("trust broken but feeling remains"), never the terminal Broken (a first betrayal
// wounds, it does not end for good), and leaves a non-existent or already-damaged bond alone. The tests pin that
// only active positive bonds are wounded, and only down to Estranged.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class RomanticWoundPolicyTests
   {
      // Every active positive bond falls to Estranged (recoverable), never Broken: a betrayal is a wound, not a
      // clean end, and the wounded partner must be able to be won back.
      [Test]
      public void GIVEN_an_active_positive_bond_WHEN_wounded_THEN_it_falls_to_Estranged()
      {
         RomanticWoundPolicy.AfterJealousyWound(RomanticStatus.Courting).Should().Be(RomanticStatus.Estranged);
         RomanticWoundPolicy.AfterJealousyWound(RomanticStatus.Intimate).Should().Be(RomanticStatus.Estranged);
         RomanticWoundPolicy.AfterJealousyWound(RomanticStatus.SecretLover).Should().Be(RomanticStatus.Estranged);
         RomanticWoundPolicy.AfterJealousyWound(RomanticStatus.Committed).Should().Be(RomanticStatus.Estranged);
      }

      // A merely budding interest (None/Curious) has no real bond to break, so a jealousy wound leaves it as it
      // is rather than inventing a betrayal out of nothing.
      [Test]
      public void GIVEN_a_budding_or_absent_bond_WHEN_wounded_THEN_it_is_unchanged()
      {
         RomanticWoundPolicy.AfterJealousyWound(RomanticStatus.None).Should().Be(RomanticStatus.None);
         RomanticWoundPolicy.AfterJealousyWound(RomanticStatus.Curious).Should().Be(RomanticStatus.Curious);
      }

      // An already-damaged bond is not pushed further by another wound: Estranged stays Estranged (not driven to
      // Broken by jealousy alone), and Broken stays Broken.
      [Test]
      public void GIVEN_an_already_damaged_bond_WHEN_wounded_THEN_it_is_unchanged()
      {
         RomanticWoundPolicy.AfterJealousyWound(RomanticStatus.Estranged).Should().Be(RomanticStatus.Estranged);
         RomanticWoundPolicy.AfterJealousyWound(RomanticStatus.Broken).Should().Be(RomanticStatus.Broken);
      }
   }
}

// Code written by Gabriel Mailhot, 17/07/2026.
// Romance audit M-R1: RomanticStatus is ordered for narrative progression, not magnitude, but gates compared it
// ordinally. Because Estranged/Broken sort AFTER the positive tiers, "Status >= Courting" and "Status < Intimate"
// let a terminal-negative romance pass every "at least courting / intimate" gate: a Broken NPC read as
// consort-eligible, as romantically bound, and still drew romantic letters. These predicates encode the real
// intent (an ACTIVE positive bond of at least a tier), so this fixture pins that Estranged and Broken are always
// excluded, the exact class of bug the ordinal comparisons caused.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class RomanticStatusExtensionsTests
   {
      // Courting-or-deeper is the "active romance" gate (romantic letters, romantically-bound-to-player). It must
      // include the four positive tiers and exclude the pre-romantic states AND, critically, the terminal ones.
      [Test]
      public void GIVEN_each_status_WHEN_asking_courting_or_deeper_THEN_only_active_positive_tiers_qualify()
      {
         RomanticStatus.Courting.IsCourtingOrDeeper().Should().BeTrue();
         RomanticStatus.Intimate.IsCourtingOrDeeper().Should().BeTrue();
         RomanticStatus.SecretLover.IsCourtingOrDeeper().Should().BeTrue();
         RomanticStatus.Committed.IsCourtingOrDeeper().Should().BeTrue();

         RomanticStatus.None.IsCourtingOrDeeper().Should().BeFalse();
         RomanticStatus.Curious.IsCourtingOrDeeper().Should().BeFalse();
         // The M-R1 core: these sort AFTER Courting in the enum but must never count as an active romance.
         RomanticStatus.Estranged.IsCourtingOrDeeper().Should().BeFalse();
         RomanticStatus.Broken.IsCourtingOrDeeper().Should().BeFalse();
      }

      // Curious-or-deeper is the "any active romance underway" gate: the floor for granting the intimacy relation
      // bonus (regard audit C1). It must include Curious and every positive tier above it, exclude the pre-romantic
      // None, and, the ordinal trap again, exclude the terminal Estranged/Broken so an intimacy tag on a DEAD
      // romance can never unlock the +3 bonus.
      [Test]
      public void GIVEN_each_status_WHEN_asking_curious_or_deeper_THEN_any_active_romance_qualifies_but_never_a_dead_one()
      {
         RomanticStatus.Curious.IsCuriousOrDeeper().Should().BeTrue();
         RomanticStatus.Courting.IsCuriousOrDeeper().Should().BeTrue();
         RomanticStatus.Intimate.IsCuriousOrDeeper().Should().BeTrue();
         RomanticStatus.SecretLover.IsCuriousOrDeeper().Should().BeTrue();
         RomanticStatus.Committed.IsCuriousOrDeeper().Should().BeTrue();

         RomanticStatus.None.IsCuriousOrDeeper().Should().BeFalse();
         // The C1 exploit guard: a terminal romance sorts after Curious in the enum but must not vouch an intimacy bonus.
         RomanticStatus.Estranged.IsCuriousOrDeeper().Should().BeFalse();
         RomanticStatus.Broken.IsCuriousOrDeeper().Should().BeFalse();
      }

      // Intimate-or-deeper is the "close enough for a consort" gate. It must include the three intimate-plus tiers
      // and exclude everything below Intimate AND the terminal states (the consort-eligibility bug).
      [Test]
      public void GIVEN_each_status_WHEN_asking_intimate_or_deeper_THEN_only_intimate_plus_tiers_qualify()
      {
         RomanticStatus.Intimate.IsIntimateOrDeeper().Should().BeTrue();
         RomanticStatus.SecretLover.IsIntimateOrDeeper().Should().BeTrue();
         RomanticStatus.Committed.IsIntimateOrDeeper().Should().BeTrue();

         RomanticStatus.None.IsIntimateOrDeeper().Should().BeFalse();
         RomanticStatus.Curious.IsIntimateOrDeeper().Should().BeFalse();
         RomanticStatus.Courting.IsIntimateOrDeeper().Should().BeFalse();
         // The M-R1 core: a dead romance must never read as consort-eligible.
         RomanticStatus.Estranged.IsIntimateOrDeeper().Should().BeFalse();
         RomanticStatus.Broken.IsIntimateOrDeeper().Should().BeFalse();
      }
   }
}

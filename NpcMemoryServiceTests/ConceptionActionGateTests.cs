// Code written by Gabriel Mailhot, 17/07/2026.
// Romance audit M-G1: impregnation_risk is the most consequential LLM action (it can start a real pregnancy),
// and the bridge now re-validates the content level before executing it, exactly as the prompt gates the
// TEACHING. This fixture pins that the gate opens only at Explicit or above: if it ever loosened, a hallucinated
// emission at Off or Mature, a content level that never offered the action, could conceive a child from nothing.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class ConceptionActionGateTests
   {
      // Off and Mature never TEACH impregnation_risk, so the bridge must never ACT on it there: any emission at
      // those levels is a hallucination and must not conceive.
      [Test]
      public void GIVEN_a_sub_explicit_level_WHEN_gating_conception_THEN_it_is_refused()
      {
         ConceptionActionGate.PermitsConception(AdultContentLevel.Off).Should().BeFalse();
         ConceptionActionGate.PermitsConception(AdultContentLevel.Mature).Should().BeFalse();
      }

      // Explicit is the exact bar at which the prompt teaches the action, and Hardcore is above it: conception is
      // permitted at both, so a legitimate explicit scene still works.
      [Test]
      public void GIVEN_explicit_or_above_WHEN_gating_conception_THEN_it_is_permitted()
      {
         ConceptionActionGate.PermitsConception(AdultContentLevel.Explicit).Should().BeTrue();
         ConceptionActionGate.PermitsConception(AdultContentLevel.Hardcore).Should().BeTrue();
      }
   }
}

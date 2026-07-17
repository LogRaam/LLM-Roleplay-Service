// Code written by Gabriel Mailhot, 17/07/2026.
// Romance audit M-R5: AttractionToPlayer was documented "evolves through conversations" and shown to the LLM but
// only court duels ever moved it, so it sat frozen at 0 (a misleading prompt line) and the SpurnedAdmirer
// jealousy branch (needs >= 55) was effectively dead. This policy is what finally makes conversation move it.
// The tests pin the two things that matter: warm acts build desire, wounds cool it, AND positive gains obey the
// same orientation-plausibility gate as the romantic arc while losses do not (a wound cools desire regardless).

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class AttractionEvolutionPolicyTests
   {
      // Warm acts build desire, intimacy more than a flirt, so the stat the LLM reads actually reflects a
      // deepening bond instead of a permanent zero.
      [Test]
      public void GIVEN_a_warm_act_on_a_plausible_pair_WHEN_evolving_THEN_attraction_rises()
      {
         AttractionEvolutionPolicy.Delta(NotableEventType.Flirt, orientationCompatible: true)
            .Should().Be(AttractionEvolutionPolicy.FlirtGain);
         AttractionEvolutionPolicy.Delta(NotableEventType.Intimacy, orientationCompatible: true)
            .Should().Be(AttractionEvolutionPolicy.IntimacyGain);
         AttractionEvolutionPolicy.IntimacyGain.Should().BeGreaterThan(AttractionEvolutionPolicy.FlirtGain);
      }

      // M-R2 parity: a warm act on an orientation-implausible pair (an LLM hallucination) grows NO attraction,
      // exactly as it grows no romantic-arc status. Otherwise the frozen-stat bug is replaced by a phantom-desire
      // bug where an NPC the orientation rules out reads as attracted.
      [Test]
      public void GIVEN_a_warm_act_on_an_implausible_pair_WHEN_evolving_THEN_attraction_does_not_rise()
      {
         AttractionEvolutionPolicy.Delta(NotableEventType.Flirt, orientationCompatible: false).Should().Be(0);
         AttractionEvolutionPolicy.Delta(NotableEventType.Intimacy, orientationCompatible: false).Should().Be(0);
      }

      // Wounds cool desire REGARDLESS of plausibility: a betrayal or quarrel lowers attraction even for a pair the
      // orientation gate would block from gaining, since a wound is a wound whoever dealt it. Betrayal must sting
      // more than a quarrel.
      [Test]
      public void GIVEN_a_wound_WHEN_evolving_THEN_attraction_falls_regardless_of_plausibility()
      {
         AttractionEvolutionPolicy.Delta(NotableEventType.Betrayal, orientationCompatible: false)
            .Should().Be(AttractionEvolutionPolicy.BetrayalLoss);
         AttractionEvolutionPolicy.Delta(NotableEventType.Conflict, orientationCompatible: true)
            .Should().Be(AttractionEvolutionPolicy.ConflictLoss);
         AttractionEvolutionPolicy.BetrayalLoss.Should().BeLessThan(AttractionEvolutionPolicy.ConflictLoss);
      }

      // A neutral event leaves attraction untouched, so ordinary talk never silently drifts the stat.
      [Test]
      public void GIVEN_a_neutral_event_WHEN_evolving_THEN_attraction_is_unchanged()
      {
         AttractionEvolutionPolicy.Delta(NotableEventType.Collaboration, orientationCompatible: true).Should().Be(0);
         AttractionEvolutionPolicy.Delta(NotableEventType.Other, orientationCompatible: true).Should().Be(0);
      }
   }
}

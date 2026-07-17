// Code written by Gabriel Mailhot, 17/07/2026.
// Jealousy, neglect measure (romance audit M-J4). This decides which recorded events count as the player having
// ATTENDED TO a co-partner, the reset signal for the neglect clock that drives the competition/estrangement loop.
// The bug it locks: the old measure reset on ANY event, so the jealousy reaction the system itself planted (and a
// received letter, logged as Other) kept resetting the clock, and the loop could never escalate for a partner who
// was truly ignored. Get the set wrong one way and jealousy never fires; the other way and it fires on nothing.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class JealousyAttentionPolicyTests
   {
      // Positive, player-directed interactions ARE attention: courting or bonding with a partner resets their
      // neglect clock, exactly as it should, so an actively-loved partner never reads as neglected.
      [Test]
      public void GIVEN_a_positive_interaction_WHEN_asking_if_it_is_attention_THEN_it_counts()
      {
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Flirt).Should().BeTrue();
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Intimacy).Should().BeTrue();
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Collaboration).Should().BeTrue();
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Agreement).Should().BeTrue();
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.FirstMeeting).Should().BeTrue();
      }

      // The M-J4 core: a jealousy reaction the system planted is NOT attention. If it counted, planting the
      // reaction would reset the very clock that decides the next escalation, and the arc could never advance.
      [Test]
      public void GIVEN_a_jealousy_reaction_WHEN_asking_if_it_is_attention_THEN_it_does_not_count()
      {
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Jealousy).Should().BeFalse();
      }

      // Other holds received letters (and ambiguous notes), so it must NOT count: a partner merely being written
      // to is not the player attending to them, the second false-attention source the audit named.
      [Test]
      public void GIVEN_a_received_letter_or_other_WHEN_asking_if_it_is_attention_THEN_it_does_not_count()
      {
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Other).Should().BeFalse();
      }

      // Negative and involuntary events are never attention: a quarrel, a betrayal, a captivity, or a farewell
      // must not reset the neglect clock as if the partner had been warmly attended to.
      [Test]
      public void GIVEN_a_negative_or_involuntary_event_WHEN_asking_if_it_is_attention_THEN_it_does_not_count()
      {
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Conflict).Should().BeFalse();
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Betrayal).Should().BeFalse();
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Confrontation).Should().BeFalse();
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Captivity).Should().BeFalse();
         JealousyAttentionPolicy.CountsAsAttention(NotableEventType.Farewell).Should().BeFalse();
      }
   }
}

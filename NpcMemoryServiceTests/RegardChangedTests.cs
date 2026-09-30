// Code written by Gabriel Mailhot, 29/09/2026.
// tashmetu (Nexus, 29/09/2026): a player of his bridge wanted a character's regard to move the game's own relation. CR
// keeps the two apart on purpose (the native relation is clan-wide, and regard's pacing is an anti-farming rule), and
// offers a notice instead: every change of regard, from the one place regard changes, with why. A listener that misses
// changes (a letter, a quest) would drift out of step; one that hears a non-change would act on nothing; one that
// throws must never undo the change or silence the others.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NpcMemoryService.Core.Standing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   [NonParallelizable] // the event is static: a parallel test's changes would reach these listeners
   public class RegardChangedTests
   {
      private static NpcProfile Profile(int regard) => new() {Id = "npc_1", Name = "Rhagaea", Clan = "c", Faction = "f", ReputationWithPlayer = regard};

      // The notice a listener acts on: who, from what, to what, and why.
      [Test]
      public void GIVEN_a_listener_WHEN_regard_changes_THEN_it_hears_who_before_after_and_why()
      {
         var heard = new List<RegardChange>();
         void Listen(RegardChange c) => heard.Add(c);
         ProfileMutator.RegardChanged += Listen;
         try
         {
            ProfileMutator.ApplyReputationDelta(Profile(14), 1, RegardCause.Letter);
         }
         finally { ProfileMutator.RegardChanged -= Listen; }

         heard.Should().ContainSingle();
         heard[0].NpcId.Should().Be("npc_1");
         heard[0].Before.Should().Be(14);
         heard[0].After.Should().Be(15);
         heard[0].Cause.Should().Be(RegardCause.Letter);
      }

      // Regard already at the ceiling does not move, and a listener must not be told it did.
      [Test]
      public void GIVEN_regard_at_the_ceiling_WHEN_it_would_rise_THEN_nothing_is_heard()
      {
         var heard = new List<RegardChange>();
         void Listen(RegardChange c) => heard.Add(c);
         ProfileMutator.RegardChanged += Listen;
         try
         {
            ProfileMutator.ApplyReputationDelta(Profile(100), 5, RegardCause.Quest);
         }
         finally { ProfileMutator.RegardChanged -= Listen; }

         heard.Should().BeEmpty();
      }

      // One listener's fault is its own: the change stands, and the next listener still hears it.
      [Test]
      public void GIVEN_a_listener_that_throws_WHEN_regard_changes_THEN_the_change_stands_and_others_still_hear_it()
      {
         var heard = new List<RegardChange>();
         void Throws(RegardChange c) => throw new System.InvalidOperationException("a bad listener");
         void Listen(RegardChange c) => heard.Add(c);
         ProfileMutator.RegardChanged += Throws;
         ProfileMutator.RegardChanged += Listen;
         NpcProfile p = Profile(0);
         try
         {
            ProfileMutator.ApplyReputationDelta(p, -3, RegardCause.Violence);
         }
         finally
         {
            ProfileMutator.RegardChanged -= Throws;
            ProfileMutator.RegardChanged -= Listen;
         }

         p.ReputationWithPlayer.Should().Be(-3);
         heard.Should().ContainSingle();
      }
   }
}

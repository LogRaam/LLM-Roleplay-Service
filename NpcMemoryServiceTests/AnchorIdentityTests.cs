// Code written by Gabriel Mailhot, 16/09/2026.
// What these pin: that a memory somebody means to come back to can be found again, and found UNIQUELY.
//
// Two defects in the audit of 15/09/2026 had one cause: a memory was identified by its VALUE, and the value
// of a conversation anchor is "Spoke with {player}." on today's date — neither unique nor stable.
//
//   TWO CONVERSATIONS ON ONE DAY produced byte-identical anchors. The summary queue deduplicated the second
//   job away as a repeat, and the first conversation's summary was then written onto the SECOND
//   conversation's event by a FindLastIndex. One memory misattributed, one conversation never enriched.
//
//   ENRICHMENT VERSUS COMPRESSION. A summarizer replaces an anchor in place, at its original index, while a
//   compression pass is working from a snapshot taken before that. Writing the snapshot back restored the
//   plain line over the rich summary, with the job already marked done and never retried.
//
// An identity settles both. These pin the lookup that decides it — including the case where falling back to
// a value match would be WORSE than finding nothing, which is the whole point.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class AnchorIdentityTests
   {
      // THE REPORTED CASE. Two sittings on one day, identical by value; the FIRST conversation's summary must
      // land on the FIRST conversation's event, which value matching could never do.
      [Test]
      public void GIVEN_two_identical_anchors_on_one_day_WHEN_looked_up_by_identity_THEN_each_finds_its_own()
      {
         NotableEvent first = Anchor().Identified();
         NotableEvent second = Anchor().Identified();
         NpcProfile npc = With(first, second);

         npc.IndexOfAnchor(first.Id, first).Should().Be(0);
         npc.IndexOfAnchor(second.Id, second).Should().Be(1);
      }

      // And the two identities must actually differ — an Identified() that reused a value would look like a
      // fix and change nothing.
      [Test]
      public void GIVEN_two_anchors_identified_separately_WHEN_compared_THEN_their_identities_differ()
      {
         Anchor().Identified().Id.Should().NotBe(Anchor().Identified().Id);
      }

      // WHAT VALUE MATCHING DID, kept as the control: the last equal event wins, which is the second
      // conversation. Without this the fix above could pass for the wrong reason.
      [Test]
      public void GIVEN_two_identical_anchors_WHEN_looked_up_by_value_alone_THEN_the_later_one_is_found()
      {
         NpcProfile npc = With(Anchor(), Anchor());

         npc.IndexOfAnchor(null, Anchor()).Should().Be(1, "this is the behaviour identities exist to replace");
      }

      // AN IDENTIFIED ANCHOR THAT IS GONE IS GONE. Falling back to a value match here would find a DIFFERENT
      // conversation's event and write this summary onto it — the very defect being fixed, reintroduced by a
      // well-meaning fallback. Missing must mean missing.
      [Test]
      public void GIVEN_an_identified_anchor_that_has_vanished_WHEN_looked_up_THEN_nothing_is_found()
      {
         NpcProfile npc = With(Anchor(), Anchor());

         npc.IndexOfAnchor("an-identity-no-event-here-carries", Anchor()).Should().Be(-1);
      }

      // OLDER SAVES still work. Every memory written before identities existed has none, and those callers
      // must keep the behaviour they have rather than silently finding nothing.
      [Test]
      public void GIVEN_an_anchor_from_an_older_save_WHEN_looked_up_THEN_the_value_match_still_serves()
      {
         var older = new NotableEvent(10, NotableEventType.Other, "Spoke with Arwa.");

         With(new NotableEvent(9, NotableEventType.Other, "something else"), older)
            .IndexOfAnchor(null, older).Should().Be(1);
      }

      // A profile with no events at all must answer -1 rather than throw: this runs on a background drain,
      // where an exception is a lost memory and a log line nobody reads.
      [Test]
      public void GIVEN_nothing_to_search_WHEN_looked_up_THEN_it_answers_plainly()
      {
         new NpcProfile {Id = "n", Name = "Test", Faction = "Vlandia", Clan = "dey Meroc"}
            .IndexOfAnchor("anything", Anchor()).Should().Be(-1);
      }

      // AUDIT #3. Compression runs off the main thread and used to write back as Clear() then AddRange, while
      // the main thread read the same list to build a prompt. Between those two calls the character's whole
      // past is GONE — a reader landing there enumerates an empty history, or throws "Collection was modified"
      // into a background task whose failure a player sees only as a memory that never arrived.
      //
      // A reference store has no such window. This pins the property that matters: a reader holding the OLD
      // list still has a whole history, not a half-emptied one.
      [Test]
      public void GIVEN_a_reader_holding_the_history_WHEN_it_is_replaced_THEN_what_they_hold_stays_whole()
      {
         NpcProfile npc = With(Anchor(), Anchor());
         List<NotableEvent> readerHolds = npc.Events;

         npc.ReplaceEvents(new List<NotableEvent> {Anchor()});

         readerHolds.Count.Should().Be(2, "a reader mid-enumeration must never see the past emptied under it");
         npc.Events.Count.Should().Be(1);
      }

      // And the swap must really take: a reader arriving AFTER it sees the new history, not the old one.
      [Test]
      public void GIVEN_a_reader_arriving_after_the_swap_WHEN_it_reads_THEN_it_sees_the_new_history()
      {
         NpcProfile npc = With(Anchor(), Anchor());

         npc.ReplaceEvents(new List<NotableEvent> {new(11, NotableEventType.Other, "the folded past")});

         npc.Events.Should().ContainSingle().Which.summary.Should().Be("the folded past");
      }

      // A null replacement is a caller error on a background thread; it must leave a usable empty history
      // rather than a null nobody checks for.
      [Test]
      public void GIVEN_a_null_replacement_WHEN_applied_THEN_the_history_is_empty_and_not_null()
      {
         NpcProfile npc = With(Anchor());

         npc.ReplaceEvents(null);

         npc.Events.Should().NotBeNull().And.BeEmpty();
      }

      #region private

      private static NotableEvent Anchor() => new(10, NotableEventType.Other, "Spoke with Arwa.");

      private static NpcProfile With(params NotableEvent[] events)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test", Faction = "Vlandia", Clan = "dey Meroc"};

         npc.Events.AddRange(new List<NotableEvent>(events));

         return npc;
      }

      #endregion
   }
}

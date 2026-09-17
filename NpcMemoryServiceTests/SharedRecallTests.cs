// Code written by Gabriel Mailhot, 16/09/2026.
// What these pin: what a character may be shown to remember IN FRONT OF OTHER PEOPLE.
//
// Two things meet in this type, and they used to live apart.
//
// THE BOUND came from WitnessMemoryDigest (23/08/2026): how much of a witness's own memory a group scene
// shows. Too little and a witness forgets salient past — a player reported a companion answering as if a
// prior agreement had never happened. Too much and the group prompt bloats with everyone's full history.
// Those tests are carried over here, with their stakes, because the rule did not change when it moved.
//
// THE FILTER came from a private method in a chat view model: a memory the player asked a character to keep
// must never travel a prompt that covers several people. It was correct and it was kept by CARE — a comment
// and whoever read it. That very nearly failed while the council was being wired, where reading
// profile.Events directly would have been shorter, would have compiled, and would have put a confidence in
// front of a whole table.
//
// So the two are now one function, and the only way to obtain a shareable recall. A field typed SharedRecall
// cannot be handed raw event text: the leak stops compiling instead of stopping in review.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Memory;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class SharedRecallTests
   {
      #region the filter — what must never leave its holder

      // THE WHOLE REASON THE TYPE EXISTS. A confidence reaches its HOLDER's own prompt through the ordinary
      // history, marked as a confidence. It must never reach a recall, because a recall is read by a room.
      [Test]
      public void GIVEN_a_memory_held_in_confidence_WHEN_a_recall_is_made_THEN_it_is_not_in_it()
      {
         SharedRecall? recall = SharedRecall.From(new List<NotableEvent> {
            Event("we spoke of the harvest"),
            Private("they asked me to keep the bastard child secret")
         }, 8);

         recall!.Text.Should().Contain("harvest");
         recall.Text.Should().NotContain("bastard child");
      }

      // Somebody whose EVERY memory is a confidence has nothing to say in company — and must produce no
      // recall at all rather than an empty one, which would read in the prompt as invented history.
      [Test]
      public void GIVEN_every_memory_is_a_confidence_WHEN_a_recall_is_made_THEN_there_is_none()
      {
         SharedRecall.From(new List<NotableEvent> {Private("a"), Private("b")}, 8).Should().BeNull();
      }

      // The cap counts what is SHAREABLE, not what exists: confidences must not consume the allowance and
      // silently push real memories out of a recall that had room for them.
      [Test]
      public void GIVEN_confidences_among_the_memories_WHEN_a_recall_is_made_THEN_they_do_not_spend_the_allowance()
      {
         SharedRecall? recall = SharedRecall.From(new List<NotableEvent> {
            Event("OLDEST"), Private("secret one"), Private("secret two"), Event("NEWEST")
         }, 2);

         recall!.Text.Should().Be("OLDEST; NEWEST");
      }

      #endregion

      #region the bound — carried over from WitnessMemoryDigest, stakes and all

      // Under the cap, the character recalls EVERYTHING, in order: a short history must not be trimmed at all,
      // or somebody with only a few memories would still seem to forget one.
      [Test]
      public void GIVEN_fewer_than_the_cap_WHEN_a_recall_is_made_THEN_all_of_it_is_kept_in_order()
      {
         SharedRecall.From(new List<NotableEvent> {Event("met at Pravend"), Event("agreed to the signal word")}, 8)!
                     .Text.Should().Be("met at Pravend; agreed to the signal word");
      }

      // Over the cap, the OLDEST kept line (the compression-folded summary, or the first meeting) must survive
      // alongside the most recent. That is the whole point of a compression-aware cap: the durable past is
      // never the thing that gives way when somebody has a long history.
      [Test]
      public void GIVEN_more_than_the_cap_WHEN_a_recall_is_made_THEN_the_oldest_and_newest_survive_and_a_middle_gives_way()
      {
         SharedRecall? recall = SharedRecall.From(new List<NotableEvent> {
            Event("OLDEST folded summary"), Event("b"), Event("c"), Event("d"), Event("NEWEST")
         }, 3);

         recall!.Text.Should().Be("OLDEST folded summary; d; NEWEST");
      }

      // Blank summaries are extraction noise. Letting them through would spend cap slots and print empty
      // "; ;" gaps in the recall line.
      [Test]
      public void GIVEN_blank_summaries_WHEN_a_recall_is_made_THEN_they_are_ignored()
      {
         SharedRecall.From(new List<NotableEvent> {Event(" "), Event("real memory"), Event("")}, 8)!
                     .Text.Should().Be("real memory");
      }

      // No content means NO recall: the prompt must not mint an empty "X remembers:" line, which reads as
      // invented history.
      [Test]
      public void GIVEN_no_real_content_WHEN_a_recall_is_made_THEN_there_is_none()
      {
         SharedRecall.From(null, 8).Should().BeNull();
         SharedRecall.From(new List<NotableEvent>(), 8).Should().BeNull();
         SharedRecall.From(new List<NotableEvent> {Event(" "), Event("")}, 8).Should().BeNull();
      }

      // A non-positive cap is a caller error, or a mis-set tuning value. It must never throw mid-turn nor
      // return a partial line; it simply yields no recall.
      [Test]
      public void GIVEN_a_non_positive_cap_WHEN_a_recall_is_made_THEN_there_is_none()
      {
         SharedRecall.From(new List<NotableEvent> {Event("a"), Event("b")}, 0).Should().BeNull();
      }

      // A null in the list is a storage accident, not a reason to end a player's turn.
      [Test]
      public void GIVEN_a_null_among_the_memories_WHEN_a_recall_is_made_THEN_it_is_stepped_over()
      {
         SharedRecall.From(new List<NotableEvent> {null!, Event("real memory")}, 8)!
                     .Text.Should().Be("real memory");
      }

      #endregion

      #region private

      private static NotableEvent Event(string summary) => new(1, NotableEventType.Other, summary);

      private static NotableEvent Private(string summary)
         => new(1, NotableEventType.Other, summary) {IsPrivate = true};

      #endregion
   }
}

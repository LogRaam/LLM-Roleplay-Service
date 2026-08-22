// Code written by Gabriel Mailhot, 15/08/2026.
// ActionTagSanitizer is the deterministic safety net that drops an [ACTION] whose type is really an [EVENT] TYPE
// word (first_meeting, intimacy, ...). Both composition paths (integrated/direct and Prose+Interpreter) occasionally
// emit one as an [ACTION] - a grok format lapse the prompt guard cannot guarantee against - and the game would only
// refuse it as an unknown verb, leaving noise in the transcript and the log. It runs inside SectionResponseParser so
// BOTH paths are protected equally (the interpret step is not penalised for a lapse the direct model shares). These
// tests pin that it strips exactly the event-type words whatever their casing/separators, keeps every real action
// (including the flow-control end_conversation, which is NOT an event type), and never allocates or drops on the
// clean common path.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ActionTagSanitizerTests
   {
      // The exact bug (rp_bench, 2026-08-15): grok emitted [ACTION] first_meeting and [ACTION] intimacy - event
      // types masquerading as actions. Left in, they reach the bridge as unknown verbs and clutter the record. The
      // sanitizer must remove them so only real actions survive.
      [Test]
      public void GIVEN_actions_that_are_really_event_types_WHEN_sanitized_THEN_they_are_dropped()
      {
         var actions = new List<GameAction>
         {
            new GameAction {Type = "first_meeting"},
            new GameAction {Type = "intimacy"},
            new GameAction {Type = "confrontation"}
         };

         ActionTagSanitizer.StripEventTypeActions(actions).Should().BeEmpty();
      }

      // The whole point is a SURGICAL strip: a real action verb must never be caught. change_relation and give_gold
      // are the bread-and-butter signals; dropping one would silence a regard change or a gift the NPC plainly meant.
      [Test]
      public void GIVEN_real_action_verbs_WHEN_sanitized_THEN_they_all_survive()
      {
         var actions = new List<GameAction>
         {
            new GameAction {Type = "change_relation"},
            new GameAction {Type = "give_gold"},
            new GameAction {Type = "harm_prisoner"}
         };

         ActionTagSanitizer.StripEventTypeActions(actions).Should().HaveCount(3);
      }

      // end_conversation is a chat-flow control, NOT a GameActionCatalog entry and NOT an event type. It is the very
      // signal the interpret step already tends to MISS, so the sanitizer must never be the thing that drops it: if it
      // did, the safety net would itself delete scene-closing signals.
      [Test]
      public void GIVEN_end_conversation_WHEN_sanitized_THEN_it_survives()
      {
         var actions = new List<GameAction> {new GameAction {Type = "end_conversation"}};

         ActionTagSanitizer.StripEventTypeActions(actions).Should().ContainSingle(a => a.Type == "end_conversation");
      }

      // The type string arrives in whatever shape the model wrote it - "First-Meeting", "FirstMeeting", "INTIMACY" -
      // and NormalizeActionType only unifies separators, not case. The sanitizer must canonicalise both, or a
      // capitalised or hyphenated event-type-as-action would slip straight through the net.
      [Test]
      public void GIVEN_event_type_actions_in_mixed_casing_and_separators_WHEN_sanitized_THEN_they_are_still_dropped()
      {
         var actions = new List<GameAction>
         {
            new GameAction {Type = "First-Meeting"},
            new GameAction {Type = "FirstMeeting"},
            new GameAction {Type = "INTIMACY"},
            new GameAction {Type = "change_relation"}
         };

         IReadOnlyList<GameAction> kept = ActionTagSanitizer.StripEventTypeActions(actions)!;

         kept.Should().ContainSingle();
         kept[0].Type.Should().Be("change_relation");
      }

      // A mixed reply (one good action, one hallucinated event-type action) is the realistic case; the sanitizer must
      // keep the good and drop only the bad, preserving order so nothing else about the reply shifts.
      [Test]
      public void GIVEN_a_mix_of_real_and_event_type_actions_WHEN_sanitized_THEN_only_the_real_ones_remain_in_order()
      {
         var actions = new List<GameAction>
         {
            new GameAction {Type = "change_relation"},
            new GameAction {Type = "first_meeting"},
            new GameAction {Type = "give_gold"}
         };

         IReadOnlyList<GameAction> kept = ActionTagSanitizer.StripEventTypeActions(actions)!;

         kept.Should().HaveCount(2);
         kept[0].Type.Should().Be("change_relation");
         kept[1].Type.Should().Be("give_gold");
      }

      // The common path is a clean reply with nothing to strip: the sanitizer must return the SAME instance untouched
      // (no needless allocation on every single parse), and an empty or null list must not throw.
      [Test]
      public void GIVEN_a_clean_or_empty_list_WHEN_sanitized_THEN_it_is_returned_unchanged()
      {
         var clean = new List<GameAction> {new GameAction {Type = "change_relation"}};
         ActionTagSanitizer.StripEventTypeActions(clean).Should().BeSameAs(clean);

         var empty = new List<GameAction>();
         ActionTagSanitizer.StripEventTypeActions(empty).Should().BeSameAs(empty);
      }
   }
}

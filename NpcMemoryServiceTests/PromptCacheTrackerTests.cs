// Code written by Gabriel Mailhot, 15/09/2026.
// What these pin: that the cache report compares a character against HIMSELF, and says so honestly when
// it has nothing to compare against yet.
//
// Choosing which two prefixes to compare was got wrong twice in one evening, and both times the report was
// worse than no report at all:
//
//   1. One previous prefix for everything. Prose and interpreter alternate every turn, so it compared GLM
//      against Grok and printed "100.0% discarded" on every line while both were perfectly stable.
//   2. Keyed by model. In a group scene the turn is routed to another lord, whose prompt is legitimately
//      different, and it printed "81.5% discarded" for a conversation in which nothing had gone wrong.
//
// cr.wire is read by players and sent to us as evidence. A verdict that is wrong in the normal case is not
// a caveat; it is an instrument that manufactures bug reports. These tests are the reason it now cannot.

#region

using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PromptCacheTrackerTests
   {
      private const string Prose = "glm";
      private const string Interp = "grok";
      private const string Sora = "npc_sora";
      private const string Temion = "npc_temion";

      // THE STEADY STATE, and the only pair whose sameness means anything: one character, one model, two
      // consecutive turns. This is the verdict a player should see on turn two.
      [Test]
      public void GIVEN_the_same_character_twice_WHEN_observed_THEN_the_prefix_is_reported_as_fully_reused()
      {
         var tracker = new PromptCacheTracker();

         tracker.Observe(Prose, Sora, "SORA'S PROMPT");

         tracker.Observe(Prose, Sora, "SORA'S PROMPT").Verdict.Should().Be(PrefixVerdict.Whole);
      }

      // FAILURE 1, as it actually printed. The two callers of a single turn must not be measured against
      // each other: their prompts have nothing in common and never will.
      [Test]
      public void GIVEN_prose_and_interpreter_alternating_WHEN_observed_THEN_neither_is_blamed_for_the_other()
      {
         var tracker = new PromptCacheTracker();

         tracker.Observe(Prose, Sora, "SORA'S PROSE PROMPT");
         tracker.Observe(Interp, Sora, "THE INTERPRETER CATALOGUE");

         tracker.Observe(Prose, Sora, "SORA'S PROSE PROMPT").Verdict.Should().Be(PrefixVerdict.Whole);
         tracker.Observe(Interp, Sora, "THE INTERPRETER CATALOGUE").Verdict.Should().Be(PrefixVerdict.Whole);
      }

      // FAILURE 2, as it actually printed: the player addressed another lord, and a healthy conversation was
      // reported as an 81.5% loss. A character met for the first time is a WRITE, not a loss.
      [Test]
      public void GIVEN_the_turn_is_routed_to_another_character_WHEN_observed_THEN_it_reads_as_a_first_request()
      {
         var tracker = new PromptCacheTracker();

         tracker.Observe(Prose, Sora, "SORA'S PROMPT");

         tracker.Observe(Prose, Temion, "TEMION'S PROMPT").Verdict.Should().Be(PrefixVerdict.NoPrevious);
      }

      // And coming BACK to the first character finds his own prefix waiting, not the other's — which is what
      // makes the instrument usable in the group scenes where the cache matters most.
      [Test]
      public void GIVEN_the_player_returns_to_the_first_character_WHEN_observed_THEN_his_own_prefix_is_the_one_compared()
      {
         var tracker = new PromptCacheTracker();

         tracker.Observe(Prose, Sora, "SORA'S PROMPT");
         tracker.Observe(Prose, Temion, "TEMION'S PROMPT");

         tracker.Observe(Prose, Sora, "SORA'S PROMPT").Verdict.Should().Be(PrefixVerdict.Whole);
      }

      // THE DEFECT MUST STILL BE VISIBLE. All of the above is worthless if the tracker has merely become
      // incapable of reporting bad news: one character whose own prefix moved is exactly what cr.wire is for.
      [Test]
      public void GIVEN_one_characters_own_prefix_moved_WHEN_observed_THEN_the_loss_is_still_reported()
      {
         var tracker = new PromptCacheTracker();

         tracker.Observe(Prose, Sora, "SORA'S PROMPT, WITNESSES: none");

         PrefixComparison second = tracker.Observe(Prose, Sora, "SORA'S PROMPT, WITNESSES: Temion");

         second.Verdict.Should().Be(PrefixVerdict.Broken);
         second.DivergesAt.Should().Be("SORA'S PROMPT, WITNESSES: ".Length);
      }

      // An unattributed call — a summarizer, an extractor — is its own bucket: it can neither be blamed on a
      // character nor blame one.
      [Test]
      public void GIVEN_a_call_with_no_subject_WHEN_observed_THEN_it_neither_blames_nor_is_blamed()
      {
         var tracker = new PromptCacheTracker();

         tracker.Observe(Prose, Sora, "SORA'S PROMPT");
         tracker.Observe(Prose, null, "A SUMMARIZER PROMPT").Verdict.Should().Be(PrefixVerdict.NoPrevious);

         tracker.Observe(Prose, Sora, "SORA'S PROMPT").Verdict.Should().Be(PrefixVerdict.Whole);
         tracker.TrackedCount.Should().Be(2);
      }
   }
}

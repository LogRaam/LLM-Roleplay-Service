// Code written by Gabriel Mailhot, 15/07/2026.
// Pins the fix for the reported in-game bug: a reasoning model (nvidia/nemotron, Reasoning Effort High) emits
// its chain-of-thought INLINE in the content as <think>...</think> instead of the separate reasoning field.
// Bannerlord's renderer hides the tags as markup, so the bare trace ("The user wants me to write a memory
// line...") was displayed as the NPC's reply and stored as the NPC's memory. The flagship case is the memory
// screenshot: a trace cut off by the token limit (no closing tag) must strip to EMPTY, feeding the existing
// bigger-budget retry, never be stored as prose.

#region

using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ReasoningTraceStripperTests
   {
      // The most common healthy shape: the model thinks, closes the tag, then writes the actual reply.
      // Only the reply may survive.
      [Test]
      public void GIVEN_a_complete_think_block_before_the_reply_WHEN_stripping_THEN_only_the_reply_remains()
      {
         ReasoningTraceStripper.Strip(
               "<think>The user wants me to write a memory line from Mesui's perspective.</think>\n" +
               "I told Huan Yi that loyalty bought with pardons is loyalty rented, not owned.")
            .Should().Be("I told Huan Yi that loyalty bought with pardons is loyalty rented, not owned.");
      }

      // The reported memory screenshot: the trace ate the whole token budget, so the closing tag never
      // arrived. Everything from the opening tag on is trace; stripping to EMPTY is what routes the reply
      // into the caller's bigger-budget retry instead of the memory store.
      [Test]
      public void GIVEN_a_trace_cut_off_before_its_closing_tag_WHEN_stripping_THEN_the_result_is_empty()
      {
         ReasoningTraceStripper.Strip(
               "<think>The user wants me to write a memory line from Mesui's perspective, capturing what she " +
               "remembers of this conversation with Huan Yi. Let me summarize what happened: 1. Huan Yi")
            .Should().BeEmpty();
      }

      // Some templates strip the OPENING tag themselves and the trace arrives headless, ending in a stray
      // closing tag. Everything up to the last closing tag is trace; the reply follows it.
      [Test]
      public void GIVEN_a_headless_trace_ending_in_a_stray_closing_tag_WHEN_stripping_THEN_the_text_after_it_survives()
      {
         ReasoningTraceStripper.Strip(
               "Let me analyze what happened here and pick the key points.</think>She kept her word after all.")
            .Should().Be("She kept her word after all.");
      }

      // Baseline regression guard: the by-far-most-common reply carries no trace tags and must flow through
      // VERBATIM, untrimmed, so the ordinary path is byte-for-byte what it was before the fix.
      [Test]
      public void GIVEN_an_ordinary_reply_without_trace_tags_WHEN_stripping_THEN_it_is_returned_verbatim()
      {
         const string reply = "  [DIALOGUE]Well met, wanderer.[/DIALOGUE]\n[ACTION]change_relation: 1[/ACTION]  ";

         ReasoningTraceStripper.Strip(reply).Should().Be(reply);
      }

      // The tag family varies by model: <thinking> (Claude-style templates), <reasoning>, <thought>. Each
      // variant missed would resurface the exact reported bug for that model family only.
      [TestCase("<thinking>pondering</thinking>")]
      [TestCase("<reasoning>pondering</reasoning>")]
      [TestCase("<thought>pondering</thought>")]
      [TestCase("<THINK>pondering</THINK>")]
      public void GIVEN_the_known_tag_variants_in_any_case_WHEN_stripping_THEN_each_is_removed(string trace)
      {
         ReasoningTraceStripper.Strip(trace + "The reply.").Should().Be("The reply.");
      }

      // A model can think in several bursts; every complete block goes, whatever surrounds it stays.
      [Test]
      public void GIVEN_multiple_trace_blocks_around_the_reply_WHEN_stripping_THEN_all_blocks_go_and_the_reply_stays()
      {
         ReasoningTraceStripper.Strip(
               "<think>first pass</think>Half the reply <think>second pass</think>and the rest.")
            .Should().Be("Half the reply and the rest.");
      }

      // The discriminator's negative space: prose angle-brackets that are not one of the trace tag names,
      // and the mod's own square-bracket machinery, must never be mistaken for a trace.
      [Test]
      public void GIVEN_angle_brackets_that_are_not_trace_tags_WHEN_stripping_THEN_the_text_is_untouched()
      {
         const string reply = "I think of you often. He wrote <unreadable> in the margin.";

         ReasoningTraceStripper.Strip(reply).Should().Be(reply);
      }

      // Null and empty inputs come from failure envelopes upstream; the stripper must stay total.
      [Test]
      public void GIVEN_null_or_empty_content_WHEN_stripping_THEN_empty_comes_back_without_throwing()
      {
         ReasoningTraceStripper.Strip(null).Should().BeEmpty();
         ReasoningTraceStripper.Strip(string.Empty).Should().BeEmpty();
      }
   }
}

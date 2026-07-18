// Code written by Gabriel Mailhot, 18/07/2026.
// Player report: un-tagged meta-reasoning ("Looking at the context: ...", "The user wants me to write a
// memory line ...", "Let me analyze what happened: 1. ...") sailed through ReasoningTraceStripper — which
// only removes TAGGED traces — and ended up DISPLAYED as the NPC's spoken line or STORED as the NPC's
// memory. MetaReasoningGuard detects that opening shape so every consumer can reject it, and the parser's
// dialogue fallback silences it instead of reading the model's homework aloud. These tests pin the
// detected openers, the legitimate prose that must NEVER trip the guard, the silent fallback, and the
// summarizer's refusal to store a meta "memory".

#region

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.Captivity;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class MetaReasoningGuardTests
   {
      // ── The guard itself ────────────────────────────────────────────────

      [TestCase("Looking at the context: the player is a lord of Vlandia.")]
      [TestCase("Let me analyze what happened: 1. the player greeted me.")]
      [TestCase("Let me summarize the exchange for the memory line.")]
      [TestCase("Let me review the conversation before writing.")]
      [TestCase("Let me break down what the player wants.")]
      [TestCase("Let me consider what the NPC would remember.")]
      [TestCase("The user wants me to write a memory line from Mesui's perspective.")]
      [TestCase("The user asked me to summarize this conversation.")]
      [TestCase("The user is asking for a one-sentence memory.")]
      [TestCase("The player might be: 1. a noble 2. a bandit.")]
      [TestCase("The player is trying to recruit me.")]
      [TestCase("The player seems to be testing my loyalty.")]
      [TestCase("Wait, the player said they were at war with us.")]
      [TestCase("   Looking at the context, with leading whitespace.")]
      [TestCase("looking at the context: lowercase still counts.")]
      public void GIVEN_text_opening_with_meta_reasoning_WHEN_checked_THEN_it_is_detected(string text)
         => MetaReasoningGuard.IsMetaReasoning(text).Should().BeTrue();

      // The false-positive bar: legitimate in-character prose that merely starts near an opener must
      // pass. "Let me think..." is RP, not analysis — the opener list deliberately excludes it.
      [TestCase("Let me think... yes, I remember you now.")]
      [TestCase("Let me see those horses of yours, then.")]
      [TestCase("The player and I spoke of horses until dusk.")]
      [TestCase("I remember the horses you promised me.")]
      [TestCase("Aye, the road was long, but the ale is good here.")]
      public void GIVEN_legitimate_prose_WHEN_checked_THEN_it_is_not_flagged(string text)
         => MetaReasoningGuard.IsMetaReasoning(text).Should().BeFalse();

      [TestCase(null)]
      [TestCase("")]
      [TestCase("   ")]
      public void GIVEN_empty_text_WHEN_checked_THEN_it_is_not_flagged(string? text)
         => MetaReasoningGuard.IsMetaReasoning(text).Should().BeFalse();

      // ── The dialogue fallback ───────────────────────────────────────────

      // The reported leak: a reply that is NOTHING but the model thinking out loud used to become the
      // NPC's spoken line. Now it yields NO dialogue — the host shows no parasite bubble.
      [Test]
      public void GIVEN_a_reply_that_is_pure_meta_reasoning_WHEN_parsed_THEN_the_dialogue_is_empty()
      {
         var parser = new SectionResponseParser();

         parser.Parse("Looking at the context: the player asked about horses. 1. I should answer in character.")
               .Dialogue.Should().BeEmpty();
      }

      // A valid [DIALOGUE] block is never touched by the guard: the meta preamble is simply not part of
      // the spoken line, exactly as before.
      [Test]
      public void GIVEN_meta_reasoning_before_a_valid_dialogue_block_WHEN_parsed_THEN_the_dialogue_is_preserved()
      {
         var parser = new SectionResponseParser();

         parser.Parse("Let me analyze what happened.\n[DIALOGUE]Aye, the horses are yours, as agreed.[/DIALOGUE]")
               .Dialogue.Should().Be("Aye, the horses are yours, as agreed.");
      }

      // Legitimate RP opening the reply (no tags at all) must survive the fallback untouched — this is
      // the false-positive guard at the parser level.
      [Test]
      public void GIVEN_a_tagless_reply_that_is_legitimate_prose_WHEN_parsed_THEN_it_is_kept_as_dialogue()
      {
         var parser = new SectionResponseParser();

         parser.Parse("Let me think... yes, I remember you now. The horse trader from Baltakhand.")
               .Dialogue.Should().Be("Let me think... yes, I remember you now. The horse trader from Baltakhand.");
      }

      // ── The memory-line guard ───────────────────────────────────────────

      // The reported corruption: a meta "memory line" was STORED as the NPC's memory and re-injected into
      // every later prompt. The summarizer now returns null, so the caller keeps the plain base summary.
      [Test]
      public async Task GIVEN_a_meta_reasoning_summary_WHEN_summarizing_THEN_no_memory_line_is_returned()
      {
         var summarizer = new ConversationSummarizer(
            new StubLlmClient("The user wants me to write a memory line from Mesui's perspective."));

         string? line = await summarizer.SummarizeAsync(Npc(), "You: hello.\nMesui: well met.", "Huan Yi");

         line.Should().BeNull();
      }

      [Test]
      public async Task GIVEN_a_genuine_memory_line_WHEN_summarizing_THEN_it_is_returned()
      {
         var summarizer = new ConversationSummarizer(
            new StubLlmClient("The rider from Baltakhand kept their word about the horses."));

         string? line = await summarizer.SummarizeAsync(Npc(), "You: the horses?\nMesui: delivered.", "Huan Yi");

         line.Should().Be("The rider from Baltakhand kept their word about the horses.");
      }

      // The captive scene carries the SAME guard as the ordinary conversation summarizer, and needs its
      // own pin: a captive scene is the one memory the player cannot re-open and correct by talking again,
      // so a stored meta "memory" there is the most durable corruption of the two.
      [Test]
      public async Task GIVEN_a_meta_reasoning_scene_summary_WHEN_summarizing_THEN_no_memory_line_is_returned()
      {
         var summarizer = new CaptiveSceneSummarizer(
            new StubLlmClient("Let me summarize the exchange for the memory line."));

         string? line = await summarizer.SummarizeAsync(Npc(), "You: let me go.\nMesui: not yet.", "interrogated", false);

         line.Should().BeNull();
      }

      // The other side of the same boundary: a genuine first-person scene memory must still reach storage,
      // or the guard would silently cost every captive scene its rich summary and leave only the base line.
      [Test]
      public async Task GIVEN_a_genuine_scene_memory_WHEN_summarizing_THEN_it_is_returned()
      {
         var summarizer = new CaptiveSceneSummarizer(
            new StubLlmClient("I held them in the dark until they spoke of the ford."));

         string? line = await summarizer.SummarizeAsync(Npc(), "You: let me go.\nMesui: not yet.", "interrogated", false);

         line.Should().Be("I held them in the dark until they spoke of the ford.");
      }

      #region private

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Mesui",
         Faction = "Khuzait",
         Clan = "Kherit"
      };

      private sealed class StubLlmClient : ILlmClient
      {
         private readonly string _content;

         public StubLlmClient(string content) => _content = content;

         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
            => Task.FromResult(new LlmResponse {Content = _content, IsSuccess = true});
      }

      #endregion
   }
}

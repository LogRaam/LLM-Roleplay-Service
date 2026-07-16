// Code written by Gabriel Mailhot, 01/07/2026.

#region

using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Pins how a chat-completion body is parsed, in particular the reasoning-model failure a
   ///   player hit in-game: MiMo/GLM spent the whole completion budget "thinking" on a scene
   ///   continuation, the body came back with a null content and the reasoning text, and the
   ///   old parser turned it into a hard "Response contained no message content" error, which
   ///   also prevented the length retry from firing.
   /// </summary>
   [TestFixture]
   public class OpenRouterResponseParsingTests
   {
      // ── Provider content-filter cuts (a moderated host stops generation mid-reply) ──

      // Both the partial text AND the finish reason must survive parsing: CompleteAsync's one-shot
      // retry (OpenRouterClient) decides whether to fire, and which of the two rolls "carried
      // further", from exactly these two fields. Drop either one and a moderated host's cut-off
      // reply reads to the player as the mod itself censoring them.
      [Test]
      public void GIVEN_a_reply_cut_by_the_providers_content_filter_WHEN_parsing_THEN_the_partial_text_and_the_reason_both_survive()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"He leans in and\"}," +
                             "\"finish_reason\":\"content_filter\"}]}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeTrue();
         response.Content.Should().Be("He leans in and");
         response.FinishReason.Should().Be("content_filter");
      }

      // Providers spell their own cut differently (underscore, hyphen, case, or "moderation" outright).
      // A variant this classifier fails to recognize silently disables the content-filter retry for
      // that provider, so the player keeps eating chopped replies from it forever.
      [TestCase("content_filter")]
      [TestCase("content-filter")]
      [TestCase("CONTENT_FILTER")]
      [TestCase("moderation")]
      public void GIVEN_the_known_filter_finish_reasons_WHEN_classifying_THEN_they_are_recognized_as_filter_cuts(string reason)
      {
         OpenRouterClient.IsContentFiltered(reason).Should().BeTrue();
      }

      // The negative space of the classifier, including null: a false positive here would route an
      // ordinary "stop"/"length" reply into the content-filter retry branch instead of its own path
      // (or, for "length", still needs its own separate retry logic to fire correctly).
      [TestCase("stop")]
      [TestCase("length")]
      [TestCase(null)]
      public void GIVEN_ordinary_finish_reasons_WHEN_classifying_THEN_they_are_not_filter_cuts(string reason)
      {
         OpenRouterClient.IsContentFiltered(reason).Should().BeFalse();
      }

      // The file's flagship bug (see header): a reasoning model spending its whole budget "thinking"
      // must parse as an EMPTY SUCCESS, not a hard error, or CompleteAsync's bigger-budget retry
      // (which only fires on IsSuccess) never gets the chance to give the model room to actually reply.
      [Test]
      public void GIVEN_a_reasoning_only_reply_cut_by_length_WHEN_parsing_THEN_it_is_an_empty_success_so_the_bigger_budget_retry_fires()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null," +
                             "\"reasoning\":\"The captor would first...\"},\"finish_reason\":\"length\"}]}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeTrue();
         response.Content.Should().BeEmpty();
         response.FinishReason.Should().Be("length");
      }

      // GLM reports its thinking under a differently-named field ("reasoning_content" instead of
      // "reasoning"). Missing this fallback would resurface the exact flagship bug, but only for
      // that model family, which is the kind of gap that is easy to miss without a dedicated case.
      [Test]
      public void GIVEN_a_reasoning_only_reply_in_the_glm_reasoning_content_field_WHEN_parsing_THEN_it_is_treated_the_same_as_reasoning()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null," +
                             "\"reasoning_content\":\"Let me think...\"},\"finish_reason\":\"length\"}]}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeTrue();
         response.FinishReason.Should().Be("length");
      }

      // The other side of the empty-success case above: when the model itself claims "stop" (not
      // "length"), a bigger budget would not have helped, retrying is pointless, so this must stay a
      // hard failure, one with the actionable "lower Reasoning Effort" guidance instead of a generic
      // parse error, since the player has no other way to know why the reply came back blank.
      [Test]
      public void GIVEN_a_reasoning_only_reply_that_claims_to_have_stopped_WHEN_parsing_THEN_it_fails_with_reasoning_guidance()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null," +
                             "\"reasoning\":\"Endless pondering\"},\"finish_reason\":\"stop\"}]}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeFalse();
         response.ErrorMessage.Should().Contain("Reasoning Effort");
      }

      // Guards the fix does not overreach: a body with neither content nor reasoning is a genuine
      // upstream error envelope, not a reasoning-only reply, and must keep failing loudly with the
      // original message instead of being swallowed into a false empty success.
      [Test]
      public void GIVEN_an_error_body_without_content_or_reasoning_WHEN_parsing_THEN_it_stays_the_original_hard_failure()
      {
         const string body = "{\"error\":{\"message\":\"upstream exploded\"}}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeFalse();
         response.ErrorMessage.Should().Be("Response contained no message content.");
      }

      // ── Inline chain-of-thought leaks (the reported memory-screen bug) ──

      // Nemotron-style models put their thinking INLINE in content as <think>...</think> rather than
      // the separate reasoning field the cases above cover. End-to-end through ParseResponse, only the
      // prose after the trace may survive; otherwise the trace is what the NPC "says" and "remembers"
      // (Bannerlord hides the tags as markup, so the player sees the bare reasoning text).
      [Test]
      public void GIVEN_a_reply_with_an_inline_think_trace_WHEN_parsing_THEN_only_the_prose_after_it_survives()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\"," +
                             "\"content\":\"<think>The user wants me to write a memory line.</think>She kept her word.\"}," +
                             "\"finish_reason\":\"stop\"}]}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeTrue();
         response.Content.Should().Be("She kept her word.");
      }

      // The exact reported failure: Reasoning Effort High plus a small completion budget, so the inline
      // trace is cut by length before its closing tag and no prose ever arrives. It must parse as an
      // EMPTY success carrying the length reason, so CompleteAsync's bigger-budget retry fires, the same
      // path as when a truncated trace arrives in the separate reasoning field.
      [Test]
      public void GIVEN_an_inline_trace_cut_by_length_with_no_prose_WHEN_parsing_THEN_an_empty_success_so_the_retry_fires()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\"," +
                             "\"content\":\"<think>Let me summarize what happened: 1. Huan Yi\"}," +
                             "\"finish_reason\":\"length\"}]}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeTrue();
         response.Content.Should().BeEmpty();
         response.FinishReason.Should().Be("length");
      }

      // Baseline regression guard: the ordinary, by-far-most-common reply shape must keep flowing
      // through unchanged while all the reasoning-model special cases above are handled around it.
      [Test]
      public void GIVEN_a_normal_reply_WHEN_parsing_THEN_content_finish_reason_and_usage_flow_through()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"[DIALOGUE]Well met.[/DIALOGUE]\"}," +
                             "\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":100,\"completion_tokens\":12}}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeTrue();
         response.Content.Should().Contain("Well met.");
         response.FinishReason.Should().Be("stop");
         response.Usage.Should().NotBeNull();
         response.Usage!.PromptTokens.Should().Be(100);
      }
   }
}

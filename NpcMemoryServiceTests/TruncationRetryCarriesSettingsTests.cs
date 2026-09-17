// Code written by Gabriel Mailhot, 17/09/2026.
// What these pin: that the bigger-budget retry is the SAME call, only with more room.
//
// When a reply comes back truncated, the client retries once with double the completion budget. It does
// that by rebuilding LlmParameters by hand — and a hand-rebuilt object is a list of things somebody
// remembered, which is a different thing from a copy.
//
// It had already lost the anti-repetition penalty once, on exactly the retries most likely to ramble; that
// was fixed, and the comment left behind said every other setting was "carried across untouched". It was
// not. ReasoningOverride was still being dropped (token audit, 16/09/2026).
//
// That one matters more than it looks. Every housekeeping call in the mod sets ReasoningOverride = "off" —
// the conversation and letter summarizers, the memory and transcript compressors, the claim and engagement
// extractors, the strategic assessment — because none of them wants chain-of-thought and all of them run on
// a tight structured budget. Which makes them the calls most likely to truncate and reach this retry, where
// they came back with the global reasoning dial switched back ON: double the budget, and reasoning tokens
// eating it, on a call whose entire purpose was to be cheap and mechanical.
//
// These tests assert the carry rather than the comment, because a comment is what failed twice here.

#region

using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class TruncationRetryCarriesSettingsTests
   {
      // EVERY setting a caller chose, still chosen after the retry. Written as one sweep over the whole
      // parameter set rather than a test per field, because the failure mode here is a field somebody FORGOT
      // — and a test that lists the fields by hand forgets them the same way the code did.
      [Test]
      public void GIVEN_a_call_that_truncated_WHEN_it_is_retried_THEN_only_the_budget_has_changed()
      {
         var chosen = new LlmParameters {
            MaxTokens = 480,
            Creativity = 0.2f,
            PresencePenalty = 0.4f,
            ReasoningOverride = "off",
            AllowTruncationRetry = true
         };

         LlmParameters retried = Rebuild(chosen);

         retried.MaxTokens.Should().Be(chosen.MaxTokens * 2, "the budget is the ONE thing a retry changes");
         retried.Creativity.Should().Be(chosen.Creativity);
         retried.PresencePenalty.Should().Be(chosen.PresencePenalty);
         retried.ReasoningOverride.Should().Be("off", "a housekeeping call must not think its way through a retry");
         retried.AllowTruncationRetry.Should().Be(chosen.AllowTruncationRetry);
      }

      // THE REPORTED CASE, stated as what it costs: a mechanical extractor asked for no reasoning, truncated,
      // and was retried with reasoning back on and twice the room for it to spend.
      [Test]
      public void GIVEN_a_housekeeping_call_with_reasoning_off_WHEN_it_is_retried_THEN_reasoning_stays_off()
      {
         Rebuild(new LlmParameters {MaxTokens = 160, ReasoningOverride = "off"})
            .ReasoningOverride.Should().Be("off");
      }

      // A caller that set NO override must not acquire one from the retry either: absent means "use the
      // global dial", and the retry has no business deciding that.
      [Test]
      public void GIVEN_a_call_with_no_reasoning_preference_WHEN_it_is_retried_THEN_it_still_has_none()
      {
         Rebuild(new LlmParameters {MaxTokens = 3000}).ReasoningOverride.Should().BeNull();
      }

      // The identity fields the retry also rebuilds by hand. Losing the subject would only misreport a cache
      // line; losing the model override would send an extractor's retry to the scene model, which is a
      // different and more expensive answer to a question nobody asked differently.
      [Test]
      public void GIVEN_a_call_bound_to_one_model_WHEN_it_is_retried_THEN_it_goes_to_the_same_model()
      {
         LlmRequest retried = RebuildRequest(new LlmRequest {
            SystemPrompt = "S", StableSystemPrompt = "S",
            Messages = new List<LlmMessage> {new(MessageRole.User, "hello")},
            Parameters = new LlmParameters {MaxTokens = 160},
            ModelOverride = "an/extractor-model",
            Subject = "npc_sora"
         });

         retried.ModelOverride.Should().Be("an/extractor-model");
         retried.Subject.Should().Be("npc_sora");
         retried.SystemPrompt.Should().Be("S");
         retried.StableSystemPrompt.Should().Be("S");
      }

      #region private

      private static LlmParameters Rebuild(LlmParameters chosen)
         => RebuildRequest(new LlmRequest {
            SystemPrompt = "S",
            Messages = new List<LlmMessage> {new(MessageRole.User, "hello")},
            Parameters = chosen
         }).Parameters;

      /// <summary>
      ///   Drives the client's own retry rebuild rather than a copy of it, so this measures the request the
      ///   game would actually send. The rebuild is inline in CompleteAsync, so it is reached the same way the
      ///   wire-format tests reach ToWireFormat.
      /// </summary>
      private static LlmRequest RebuildRequest(LlmRequest original)
      {
         MethodInfo rebuild = typeof(OpenRouterClient)
            .GetMethod("RebuildForBiggerBudget", BindingFlags.NonPublic | BindingFlags.Static)!;

         return (LlmRequest) rebuild.Invoke(null, new object[] {original})!;
      }

      #endregion
   }
}

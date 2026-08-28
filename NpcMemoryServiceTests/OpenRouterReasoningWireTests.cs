// Code written by Gabriel Mailhot, 26/08/2026.
// Pins HOW the reasoning keyword is serialized on the wire, which decides whether a local OpenAI-compatible
// server (Ollama) actually honours the player's reasoning choice. Field report (Shine, 2026-08-25): CR only ever
// emitted OpenRouter's "reasoning" OBJECT, which Ollama silently ignores, so Qwen3.5 Heretic kept "thinking" and
// spent the entire 1,500-token reply budget on internal reasoning (24 s, empty content, finish_reason=length,
// then CR's doubled-budget retry). The fix serializes the SAME keyword as the top-level "reasoning_effort" STRING
// for the OpenAI-compatible kind, and keeps the object form for OpenRouter. If this drifts, either a local model
// resumes burning its budget thinking (wire went back to the ignored object), or OpenRouter stops seeing the
// object it needs; and the "Default" boundary is what guarantees an untouched setup sends nothing at all.

#region

using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class OpenRouterReasoningWireTests
   {
      private static OpenRouterClient BuildClient(string? reasoningKeyword, bool asEffortString)
         => new(new HttpClient(), new OpenRouterConfig {
            Model = "some/model",
            ReasoningProvider = () => reasoningKeyword,
            ReasoningAsEffortStringProvider = () => asEffortString
         });

      private static LlmRequest Request()
         => new() {
            SystemPrompt = "system",
            Messages = new[] {new LlmMessage(MessageRole.User, "hello")}
         };

      private static Dictionary<string, object> Body(string? keyword, bool asEffortString)
         => (Dictionary<string, object>) BuildClient(keyword, asEffortString).ToWireFormat(Request());

      private static LlmRequest RequestWithOverride(string? reasoningOverride)
         => new() {
            SystemPrompt = "system",
            Messages = new[] {new LlmMessage(MessageRole.User, "hello")},
            Parameters = new LlmParameters {ReasoningOverride = reasoningOverride}
         };

      private static Dictionary<string, object> BodyWithOverride(
         string? globalKeyword, bool asEffortString, string? reasoningOverride)
         => (Dictionary<string, object>) BuildClient(globalKeyword, asEffortString)
            .ToWireFormat(RequestWithOverride(reasoningOverride));

      // THE FIX: a local/OpenAI-compatible endpoint reads the top-level reasoning_effort string. Off must map to
      // "none" (the spelling Ollama honours) so a reasoning model stops eating its whole reply budget. The object
      // form must be absent, or the same request would carry two contradictory reasoning signals.
      [Test]
      public void GIVEN_the_openai_compatible_kind_WHEN_reasoning_is_off_THEN_the_wire_carries_reasoning_effort_none_only()
      {
         Dictionary<string, object> body = Body("Off", asEffortString: true);

         body.Should().ContainKey("reasoning_effort");
         body["reasoning_effort"].Should().Be("none");
         body.Should().NotContainKey("reasoning");
      }

      // An explicit effort passes through verbatim as the OpenAI spelling, so a player who wants some (but bounded)
      // thinking on a local model gets exactly that level rather than the model's uncontrolled default.
      [Test]
      public void GIVEN_the_openai_compatible_kind_WHEN_reasoning_is_low_THEN_the_wire_carries_that_effort_string()
      {
         Dictionary<string, object> body = Body("Low", asEffortString: true);

         body["reasoning_effort"].Should().Be("low");
         body.Should().NotContainKey("reasoning");
      }

      // OpenRouter reads its own "reasoning" object, so for that kind the keyword must still take the object shape
      // and the top-level string must be absent, or OpenRouter would not see the reasoning control at all.
      [Test]
      public void GIVEN_the_openrouter_kind_WHEN_reasoning_is_off_THEN_the_wire_carries_the_reasoning_object_only()
      {
         Dictionary<string, object> body = Body("Off", asEffortString: false);

         body.Should().ContainKey("reasoning");
         body.Should().NotContainKey("reasoning_effort");
      }

      // The load-bearing "no surprise" boundary: Default (the local dropdown's out-of-the-box value) must emit
      // NEITHER field, so a player who never touches the setting sends the exact payload they always did.
      [Test]
      public void GIVEN_default_WHEN_building_the_body_THEN_no_reasoning_field_is_sent_on_either_kind()
      {
         Body("Default", asEffortString: true).Should().NotContainKey("reasoning_effort");
         Body("Default", asEffortString: true).Should().NotContainKey("reasoning");
         Body("Default", asEffortString: false).Should().NotContainKey("reasoning");
         Body("Default", asEffortString: false).Should().NotContainKey("reasoning_effort");
      }

      // A null keyword (no reasoning provider value resolved) is the same "send nothing" guarantee as Default, on
      // both wire shapes: the choke must never invent a field from the absence of a setting.
      [Test]
      public void GIVEN_a_null_keyword_WHEN_building_the_body_THEN_no_reasoning_field_is_sent()
      {
         Body(null, asEffortString: true).Should().NotContainKey("reasoning_effort");
         Body(null, asEffortString: false).Should().NotContainKey("reasoning");
      }

      // Direct mapping guard for the OpenAI spelling: the words off/none/disabled/false all collapse to "none",
      // the graded efforts pass through, and anything unrecognised omits the field (returns null).
      [Test]
      public void GIVEN_various_keywords_WHEN_mapping_to_the_effort_string_THEN_they_normalise_as_documented()
      {
         OpenRouterClient.BuildReasoningEffort("none").Should().Be("none");
         OpenRouterClient.BuildReasoningEffort("disabled").Should().Be("none");
         OpenRouterClient.BuildReasoningEffort("HIGH").Should().Be("high");
         OpenRouterClient.BuildReasoningEffort("default").Should().BeNull();
         OpenRouterClient.BuildReasoningEffort("").Should().BeNull();
         OpenRouterClient.BuildReasoningEffort(null).Should().BeNull();
      }

      // STAKE: without this, memory compression on a Medium/High reasoning model loops and fails with an empty
      // reply (the exact player report this fixes), because the housekeeping call would inherit the player's
      // global dial instead of forcing reasoning off for itself. Covers the OpenAI-compatible wire shape.
      [Test]
      public void GIVEN_a_per_request_override_of_off_WHEN_the_global_dial_is_high_THEN_the_effort_string_wire_carries_off_not_high()
      {
         Dictionary<string, object> body = BodyWithOverride("high", asEffortString: true, reasoningOverride: "off");

         body.Should().ContainKey("reasoning_effort");
         body["reasoning_effort"].Should().Be("none");
         body.Should().NotContainKey("reasoning");
      }

      // STAKE: without this, a housekeeping call routed to OpenRouter (rather than a local endpoint) would still
      // inherit the player's global reasoning dial via the "reasoning" object, and a compression call on a
      // reasoning model would burn its whole budget thinking instead of emitting the [KEEP] list.
      [Test]
      public void GIVEN_a_per_request_override_of_off_WHEN_the_global_dial_is_high_THEN_the_reasoning_object_wire_carries_off_not_high()
      {
         Dictionary<string, object> body = BodyWithOverride("high", asEffortString: false, reasoningOverride: "off");

         body.Should().ContainKey("reasoning");
         body.Should().NotContainKey("reasoning_effort");
      }

      // STAKE: without this pin, a future edit could make the override always win, silently overriding the
      // player's own explicit "Default" (no reasoning field) choice on every ordinary chat call and sending a
      // reasoning field nobody asked for.
      [Test]
      public void GIVEN_a_null_override_WHEN_the_global_dial_is_default_THEN_no_reasoning_field_is_sent()
      {
         BodyWithOverride("Default", asEffortString: true, reasoningOverride: null).Should().NotContainKey("reasoning_effort");
         BodyWithOverride("Default", asEffortString: false, reasoningOverride: null).Should().NotContainKey("reasoning");
      }

      // STAKE: pins that a null/blank per-request override is a true no-op, not an accidental "off": the ordinary
      // chat, action-interpreter, and any other non-housekeeping call must keep sending the player's own high
      // dial exactly as before this change, or every in-game conversation would silently lose reasoning too.
      [Test]
      public void GIVEN_a_null_override_WHEN_the_global_dial_is_high_THEN_the_wire_still_carries_high()
      {
         BodyWithOverride("high", asEffortString: true, reasoningOverride: null)["reasoning_effort"].Should().Be("high");
         BodyWithOverride("high", asEffortString: false, reasoningOverride: null).Should().ContainKey("reasoning");
      }

      // STAKE: pins that a blank (whitespace-only) override behaves the same as null, so a caller that
      // constructs LlmParameters with an empty string by mistake does not accidentally force reasoning off on a
      // call that was meant to defer to the global dial.
      [Test]
      public void GIVEN_a_blank_override_WHEN_the_global_dial_is_high_THEN_the_wire_still_carries_high()
      {
         BodyWithOverride("high", asEffortString: true, reasoningOverride: "   ")["reasoning_effort"].Should().Be("high");
      }
   }
}

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

      [Test]
      public void GIVEN_a_reasoning_only_reply_in_the_glm_reasoning_content_field_WHEN_parsing_THEN_it_is_treated_the_same_as_reasoning()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null," +
                             "\"reasoning_content\":\"Let me think...\"},\"finish_reason\":\"length\"}]}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeTrue();
         response.FinishReason.Should().Be("length");
      }

      [Test]
      public void GIVEN_a_reasoning_only_reply_that_claims_to_have_stopped_WHEN_parsing_THEN_it_fails_with_reasoning_guidance()
      {
         const string body = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null," +
                             "\"reasoning\":\"Endless pondering\"},\"finish_reason\":\"stop\"}]}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeFalse();
         response.ErrorMessage.Should().Contain("Reasoning Effort");
      }

      [Test]
      public void GIVEN_an_error_body_without_content_or_reasoning_WHEN_parsing_THEN_it_stays_the_original_hard_failure()
      {
         const string body = "{\"error\":{\"message\":\"upstream exploded\"}}";

         LlmResponse response = OpenRouterClient.ParseResponse(body);

         response.IsSuccess.Should().BeFalse();
         response.ErrorMessage.Should().Be("Response contained no message content.");
      }

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

// Code written by Gabriel Mailhot, 20/08/2026.
// Player report (Desporion): a marriage proposal "agreed on the terms" over three letters was denied face to
// face because the letter system's memory of it was generic ("Received a letter (MarriageProposal)"), never
// the letter's actual content. LetterMemorySummarizer is the async second stage that turns a letter's real
// text into a rich first-person memory; this test pins that the prompt sent to the LLM actually carries the
// letter's content, demands first-person past tense, names the player, and forbids inventing facts beyond
// the letter, since a summary that hallucinates terms the letter never mentioned would just trade one
// contradiction (the NPC denying his own words) for another (the NPC "remembering" words he never wrote).

#region

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LetterMemorySummarizerPromptTests
   {
      // The load-bearing fix itself: without the letter's own text reaching the model as the user message, the
      // "rich" summary would just be another guess dressed up in prose, no better than the generic base line
      // it is meant to replace.
      [Test]
      public async Task GIVEN_a_letter_summary_request_WHEN_the_request_is_built_THEN_the_user_message_carries_the_letter_content()
      {
         var capturingClient = new CapturingLlmClient();
         var summarizer = new LetterMemorySummarizer(capturingClient);

         await summarizer.SummarizeAsync(Npc(), "I ask for your daughter's hand, and offer alliance in return.", true, "Aldric");

         capturingClient.LastRequest.Should().NotBeNull();
         capturingClient.LastRequest!.Messages.Should().ContainSingle(m =>
            m.Content.Contains("I ask for your daughter's hand, and offer alliance in return."));
      }

      // Read back later into the NPC's own chat prompt as their own memory, so it must be told to write as
      // the NPC, about the past, not as a narrator describing the letter from outside.
      [Test]
      public async Task GIVEN_a_letter_summary_request_WHEN_the_system_prompt_is_built_THEN_it_demands_first_person_past_tense()
      {
         var capturingClient = new CapturingLlmClient();
         var summarizer = new LetterMemorySummarizer(capturingClient);

         await summarizer.SummarizeAsync(Npc(), "I ask for your daughter's hand.", true, "Aldric");

         capturingClient.LastRequest!.SystemPrompt.Should().Contain("FIRST PERSON").And.Contain("PAST TENSE");
      }

      // The exact fix for the reported bug: the summary must be pinned to naming the player, and forbidden from
      // inventing anything the letter did not actually say (a hallucinated "agreed" term would recreate the
      // same class of contradiction the whole feature exists to close).
      [Test]
      public async Task GIVEN_a_letter_summary_request_WHEN_the_system_prompt_is_built_THEN_it_names_the_player_and_forbids_invention()
      {
         var capturingClient = new CapturingLlmClient();
         var summarizer = new LetterMemorySummarizer(capturingClient);

         await summarizer.SummarizeAsync(Npc(), "I ask for your daughter's hand.", true, "Aldric");

         capturingClient.LastRequest!.SystemPrompt.Should().Contain("Aldric");
         capturingClient.LastRequest!.SystemPrompt.Should().Contain("NEVER invent");
      }

      // npcIsSender selects which of the two people wrote the letter under examination; a wrong framing would
      // ask the model to remember writing a letter it only received, or vice versa.
      [Test]
      public async Task GIVEN_npcIsSender_false_WHEN_the_system_prompt_is_built_THEN_it_frames_the_letter_as_received()
      {
         var capturingClient = new CapturingLlmClient();
         var summarizer = new LetterMemorySummarizer(capturingClient);

         await summarizer.SummarizeAsync(Npc(), "Will you accept my terms?", false, "Aldric");

         capturingClient.LastRequest!.SystemPrompt.Should().Contain("Aldric sent to you");
      }

      #region private

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Desporion",
         Faction = "Vlandia",
         Clan = "Desporion's Clan"
      };

      private sealed class CapturingLlmClient : ILlmClient
      {
         public LlmRequest? LastRequest { get; private set; }

         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
         {
            LastRequest = request;
            return Task.FromResult(new LlmResponse {Content = "I wrote to Aldric to propose a match between our houses.", IsSuccess = true});
         }
      }

      #endregion
   }
}

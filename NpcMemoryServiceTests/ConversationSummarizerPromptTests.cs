// Code written by Gabriel Mailhot, 24/07/2026.
// Player report (Nexus): NPCs forget a stated plan/intention from the SAME in-game day, one conversation
// later. Root cause was the fallback recap being suppressed whenever the LLM had also emitted an [EVENT]
// that session (fixed separately, mod-side, ChatViewModel.FinalizeConversationMemory). But even when the
// recap DOES run, ConversationSummarizer.BuildSystemPrompt is what decides what the model is told to keep.
// A plan or promise is exactly the kind of detail a model drops from a loose "what was discussed" summary
// unless told explicitly to hold onto it. This test pins that the system prompt sent to the LLM carries an
// unambiguous instruction to preserve any stated plan, promise, arrangement, or agreed next step, since that
// is the load-bearing text that keeps the NPC from going blank (or reacting as if a promise was never made)
// next time the player brings it up.

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
   public class ConversationSummarizerPromptTests
   {
      // The load-bearing fix: without this instruction, a stated plan (e.g. "I'll ask around and tell you
      // what I learn") is exactly the kind of detail a loose "what was discussed" summary drops, since it
      // reads as filler rather than a fact worth keeping. Losing it is the reported bug: the NPC forgets a
      // same-day plan one conversation later and goes blank, or contradicts what they just promised.
      [Test]
      public async Task GIVEN_a_conversation_summary_request_WHEN_the_system_prompt_is_built_THEN_it_instructs_the_model_to_preserve_stated_plans()
      {
         var capturingClient = new CapturingLlmClient();
         var summarizer = new ConversationSummarizer(capturingClient);

         await summarizer.SummarizeAsync(Npc(), "You: will you look into it?\nMesui: aye, I'll ask around and send word.", "Huan Yi");

         capturingClient.LastRequest.Should().NotBeNull();
         capturingClient.LastRequest!.SystemPrompt.Should().Contain("plan, promise, arrangement or stated intention");
         capturingClient.LastRequest!.SystemPrompt.Should().Contain("what was agreed should happen next");
      }

      // Gabriel (17/10/2026): a companion remembered "the threat he described" of Fercread, a woman. The summary
      // is written in the third person about the player, so the model must be told her pronouns, never left to guess.
      [Test]
      public async Task GIVEN_the_player_is_a_woman_WHEN_the_summary_prompt_is_built_THEN_it_gives_her_pronouns()
      {
         var capturingClient = new CapturingLlmClient();
         var summarizer = new ConversationSummarizer(capturingClient);

         await summarizer.SummarizeAsync(Npc(), "You: be watchful. Mesui: I will.", "Fercread", playerIsFemale: true);

         capturingClient.LastRequest!.SystemPrompt.Should().Contain("Fercread is a woman: refer to her as she/her.");
      }

      #region private

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Mesui",
         Faction = "Khuzait",
         Clan = "Kherit"
      };

      private sealed class CapturingLlmClient : ILlmClient
      {
         public LlmRequest? LastRequest { get; private set; }

         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
         {
            LastRequest = request;
            return Task.FromResult(new LlmResponse {Content = "Mesui agreed to ask around and report back.", IsSuccess = true});
         }
      }

      #endregion
   }
}

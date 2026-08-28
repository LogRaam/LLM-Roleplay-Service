// Code written by Gabriel Mailhot, 12/06/2026.

#region

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;

#endregion

namespace NpcMemoryService.Core.Services
{
   /// <summary>
   ///   Generates a first-person memory line summarizing an ordinary conversation
   ///   from the NPC's point of view. Used by the host as a FALLBACK when a chat
   ///   closes without the LLM having emitted any [EVENT] block during the session
   ///   (e.g. the player walked away mid-exchange) — so the NPC still remembers the
   ///   conversation happened. One one-shot LLM call, run asynchronously AFTER the
   ///   chat closes so it never blocks the UI. On failure the caller keeps the plain
   ///   base summary it recorded synchronously.
   /// </summary>
   public sealed class ConversationSummarizer
   {
      private readonly ILlmClient _llmClient;

      public ConversationSummarizer(ILlmClient llmClient)
      {
         _llmClient = llmClient;
      }

      public LlmParameters Parameters { get; init; } = new() {
         MaxTokens = 160,
         Creativity = 0.4f,
         ReasoningOverride = "off" // mechanical summary, never benefits from reasoning
      };

      /// <summary>
      ///   Returns a concise first-person, past-tense memory line, or null on failure /
      ///   empty transcript (the caller then keeps the base summary it already stored).
      /// </summary>
      public async Task<string?> SummarizeAsync(
         NpcProfile npc,
         string conversationTranscript,
         string playerName,
         string? replyLanguage = null,
         CancellationToken ct = default)
      {
         if (npc == null || string.IsNullOrWhiteSpace(conversationTranscript)) return null;

         var request = new LlmRequest {
            SystemPrompt = BuildSystemPrompt(npc, playerName, replyLanguage),
            Messages = [new LlmMessage(MessageRole.User, conversationTranscript)],
            Parameters = Parameters
         };

         LlmResponse? response = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);

         if (response is not {IsSuccess: true} || string.IsNullOrWhiteSpace(response.Content))
            return null;

         // The model thought out loud instead of remembering ("The user wants me to write a memory
         // line..."): that meta-reasoning must never be STORED as the NPC's memory. Return null so the
         // caller keeps the plain base summary it already recorded.
         if (MetaReasoningGuard.IsMetaReasoning(response.Content))
            return null;

         return response.Content.Trim();
      }

      #region private

      private static string BuildSystemPrompt(NpcProfile npc, string playerName, string? replyLanguage)
      {
         string who = string.IsNullOrWhiteSpace(playerName)
            ? "a visitor"
            : playerName;
         var sb = new StringBuilder();
         sb.AppendLine($"You are {npc.Name}. You just spoke with {who}; the conversation has ended.");
         sb.AppendLine("Below is the transcript of the exchange.");
         sb.AppendLine();
         sb.AppendLine("Write ONE or TWO sentences, in the FIRST PERSON and PAST TENSE, capturing what YOU");
         sb.AppendLine("remember of this conversation — for your own private memory. Be concrete: what was");
         sb.AppendLine("discussed, asked, agreed, refused or learned, and how it left matters between you.");
         sb.AppendLine("ALWAYS capture any plan, promise, arrangement or stated intention either of you made,");
         sb.AppendLine("including what was agreed should happen next (word to gather, a meeting, a favor owed).");
         sb.AppendLine("If the visitor left abruptly mid-conversation, remember that too. No preamble, no");
         sb.AppendLine("quotation marks, no section tags — just the memory line itself.");
         sb.AppendLine(string.IsNullOrWhiteSpace(replyLanguage)
            ? "Write it in the same language as the transcript below."
            : $"Write it in {replyLanguage!.Trim()}, regardless of the language of the transcript below.");

         return sb.ToString();
      }

      #endregion
   }
}
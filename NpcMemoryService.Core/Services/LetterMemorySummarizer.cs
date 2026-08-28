// Code written by Gabriel Mailhot, 20/08/2026.

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
   ///   Generates a first-person memory line summarizing a LETTER's actual substance from the NPC's point of
   ///   view (what it proposed, agreed, reported), for storage on the sender's own profile. One-shot LLM call,
   ///   run asynchronously by the host AFTER the base memory is already recorded, so it never blocks the
   ///   courier/delivery tick. On failure the caller keeps the plain base line
   ///   <see cref="LetterMemoryPolicy.BaseMemory" /> already stored. Mirrors
   ///   <see cref="ConversationSummarizer" />'s pattern exactly (itself mirrored from the SDK's
   ///   CaptiveSceneSummarizer, in Captivity/).
   /// </summary>
   public sealed class LetterMemorySummarizer
   {
      private readonly ILlmClient _llmClient;

      public LetterMemorySummarizer(ILlmClient llmClient)
      {
         _llmClient = llmClient;
      }

      public LlmParameters Parameters { get; init; } = new() {
         MaxTokens = 160,
         Creativity = 0.4f,
         ReasoningOverride = "off" // mechanical summary, never benefits from reasoning
      };

      /// <summary>
      ///   Returns a concise first-person, past-tense memory line, or null on failure / empty content (the
      ///   caller then keeps the base summary it already stored). <paramref name="npcIsSender" /> selects
      ///   whether the NPC wrote or received the letter, so the prompt frames the transcript correctly.
      /// </summary>
      public async Task<string?> SummarizeAsync(
         NpcProfile npc,
         string letterContent,
         bool npcIsSender,
         string playerName,
         string? replyLanguage = null,
         CancellationToken ct = default)
      {
         if (npc == null || string.IsNullOrWhiteSpace(letterContent)) return null;

         var request = new LlmRequest {
            SystemPrompt = BuildSystemPrompt(npc, npcIsSender, playerName, replyLanguage),
            Messages = [new LlmMessage(MessageRole.User, letterContent)],
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

      private static string BuildSystemPrompt(NpcProfile npc, bool npcIsSender, string playerName, string? replyLanguage)
      {
         string who = string.IsNullOrWhiteSpace(playerName)
            ? "the player"
            : playerName;
         var sb = new StringBuilder();
         sb.AppendLine(npcIsSender
            ? $"You are {npc.Name}. Below is a letter YOU wrote and sent to {who}."
            : $"You are {npc.Name}. Below is a letter {who} sent to you.");
         sb.AppendLine();
         sb.AppendLine("Write ONE or TWO sentences, in the FIRST PERSON and PAST TENSE, capturing what YOU");
         sb.AppendLine("remember of this letter, for your own private memory: what it proposed, asked, agreed,");
         sb.AppendLine($"reported, or threatened. Name {who} by name. NEVER invent any fact beyond what the");
         sb.AppendLine("letter below actually says. No preamble, no quotation marks, no section tags, just the");
         sb.AppendLine("memory line itself. This is your own recollection, not a report to anyone else.");
         sb.AppendLine(string.IsNullOrWhiteSpace(replyLanguage)
            ? "Write it in the same language as the letter below."
            : $"Write it in {replyLanguage!.Trim()}, regardless of the language of the letter below.");

         return sb.ToString();
      }

      #endregion
   }
}

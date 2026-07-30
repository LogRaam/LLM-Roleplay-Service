// Code written by Gabriel Mailhot, 30/07/2026.
// LLM-driven compression of a LIVE conversation's history (distinct from LlmMemoryCompressor, which folds an
// NPC's PERSISTENT event memory). A long exchange, above all a council of several members, must pass a compact
// recap of what has been said to every turn so all participants stay coherent, without replaying an ever-growing
// raw transcript that inflates each prompt and can overflow a small or reasoning model (Gabriel 2026-07-30). The
// mod decides WHEN and WHICH lines to fold (ConversationCompressionPolicy) and holds the result on the
// transcript; this only writes the updated recap from the prior recap plus the new lines.

#region

using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Compression
{
   /// <summary>Folds newly finished lines of a conversation into a compact running recap via the LLM.</summary>
   public sealed class ConversationHistoryCompressor
   {
      private readonly ILlmClient _llmClient;

      public ConversationHistoryCompressor(ILlmClient llmClient)
      {
         _llmClient = llmClient;
      }

      /// <summary>
      ///   A recap is prose, not a structured decision, so a little room and a low, not zero, creativity. Bounded
      ///   so the recap itself never grows without limit as the conversation runs on.
      /// </summary>
      public LlmParameters Parameters { get; init; } = new() {
         MaxTokens = 700,
         Creativity = 0.3f
      };

      /// <summary>
      ///   Returns the recap updated to include <paramref name="newLines" />, folding them into
      ///   <paramref name="existingRecap" /> (empty on the first fold). Returns null on any LLM failure so the
      ///   caller can keep what it had (the compression is never allowed to lose the conversation).
      /// </summary>
      public async Task<string?> CompressAsync(string existingRecap, string newLines, CancellationToken ct = default)
      {
         if (string.IsNullOrWhiteSpace(newLines)) return existingRecap;

         var request = new LlmRequest {
            SystemPrompt = BuildSystemPrompt(),
            Messages = [new LlmMessage(MessageRole.User, BuildUserPrompt(existingRecap, newLines))],
            Parameters = Parameters
         };

         LlmResponse llmResponse = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);

         return llmResponse is {IsSuccess: true} && !string.IsNullOrWhiteSpace(llmResponse.Content)
            ? llmResponse.Content.Trim()
            : null;
      }

      #region private

      private static string BuildSystemPrompt()
      {
         return "You keep a running recap of an in-character conversation so that everyone present can stay "
              + "consistent with what has already been said. Rewrite the recap so it also covers the new lines. "
              + "Keep it a compact third-person summary: WHO said, asked, agreed to, or refused WHAT, and any "
              + "promise, grievance, or decision, naming each speaker so no one is confused with another. Keep "
              + "concrete commitments and turning points; drop small talk and repetition. Do not invent anything "
              + "not in the lines. Reply with the updated recap ONLY, no preamble, no commentary.";
      }

      private static string BuildUserPrompt(string existingRecap, string newLines)
      {
         if (string.IsNullOrWhiteSpace(existingRecap))
            return "Write the recap of this conversation so far:\n\n" + newLines;

         return "Recap so far:\n" + existingRecap.Trim() + "\n\nNew lines to fold in:\n" + newLines
              + "\n\nWrite the updated recap.";
      }

      #endregion
   }
}

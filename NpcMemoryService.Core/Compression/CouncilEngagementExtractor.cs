// Code written by Gabriel Mailhot, 30/07/2026.
// Council "Engagements" feature, slice B: reads a whole council conversation and lists the decisions currently
// standing, as a raw JSON array. The mod's EngagementLedger (CalradiaRemembers.Logic) reconciles to this snapshot
// every reading, so it is a MIRROR of the conversation, not a transaction log. Distinct from
// ConversationHistoryCompressor, which writes a prose recap; this writes a structured list of what is settled.

#region

using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Compression
{
   /// <summary>Extracts the set of decisions/commitments currently standing in a council conversation.</summary>
   public sealed class CouncilEngagementExtractor
   {
      private readonly ILlmClient _llmClient;

      public CouncilEngagementExtractor(ILlmClient llmClient)
      {
         _llmClient = llmClient;
      }

      /// <summary>
      ///   A structured extraction, not prose: little room needed and low creativity, since inventing beyond
      ///   what was said would plant a decision the table never actually made.
      /// </summary>
      public LlmParameters Parameters { get; init; } = new() {
         MaxTokens = 600,
         Creativity = 0.2f
      };

      /// <summary>
      ///   Returns the raw LLM content for <paramref name="conversationText" />, expected to be a JSON array of
      ///   flat decision objects (see <see cref="BuildSystemPrompt" />). Returns null when the conversation text
      ///   is blank, the call fails, or the content is blank, so the caller can keep whatever snapshot it had.
      /// </summary>
      public async Task<string?> ExtractAsync(string conversationText, CancellationToken ct = default)
      {
         if (string.IsNullOrWhiteSpace(conversationText)) return null;

         var request = new LlmRequest {
            SystemPrompt = BuildSystemPrompt(),
            Messages = [new LlmMessage(MessageRole.User, BuildUserPrompt(conversationText))],
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
         return "You read an in-character COUNCIL conversation and list ONLY the decisions or commitments that "
              + "CURRENTLY STAND: actually stated in the conversation AND assented to in-fiction by the party who "
              + "would be bound by them. Omit anything merely floated then dropped, contradicted, or explicitly "
              + "walked back (\"actually, let us not do that\" means it does NOT stand). Output a JSON array and "
              + "NOTHING else: no prose, no markdown fences. Each element is a flat object with exactly these "
              + "string fields: \"id\" (a short stable slug like \"raid-sturgia\", REUSED unchanged on every "
              + "reading while that decision still stands, so the same decision is recognised turn to turn), "
              + "\"subject\" (who is bound: a person's name or a group), \"decision\" (the decision in a few "
              + "plain words), \"status\" (\"agreed\" when the bound party has assented, otherwise "
              + "\"discussed\"). If nothing stands, output []. Never invent a decision not supported by the "
              + "conversation.";
      }

      private static string BuildUserPrompt(string conversationText)
      {
         return "Council conversation so far:\n\n" + conversationText
              + "\n\nList the decisions that currently stand, as the JSON array described.";
      }

      #endregion
   }
}

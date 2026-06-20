// Code written by Gabriel Mailhot, 19/06/2026.
// v1.2 chronicle: one-shot LLM pass that rewrites a bare list of dated deeds into flowing
// chronicler-voice prose. Mirrors the summarizer pattern (ILlmClient.CompleteAsync).

#region

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Services
{
   /// <summary>
   ///   Turns the player's plain, dated deed list into an "illuminated" chronicle — connected
   ///   medieval-chronicler prose — via a single LLM call. The optional <c>styleGuidance</c> lets
   ///   the host hand in an editable voice (a monk latinist, a Nord skald…). Returns null on
   ///   failure so the caller keeps the free templated chronicle.
   /// </summary>
   public sealed class ChronicleIlluminator
   {
      private readonly ILlmClient _llmClient;

      public ChronicleIlluminator(ILlmClient llmClient)
      {
         _llmClient = llmClient;
      }

      public LlmParameters Parameters { get; init; } = new LlmParameters {
         MaxTokens  = 1200,
         Creativity = 0.7f
      };

      /// <summary>
      ///   Rewrites <paramref name="deeds"/> into chronicler prose. When <paramref name="continuationOf"/>
      ///   is supplied (the tail of an existing illuminated chronicle), the model continues seamlessly
      ///   from it and covers ONLY the new deeds — so a living chronicle can be extended cheaply.
      /// </summary>
      public async Task<string?> IlluminateAsync(
         string playerName, string deeds, string styleGuidance, string? continuationOf = null, CancellationToken ct = default)
      {
         if (string.IsNullOrWhiteSpace(deeds)) return null;

         var request = new LlmRequest {
            SystemPrompt = BuildSystemPrompt(playerName, styleGuidance, continuationOf),
            Messages     = [new LlmMessage(MessageRole.User, deeds)],
            Parameters   = Parameters
         };

         LlmResponse? response = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);
         if (response == null || !response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            return null;

         return response.Content.Trim();
      }

      private static string BuildSystemPrompt(string playerName, string styleGuidance, string? continuationOf)
      {
         var sb = new StringBuilder();
         sb.AppendLine($"You are a court chronicler writing the life of {playerName}, a lord of Calradia.");
         sb.AppendLine("Below is a bare list of their deeds, each with its date. Rewrite them into a flowing");
         sb.AppendLine("chronicle in a medieval chronicler's voice — grand and vivid, reverent or wry as the");
         sb.AppendLine("deeds warrant — as connected prose, not a list. Preserve every deed and its order; you");
         sb.AppendLine("may weave a run of them into a single paragraph. Keep the dates legible in the prose.");
         sb.AppendLine("Invent NO events that are not in the list. Period-appropriate language only; no modern");
         sb.AppendLine("words, no preamble, no headings, no commentary — write only the chronicle itself.");

         if (!string.IsNullOrWhiteSpace(continuationOf))
         {
            sb.AppendLine();
            sb.AppendLine("You are CONTINUING an existing chronicle. Its most recent lines read:");
            sb.AppendLine($"«{continuationOf!.Trim()}»");
            sb.AppendLine("Pick up seamlessly from there, in the same voice and tense. Do NOT restate what is");
            sb.AppendLine("already written — chronicle ONLY the new deeds listed below, as the next passage.");
         }

         if (!string.IsNullOrWhiteSpace(styleGuidance))
         {
            sb.AppendLine();
            sb.AppendLine("STYLE GUIDANCE (write in this voice):");
            sb.AppendLine(styleGuidance.Trim());
         }
         return sb.ToString();
      }
   }
}

// Code written by Gabriel Mailhot, 07/06/2026.

#region

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;

#endregion

namespace NpcMemoryService.Core.Captivity
{
   /// <summary>
   ///   Generates a first-person memory line summarizing a captive scene from the
   ///   captor's point of view, for storage as a <see cref="NotableEventType.Captivity" />
   ///   event. This is a single one-shot LLM call, run asynchronously by the host AFTER
   ///   the scene ends so it never blocks the chat UI. On failure the caller keeps the
   ///   plain base summary it recorded synchronously when the scene closed.
   /// </summary>
   public sealed class CaptiveSceneSummarizer
   {
      private readonly ILlmClient _llmClient;

      public CaptiveSceneSummarizer(ILlmClient llmClient)
      {
         _llmClient = llmClient;
      }

      public LlmParameters Parameters { get; init; } = new() {
         MaxTokens = 160,
         Creativity = 0.4f
      };

      /// <summary>
      ///   Returns a concise first-person, past-tense memory line, or null on failure /
      ///   empty transcript (the caller then keeps the base summary it already stored).
      /// </summary>
      public async Task<string?> SummarizeAsync(
         NpcProfile captor,
         string sceneTranscript,
         string intentVerb,
         bool playerIsFemale,
         string? replyLanguage = null,
         CancellationToken ct = default)
      {
         if (captor == null || string.IsNullOrWhiteSpace(sceneTranscript)) return null;

         var request = new LlmRequest {
            SystemPrompt = BuildSystemPrompt(captor, intentVerb, playerIsFemale, replyLanguage),
            Messages = [new LlmMessage(MessageRole.User, sceneTranscript)],
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

      private static string BuildSystemPrompt(NpcProfile captor, string intentVerb, bool playerIsFemale, string? replyLanguage)
      {
         string pronoun = playerIsFemale
            ? "her"
            : "him";
         var sb = new StringBuilder();
         sb.AppendLine($"You are {captor.Name}. You have just held a prisoner in your power and {intentVerb} {pronoun}.");
         sb.AppendLine("Below is the transcript of what happened in the scene.");
         sb.AppendLine();
         sb.AppendLine("Write ONE or TWO sentences, in the FIRST PERSON and PAST TENSE, capturing what YOU");
         sb.AppendLine("remember of this, for your own private memory. Be concrete about what you did to the");
         sb.AppendLine("prisoner and how it left matters between you. No preamble, no quotation marks, no");
         sb.AppendLine("section tags, just the memory line itself. This is your own recollection, not a");
         sb.AppendLine("report to anyone else.");
         // AUDIT-CAPTIVE-STYLE T4 (2026-07-24): this summary is reinjected verbatim into every later scene
         // with the same captor (AppendHistory), so if it copies the scene's vivid phrasings ("sweet", "the
         // tendons of his neck") it seeds the very cross-scene sameness the audit is about. Record the FACTS
         // and acts, not the prose: no vivid sensory quotes, no borrowed turns of phrase.
         sb.AppendLine("Record the FACTS and acts plainly, not the prose: do not carry over vivid sensory");
         sb.AppendLine("phrasings or striking turns of phrase from the scene, only what happened and where it left you.");
         sb.AppendLine(string.IsNullOrWhiteSpace(replyLanguage)
            ? "Write it in the same language as the transcript below."
            : $"Write it in {replyLanguage!.Trim()}, regardless of the language of the transcript below.");

         return sb.ToString();
      }

      #endregion
   }
}
// Code written by Gabriel Mailhot, 07/06/2026. Sprint 17.

#region

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Captivity
{
    /// <summary>
    ///   Generates a first-person memory line summarizing a captive scene from the
    ///   captor's point of view, for storage as a <see cref="NotableEventType.Captivity"/>
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

        public LlmParameters Parameters { get; init; } = new LlmParameters {
            MaxTokens  = 160,
            Creativity = 0.4f
        };

        /// <summary>
        ///   Returns a concise first-person, past-tense memory line, or null on failure /
        ///   empty transcript (the caller then keeps the base summary it already stored).
        /// </summary>
        public async Task<string?> SummarizeAsync(
            NpcProfile captor,
            string     sceneTranscript,
            string     intentVerb,
            bool       playerIsFemale,
            CancellationToken ct = default)
        {
            if (captor == null || string.IsNullOrWhiteSpace(sceneTranscript)) return null;

            var request = new LlmRequest {
                SystemPrompt = BuildSystemPrompt(captor, intentVerb, playerIsFemale),
                Messages     = [new LlmMessage(MessageRole.User, sceneTranscript)],
                Parameters   = Parameters
            };

            LlmResponse? response = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);
            if (response == null || !response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
                return null;

            return response.Content.Trim();
        }

        private static string BuildSystemPrompt(NpcProfile captor, string intentVerb, bool playerIsFemale)
        {
            string pronoun = playerIsFemale ? "her" : "him";
            var sb = new StringBuilder();
            sb.AppendLine($"You are {captor.Name}. You have just held a prisoner in your power and {intentVerb} {pronoun}.");
            sb.AppendLine("Below is the transcript of what happened in the scene.");
            sb.AppendLine();
            sb.AppendLine("Write ONE or TWO sentences, in the FIRST PERSON and PAST TENSE, capturing what YOU");
            sb.AppendLine("remember of this — for your own private memory. Be concrete about what you did to the");
            sb.AppendLine("prisoner and how it left matters between you. No preamble, no quotation marks, no");
            sb.AppendLine("section tags — just the memory line itself. This is your own recollection, not a");
            sb.AppendLine("report to anyone else.");
            return sb.ToString();
        }
    }
}

// Code written by Gabriel Mailhot, 12/09/2026.
// Invented facts, increment 2: reading back what a character asserted that nobody had told him.
//
// This runs at conversation close, beside ConversationSummarizer, which is the whole economy of it: that pass
// already exists and already has the transcript, so capturing claims costs no extra call per conversation.
//
// The prompt and the parse are static and pure so both can be tested without a model. What is left for the
// model is the one thing only a model can do: read prose and notice an assertion.

#region

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   Pulls the factual assertions a character made about the world out of a finished conversation, so
    ///   <see cref="WorldClaimPolicy" /> can decide which of them Calradia should adopt.
    /// </summary>
    public sealed class WorldClaimExtractor
    {
        private readonly ILlmClient _llmClient;

        public WorldClaimExtractor(ILlmClient llmClient)
        {
            _llmClient = llmClient;
        }

        /// <summary>Mechanical classification. Reasoning off, and cold: this is not a place for invention.</summary>
        public LlmParameters Parameters { get; init; } = new() {
            MaxTokens = 220,
            Creativity = 0.1f,
            ReasoningOverride = "off"
        };

        /// <summary>
        ///   The instruction. Deliberately NARROW: it asks for a short closed list of subjects rather than
        ///   "any fact", because a false positive here is written into a save and read back to strangers
        ///   months later, while a false negative merely loses a rumour nobody had yet.
        /// </summary>
        public static string BuildSystemPrompt(string speakerName)
        {
            string who = string.IsNullOrWhiteSpace(speakerName) ? "the character" : speakerName!.Trim();

            var sb = new StringBuilder();
            sb.AppendLine("You are reading a finished conversation and doing one mechanical job: listing the");
            sb.AppendLine($"factual claims {who} made ABOUT THE WORLD which nobody in the conversation had told them.");
            sb.AppendLine();
            sb.AppendLine("Report ONLY claims of these kinds, and use exactly these labels:");
            sb.AppendLine("  BLIGHT   - a failed harvest, murrain, sickness of crops or livestock");
            sb.AppendLine("  BANDITRY - brigands, a road grown unsafe, a caravan taken, travellers waylaid");
            sb.AppendLine("  PASSING  - a death, a birth or a marriage among ordinary people");
            sb.AppendLine("  OMEN     - a portent, a sign, a superstition people are repeating");
            sb.AppendLine("  QUARREL  - a falling-out, a scandal or a dispute at some hall or town");
            sb.AppendLine();
            sb.AppendLine("IGNORE, and never report, anything about: numbers of soldiers, money, taxes, prices, who");
            sb.AppendLine("holds or governs a settlement, wars, sieges, battles, prosperity, garrisons, or anything");
            sb.AppendLine("the player did or owns. Those are recorded elsewhere and must not be reported here.");
            sb.AppendLine();
            sb.AppendLine("Also ignore anything the PLAYER said. Only what the character asserted counts.");
            sb.AppendLine();
            sb.AppendLine("One line per claim, in this exact format, and nothing else:");
            sb.AppendLine("CLAIM | <LABEL> | <place name, or ->  | <FACT or GUESS> | <one plain sentence, third person>");
            sb.AppendLine();
            sb.AppendLine("FACT if they stated it flatly; GUESS if they hedged, wondered, or called it rumour.");
            sb.AppendLine("If there are no such claims, reply with exactly: NONE");

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        ///   Reads the model's lines back into claims. Tolerant of surrounding chatter and strict about the
        ///   line itself: a line that does not parse is dropped rather than guessed at, because a guess here
        ///   becomes a permanent fact.
        /// </summary>
        public static IReadOnlyList<WorldClaim> Parse(string? reply, string speakerId, string speakerName)
        {
            var claims = new List<WorldClaim>();
            if (string.IsNullOrWhiteSpace(reply)) return claims;

            foreach (string raw in reply!.Split('\n'))
            {
                string line = raw.Trim();
                if (!line.StartsWith("CLAIM", StringComparison.OrdinalIgnoreCase)) continue;

                string[] parts = line.Split('|');
                if (parts.Length < 5) continue;

                ClaimSubject subject = SubjectFrom(parts[1]);
                if (subject == ClaimSubject.Unknown) continue;

                string place = parts[2].Trim();
                string text = string.Join("|", parts, 4, parts.Length - 4).Trim();

                if (text.Length == 0) continue;

                claims.Add(new WorldClaim {
                    SpeakerId = speakerId,
                    SpeakerName = speakerName,
                    Subject = subject,
                    PlaceName = place is "-" or "" ? null : place,
                    Text = text,
                    StatedAsFact = parts[3].Trim().Equals("FACT", StringComparison.OrdinalIgnoreCase)
                });
            }

            return claims;
        }

        /// <summary>
        ///   Reads a finished conversation. Returns an empty list on any failure: losing a rumour costs
        ///   nothing, and this runs on a background path where an exception would cost a memory.
        /// </summary>
        public async Task<IReadOnlyList<WorldClaim>> ExtractAsync(
            NpcProfile npc,
            string conversationTranscript,
            CancellationToken ct = default)
        {
            var none = new List<WorldClaim>();
            if (npc == null || string.IsNullOrWhiteSpace(conversationTranscript)) return none;

            var request = new LlmRequest {
                SystemPrompt = BuildSystemPrompt(npc.Name),
                Messages = [new LlmMessage(MessageRole.User, conversationTranscript)],
                Parameters = Parameters
            };

            LlmResponse? response = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);

            if (response is not {IsSuccess: true} || string.IsNullOrWhiteSpace(response.Content)) return none;

            return Parse(response.Content, npc.Id, npc.Name);
        }

        #region private

        private static ClaimSubject SubjectFrom(string label)
        {
            switch (label.Trim().ToUpperInvariant())
            {
                case "BLIGHT": return ClaimSubject.Blight;
                case "BANDITRY": return ClaimSubject.Banditry;
                case "PASSING": return ClaimSubject.PassingOrBirth;
                case "OMEN": return ClaimSubject.Omen;
                case "QUARREL": return ClaimSubject.LocalQuarrel;

                // Anything else - including a label the model invented, and including one naming a subject the
                // engine owns - is Unknown, and the gate refuses Unknown. The extractor never widens the list.
                default: return ClaimSubject.Unknown;
            }
        }

        #endregion
    }
}

// Code written by Gabriel Mailhot, 07/06/2026.

#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;

#endregion

using NpcMemoryService.Core.Services;

namespace NpcMemoryService.Core.Compression
{
   /// <summary>
   ///   LLM-driven memory compression. Asks the model to identify which events
   ///   can be dropped because later, stronger ones have superseded them.
   ///   The model is bound by conservative guardrails: never drop the first
   ///   meeting, the most recent N events, or any betrayal / intimacy event.
   /// </summary>
   public sealed class LlmMemoryCompressor : IMemoryCompressor
   {
      private readonly ILlmClient _llmClient;
      private readonly string? _replyLanguage;

      /// <summary>
      ///   <paramref name="replyLanguage" /> is the player's own Reply Language setting, or null/blank to take
      ///   the language from the events themselves. It is a CONSTRUCTOR argument rather than stored state
      ///   because the host builds a compressor per run, so a setting changed mid-campaign can never go stale
      ///   here (fkasad, 2026-09-09: see MemoryLanguagePolicy for what this fixes).
      /// </summary>
      public LlmMemoryCompressor(ILlmClient llmClient, string? replyLanguage = null)
      {
         _replyLanguage = replyLanguage;
         _llmClient = llmClient;
      }

      /// <summary>Below this event count, compression is a no-op.</summary>
      public int MinEventsForCompression { get; init; } = 12;

      // 2000, not 400: a REASONING model (GLM, MiMo) spends its whole reply budget on internal thinking before
      // it writes the [KEEP] list, so a tight budget comes back empty and the compression fails ("the model
      // spent its entire reply budget on internal reasoning"). A roomier budget leaves space for the answer
      // after the reasoning; the client still doubles it once on a retry if even this is eaten.
      public LlmParameters Parameters { get; init; } = new() {
         MaxTokens = 2000,
         Creativity = 0.2f, // deterministic; we want consistent decisions
         // Reasoning off regardless of the global dial: this is a mechanical keep/drop decision, and a
         // reasoning model asked to make it can loop on internal thinking and burn the whole budget with no
         // text produced (player report, Medium/High reasoning dial).
         ReasoningOverride = "off"
      };

      /// <summary>How many of the most recent events are always preserved.</summary>
      public int RecentEventsAlwaysKept { get; init; } = 5;

      public async Task<CompressionResult> CompressAsync(NpcProfile profile, CancellationToken ct = default)
      {
         IReadOnlyList<NotableEvent> events = profile.Events;

         if (events.Count < MinEventsForCompression) return CompressionResult.NoOp(events);

         string systemPrompt = BuildSystemPrompt();
         string userPrompt = BuildUserPrompt(profile);

         var request = new LlmRequest {
            SystemPrompt = systemPrompt,
            Messages = [new LlmMessage(MessageRole.User, userPrompt)],
            Parameters = Parameters
         };

         LlmResponse? llmResponse = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);

         if (!llmResponse.IsSuccess) return CompressionResult.Failure(llmResponse.ErrorMessage ?? "Unknown LLM error.");

         (IReadOnlyList<int> keepIndices, string? summary) = ParseResponse(llmResponse.Content);

         // Safety net: always preserve the protected indices, even if the LLM forgot.
         IEnumerable<int> protectedIndices = ComputeProtectedIndices(events);
         var finalIndices = new SortedSet<int>(keepIndices.Concat(protectedIndices));

         List<NotableEvent> kept = [];
         foreach (int i in finalIndices)
            if (i >= 0 && i < events.Count)
               kept.Add(events[i]);

         return CompressionResult.Success(
            kept,
            events.Count - kept.Count,
            string.IsNullOrWhiteSpace(summary)
               ? null
               : summary!.Trim(),
            llmResponse.Usage);
      }

      #region private

      private static string BuildUserPrompt(NpcProfile profile)
      {
         var sb = new StringBuilder();
         sb.AppendLine($"NPC: {profile.Name} ({profile.Clan} clan, {profile.Faction})");

         if (!string.IsNullOrWhiteSpace(profile.BackgroundContext))
         {
            sb.AppendLine();
            sb.AppendLine("EXISTING BACKGROUND CONTEXT (incorporate into the new summary):");
            sb.AppendLine(profile.BackgroundContext);
         }

         sb.AppendLine();
         sb.AppendLine("EVENTS (index — day — type — summary):");

         for (var i = 0; i < profile.Events.Count; i++)
         {
            NotableEvent? ev = profile.Events[i];
            sb.AppendLine($"[{i}] Day {ev.gameDay} — {ev.type} — {ev.summary}");
         }

         return sb.ToString();
      }

      private static string? ExtractSection(string text, string tag)
      {
         var pattern = $@"\[{Regex.Escape(tag)}\](.*?)\[/{Regex.Escape(tag)}\]";
         Match match = Regex.Match(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

         return match.Success
            ? match.Groups[1].Value
            : null;
      }

      // ── Response parsing ──────────────────────────────────────────────────

      private static (IReadOnlyList<int> KeepIndices, string? Summary) ParseResponse(string raw)
      {
         string? keepBody = ExtractSection(raw, "KEEP");
         string? summary = ExtractSection(raw, "DROP_SUMMARY");

         var indices = new List<int>();

         if (string.IsNullOrWhiteSpace(keepBody)) return (indices, summary);
         foreach (string token in keepBody!.Split([',', ' ', '\n', '\r', '\t'],
                     StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
               indices.Add(v);

         return (indices, summary);
      }

      // ── Prompt building ───────────────────────────────────────────────────

      private string BuildSystemPrompt()
      {
         var sb = new StringBuilder();
         sb.AppendLine("You compress an NPC's memory of past interactions with a player.");
         sb.AppendLine("Your job is to identify which events can be DROPPED because later, stronger");
         sb.AppendLine("events have superseded them. Be conservative — when in doubt, KEEP.");
         sb.AppendLine();
         sb.AppendLine("HARD RULES (always honor):");
         sb.AppendLine("- ALWAYS keep the FirstMeeting event if present.");
         sb.AppendLine($"- ALWAYS keep the {RecentEventsAlwaysKept} most recent events.");
         sb.AppendLine("- ALWAYS keep every Betrayal, Intimacy, Confrontation, Agreement, Farewell, Captivity, and Jealousy event.");
         sb.AppendLine();
         sb.AppendLine("SOFT GUIDANCE:");
         sb.AppendLine("- A series of minor skirmishes followed by a major battle: drop the skirmishes.");
         sb.AppendLine("- An alliance followed by a betrayal: keep both — they form an arc.");
         sb.AppendLine("- Repeated similar events of low impact: keep the most representative.");
         sb.AppendLine();
         sb.AppendLine("OUTPUT FORMAT (exactly):");
         sb.AppendLine();
         sb.AppendLine("[KEEP]");
         sb.AppendLine("Comma-separated indices to keep, e.g.: 0, 2, 4, 5, 6, 7");
         sb.AppendLine("[/KEEP]");
         sb.AppendLine();
         sb.AppendLine("[DROP_SUMMARY]");
         sb.AppendLine("One short paragraph (≤3 sentences) capturing what the dropped events collectively");
         sb.AppendLine("represent, written so a future you can reference it as background context.");
         sb.AppendLine("[/DROP_SUMMARY]");
         sb.AppendLine();
         // This paragraph becomes the NPC's BackgroundContext, which is injected into every later prompt, so a
         // language slip here does not stay in the memory: it reaches the dialogue. fkasad (2026-09-09) foresaw
         // exactly that, and until today this prompt said nothing about language at all.
         sb.AppendLine(MemoryLanguagePolicy.Directive(_replyLanguage, "events"));

         return sb.ToString();
      }

      // ── Safety net ────────────────────────────────────────────────────────

      private IEnumerable<int> ComputeProtectedIndices(IReadOnlyList<NotableEvent> events)
      {
         var protected_ = new HashSet<int>();

         // First meeting
         for (var i = 0; i < events.Count; i++)
            if (events[i].type == NotableEventType.FirstMeeting)
            {
               protected_.Add(i);

               break;
            }

         // Most recent N
         for (int i = Math.Max(0, events.Count - RecentEventsAlwaysKept); i < events.Count; i++)
            protected_.Add(i);

         // PRIVATE MEMORIES ARE SINGLETONS, and that is the whole argument (audit, 13/09/2026). Every other
         // event in this ledger left a trace somewhere else: it was rendered to a witness, it reached the
         // target's prompt, it was summarised into somebody's BackgroundContext. A private one never did - it
         // is deliberately excluded from BuildWitnessMemory, which is the single path by which one hero's
         // memories reach another's prompt. So this list is the only copy in the game, and a compressor that
         // drops it destroys the thing rather than compressing it.
         //
         // Concretely: a companion briefing is written as NotableEventType.Other, which is protected by
         // nothing here. Five newer events and the compression model could quietly forget the mission the
         // player drew him aside to give - the exact failure the whole pillar exists to prevent.
         //
         // Unconditional, matching every other entry below. These are rare by construction (one per private
         // word), so this cannot crowd out the compressor's work the way protecting a common type would.
         for (var i = 0; i < events.Count; i++)
            if (events[i].IsPrivate)
               protected_.Add(i);

         // Always-keep types
         for (var i = 0; i < events.Count; i++)
         {
            NotableEventType t = events[i].type;
            if (t is NotableEventType.Betrayal
                or NotableEventType.Intimacy
                or NotableEventType.Confrontation
                or NotableEventType.Agreement
                or NotableEventType.Farewell
                or NotableEventType.Captivity
                // Grudges pillar: Jealousy is mod-written and rare, and the jealousy backfill (and the
                // live Jealousy grudge it plants) depends on this NotableEvent surviving compression.
                or NotableEventType.Jealousy) protected_.Add(i);
         }

         return protected_;
      }

      #endregion
   }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.Parsing
{
    /// <summary>
    /// Regex-based parser for the canonical bracketed-section LLM response format.
    /// Tolerant of malformed input: never throws, returns a degraded
    /// <see cref="ParsedResponse"/> when sections are missing or invalid.
    /// </summary>
    public sealed class SectionResponseParser : IResponseParser
    {
        // Section tag names (canonical form, matched case-insensitively).
        private const string DialogueTag   = "DIALOGUE";
        private const string MemoryTag     = "MEMORY";
        private const string EventTag      = "EVENT";
        private const string ReputationTag = "REPUTATION";

        // Property keys inside sections (matched case-insensitively).
        private const string TopicKey        = "topic";
        private const string SentimentKey    = "sentiment";
        private const string DecisionKey     = "decision";
        private const string EventTypeKey    = "type";
        private const string SummaryKey      = "summary";
        private const string ClanDeltaKey    = "clan_delta";
        private const string FactionDeltaKey = "faction_delta";

        public ParsedResponse Parse(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return new ParsedResponse { Dialogue = string.Empty };
            }

            string dialogue = ExtractSection(rawResponse, DialogueTag)
                              ?? ExtractDialogueFallback(rawResponse);

            string? memorySection     = ExtractSection(rawResponse, MemoryTag);
            string? eventSection      = ExtractSection(rawResponse, EventTag);
            string? reputationSection = ExtractSection(rawResponse, ReputationTag);

            return new ParsedResponse
            {
                Dialogue      = dialogue.Trim(),
                Memory        = ParseMemory(memorySection),
                NewEventData  = ParseEventData(eventSection),
                Reputation    = ParseReputation(reputationSection)
            };
        }

        /// <summary>
        /// Extracts the body of [TAG]...[/TAG]. Returns null if not found.
        /// Non-greedy: stops at the first closing tag encountered.
        /// </summary>
        private static string? ExtractSection(string text, string tag)
        {
            string pattern = $@"\[{Regex.Escape(tag)}\](.*?)\[/{Regex.Escape(tag)}\]";
            Match match = Regex.Match(text, pattern,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Fallback when no [DIALOGUE] tag is found: returns everything before
        /// the first recognized section tag, or the whole text if none present.
        /// </summary>
        private static string ExtractDialogueFallback(string text)
        {
            string pattern = $@"\[(?:MEMORY|EVENT|REPUTATION)\]";
            Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return match.Success ? text.Substring(0, match.Index) : text;
        }

        private static ConversationMemory? ParseMemory(string? section)
        {
            if (string.IsNullOrWhiteSpace(section)) return null;
            var fields = ParseKeyValueLines(section!);

            if (!fields.TryGetValue(TopicKey, out string? topic)) return null;
            if (!fields.TryGetValue(SentimentKey, out string? sentiment)) return null;
            fields.TryGetValue(DecisionKey, out string? decision);

            return new ConversationMemory(topic, sentiment, decision);
        }

        private static ParsedEventData? ParseEventData(string? section)
        {
            if (string.IsNullOrWhiteSpace(section)) return null;
            var fields = ParseKeyValueLines(section!);

            if (!fields.TryGetValue(EventTypeKey, out string? typeStr)) return null;
            if (!fields.TryGetValue(SummaryKey, out string? summary)) return null;

            return new ParsedEventData(ParseEventType(typeStr), summary);
        }

        private static ReputationDelta? ParseReputation(string? section)
        {
            if (string.IsNullOrWhiteSpace(section)) return null;
            var fields = ParseKeyValueLines(section!);

            int? clanDelta    = TryParseSignedInt(fields, ClanDeltaKey);
            int? factionDelta = TryParseSignedInt(fields, FactionDeltaKey);

            if (clanDelta is null && factionDelta is null) return null;
            return new ReputationDelta(clanDelta, factionDelta);
        }

        private static int? TryParseSignedInt(IReadOnlyDictionary<string, string> fields, string key)
        {
            if (!fields.TryGetValue(key, out string? raw)) return null;
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
                ? v
                : (int?)null;
        }

        /// <summary>
        /// Parses lines of the form "key: value" into a dictionary.
        /// Tolerant: skips lines without ':', strips optional '#' value prefix,
        /// trims whitespace, comparison is case-insensitive.
        /// </summary>
        private static Dictionary<string, string> ParseKeyValueLines(string section)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLine in section.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0) continue;

                int colonIdx = line.IndexOf(':');
                if (colonIdx <= 0) continue;

                string key   = line.Substring(0, colonIdx).Trim();
                string value = line.Substring(colonIdx + 1).Trim();

                if (value.StartsWith("#", StringComparison.Ordinal))
                {
                    value = value.Substring(1);
                }

                dict[key] = value;
            }

            return dict;
        }

        private static NotableEventType ParseEventType(string raw)
        {
            string v = raw.TrimStart('#').Trim();

            if (Enum.TryParse<NotableEventType>(v, ignoreCase: true, out NotableEventType direct))
            {
                return direct;
            }

            return v.ToLowerInvariant() switch
            {
                "first_meeting" or "meeting"       => NotableEventType.FirstMeeting,
                "conflict"                         => NotableEventType.Conflict,
                "collaboration"                    => NotableEventType.Collaboration,
                "flirt"                            => NotableEventType.Flirt,
                "intimacy"                         => NotableEventType.Intimacy,
                "betrayal"                         => NotableEventType.Betrayal,
                "confrontation"                    => NotableEventType.Confrontation,
                _                                  => NotableEventType.Other
            };
        }
    }
}

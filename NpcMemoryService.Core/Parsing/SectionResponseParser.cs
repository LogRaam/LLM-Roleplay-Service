// Code written by Gabriel Mailhot, 12/05/2026.

#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Parsing
{
    /// <summary>
    ///   Regex-based parser for the canonical bracketed-section LLM response format.
    ///   Tolerant of malformed input: never throws, returns a degraded
    ///   <see cref="ParsedResponse" /> when sections are missing or invalid.
    /// </summary>
    public sealed class SectionResponseParser : IResponseParser
    {
        private const string ActionContextKey = "context";
        private const string ActionTag = "ACTION";
        private const string ActionTypeKey = "type";
        private const string ClanDeltaKey = "clan_delta";

        private const string DecisionKey = "decision";

        // Section tag names (canonical form, matched case-insensitively).
        private const string DialogueTag = "DIALOGUE";
        private const string DiscoveryDescriptionKey = "description";
        private const string DiscoveryKeyKey = "key";
        private const string DiscoveryTag = "DISCOVERY";
        private const string EventTag = "EVENT";
        private const string EventTypeKey = "type";
        private const string FactionDeltaKey = "faction_delta";
        private const string MemoryTag = "MEMORY";
        private const string NarrationTag = "NARRATION";
        private const string QuestAbandonTag = "QUEST_ABANDON";
        private const string QuestCompleteTag = "QUEST_COMPLETE";
        private const string QuestTag = "QUEST";
        private const string ReputationTag = "REPUTATION";
        private const string WitnessReactionTag = "WITNESS_REACTION";
        private const string SentimentKey = "sentiment";
        private const string SummaryKey = "summary";

        // Property keys inside sections (matched case-insensitively).
        private const string TopicKey = "topic";

        public ParsedResponse Parse(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse)) return new ParsedResponse {Dialogue = string.Empty};

            var dialogue = ExtractDialogue(rawResponse);

            var narrationSection = ExtractSection(rawResponse, NarrationTag);
            var memorySection = ExtractSection(rawResponse, MemoryTag);
            var eventSection = ExtractSection(rawResponse, EventTag);
            var reputationSection = ExtractSection(rawResponse, ReputationTag);
            var discoverySection = ExtractSection(rawResponse, DiscoveryTag);
            var questSection = ExtractSection(rawResponse, QuestTag);
            var questCompleteSection = ExtractSection(rawResponse, QuestCompleteTag);
            var questAbandonSection = ExtractSection(rawResponse, QuestAbandonTag);
            IReadOnlyList<GameAction> actions = ParseActions(rawResponse);
            IReadOnlyList<WitnessReaction> witnessReactions = ParseWitnessReactions(rawResponse);

            return new ParsedResponse {
                Dialogue = dialogue.Trim(),
                Narration = string.IsNullOrWhiteSpace(narrationSection) ? null : narrationSection.Trim(),
                Memory = ParseMemory(memorySection),
                NewEventData = ParseEventData(eventSection),
                Reputation = ParseReputation(reputationSection),
                Actions = actions,
                Discovery = ParseDiscovery(discoverySection),
                QuestGiven = ParseQuestProposal(questSection),
                QuestCompleted = ParseQuestCompletion(questCompleteSection),
                QuestAbandoned = ParseQuestAbandon(questAbandonSection),
                WitnessReactions = witnessReactions
            };
        }

        #region private

        /// <summary>
        ///   Extracts the dialogue, tolerating a missing [/DIALOGUE] close tag — which the
        ///   LLM sometimes drops when it ends its turn by handing the floor to the player.
        ///   Order: (1) a properly closed [DIALOGUE]…[/DIALOGUE]; (2) an open [DIALOGUE] with
        ///   no close — take up to the next section tag; (3) no tag at all — everything before
        ///   the first section. Any stray [DIALOGUE]/[/DIALOGUE] markers are then stripped so
        ///   the raw tag never leaks into the displayed line.
        /// </summary>
        private static string ExtractDialogue(string text)
        {
            string body;

            string? closed = ExtractSection(text, DialogueTag);
            if (closed != null)
            {
                body = closed;
            }
            else
            {
                Match open = Regex.Match(text, $@"\[{DialogueTag}\]", RegexOptions.IgnoreCase);
                body = open.Success
                    ? TrimAtFirstSection(text.Substring(open.Index + open.Length))
                    : ExtractDialogueFallback(text);
            }

            // Safety net: drop any residual [DIALOGUE]/[/DIALOGUE] markers that slipped through.
            return Regex.Replace(body, @"\[/?DIALOGUE\]", "", RegexOptions.IgnoreCase);
        }

        /// <summary>Returns the text up to the first recognized section boundary (or all of it).</summary>
        private static string TrimAtFirstSection(string text)
        {
            const string pattern = @"\[(?:/DIALOGUE|NARRATION|MEMORY|EVENT|REPUTATION|ACTION|DISCOVERY|QUEST_COMPLETE|QUEST_ABANDON|QUEST|WITNESS_REACTION)\]";
            Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

            return match.Success
                ? text.Substring(0, match.Index)
                : text;
        }

        /// <summary>
        ///   Fallback when no [DIALOGUE] tag is found: returns everything before
        ///   the first recognized section tag, or the whole text if none present.
        /// </summary>
        private static string ExtractDialogueFallback(string text)
        {
            var pattern = @"\[(?:NARRATION|MEMORY|EVENT|REPUTATION|ACTION|DISCOVERY|QUEST_COMPLETE|QUEST_ABANDON|QUEST|WITNESS_REACTION)\]";
            Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

            return match.Success
                ? text.Substring(0, match.Index)
                : text;
        }

        /// <summary>
        ///   Extracts the body of [TAG]...[/TAG]. Returns null if not found.
        ///   Non-greedy: stops at the first closing tag encountered.
        /// </summary>
        private static string? ExtractSection(string text, string tag)
        {
            var pattern = $@"\[{Regex.Escape(tag)}\](.*?)\[/{Regex.Escape(tag)}\]";
            Match match = Regex.Match(text, pattern,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return match.Success
                ? match.Groups[1].Value
                : null;
        }

        /// <summary>
        ///   Extracts all [ACTION] sections (multiple allowed per response).
        ///   Each section must specify a <c>type</c>; sections missing it are skipped.
        /// </summary>
        private static IReadOnlyList<GameAction> ParseActions(string text)
        {
            var actions = new List<GameAction>();
            var pattern = $@"\[{ActionTag}\](.*?)\[/{ActionTag}\]";

            foreach (Match match in Regex.Matches(text, pattern,
                         RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                Dictionary<string, string> fields = ParseKeyValueLines(match.Groups[1].Value);

                if (!fields.TryGetValue(ActionTypeKey, out var type) || string.IsNullOrWhiteSpace(type))
                    continue;

                fields.TryGetValue(ActionContextKey, out var context);

                // Everything else becomes parameters.
                var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> kvp in fields)
                {
                    if (string.Equals(kvp.Key, ActionTypeKey, StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.Equals(kvp.Key, ActionContextKey, StringComparison.OrdinalIgnoreCase)) continue;
                    parameters[kvp.Key] = kvp.Value;
                }

                actions.Add(new GameAction {
                    Type = type.Trim(),
                    Context = context,
                    Parameters = parameters
                });
            }

            return actions;
        }

        /// <summary>
        ///   Extracts all [WITNESS_REACTION] blocks (zero or more per response).
        ///   Each block must have a <c>name</c> and <c>text</c> field; malformed blocks
        ///   are skipped silently.
        /// </summary>
        private static IReadOnlyList<WitnessReaction> ParseWitnessReactions(string text)
        {
            var reactions = new List<WitnessReaction>();
            var pattern = $@"\[{WitnessReactionTag}\](.*?)\[/{WitnessReactionTag}\]";

            foreach (Match match in Regex.Matches(text, pattern,
                         RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                Dictionary<string, string> fields = ParseKeyValueLines(match.Groups[1].Value);

                if (!fields.TryGetValue("name", out var name)
                    || string.IsNullOrWhiteSpace(name)) continue;
                if (!fields.TryGetValue("text", out var reactionText)
                    || string.IsNullOrWhiteSpace(reactionText)) continue;

                reactions.Add(new WitnessReaction {
                    Name = name.Trim(),
                    Text = reactionText.Trim()
                });
            }

            return reactions;
        }

        private static DiscoveredTrait? ParseDiscovery(string? section)
        {
            if (string.IsNullOrWhiteSpace(section)) return null;
            Dictionary<string, string> fields = ParseKeyValueLines(section!);

            if (!fields.TryGetValue(DiscoveryKeyKey, out var key)) return null;
            if (!fields.TryGetValue(DiscoveryDescriptionKey, out var description)) return null;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(description)) return null;

            // GameDay is 0 here — the consumer stamps the real day when persisting.
            return new DiscoveredTrait { Key = key.Trim(), Description = description.Trim(), GameDay = 0 };
        }

        /// <summary>
        ///   Parses a [QUEST] block. Returns null when the block is absent or its type
        ///   is unrecognized — a malformed proposal is dropped, never half-persisted.
        ///   Target names stay as the LLM wrote them; the consumer resolves them to
        ///   real game objects and computes the deadline from <c>deadline_days</c>.
        /// </summary>
        private static QuestProposal? ParseQuestProposal(string? section)
        {
            if (string.IsNullOrWhiteSpace(section)) return null;
            Dictionary<string, string> fields = ParseKeyValueLines(section!);

            if (!fields.TryGetValue("type", out var typeStr)) return null;
            QuestType? type = ParseQuestType(typeStr);
            if (type == null) return null;

            fields.TryGetValue("description", out var description);

            var deadline = TryParseSignedInt(fields, "deadline_days");
            if (deadline is <= 0) deadline = null; // 0 or negative means "no deadline"

            return new QuestProposal {
                Type = type.Value,
                Description = description?.Trim() ?? "",
                TargetSettlement = NullIfBlank(GetField(fields, "target_settlement")),
                TargetHero = NullIfBlank(GetField(fields, "target_hero")),
                TargetFaction = NullIfBlank(GetField(fields, "target_faction")),
                DeadlineDays = deadline,
                RewardGold = ClampNonNegative(TryParseSignedInt(fields, "reward_gold")),
                RewardRelation = ClampNonNegative(TryParseSignedInt(fields, "reward_relation"))
            };
        }

        /// <summary>
        ///   Parses a [QUEST_COMPLETE] block. An empty body is valid — it means
        ///   "complete the single satisfied quest". A named <c>type</c> disambiguates
        ///   when the giver has several quests open. Returns null only when the block
        ///   is entirely absent.
        /// </summary>
        private static QuestCompletionClaim? ParseQuestCompletion(string? section)
        {
            if (section == null) return null;

            Dictionary<string, string> fields = ParseKeyValueLines(section);
            QuestType? type = fields.TryGetValue("type", out var typeStr)
                ? ParseQuestType(typeStr)
                : null;

            return new QuestCompletionClaim { Type = type };
        }

        /// <summary>
        ///   Parses a [QUEST_ABANDON] block. Like completion, an empty body means
        ///   "the single outstanding quest"; a named <c>type</c> disambiguates. Returns
        ///   null only when the block is entirely absent.
        /// </summary>
        private static QuestAbandonClaim? ParseQuestAbandon(string? section)
        {
            if (section == null) return null;

            Dictionary<string, string> fields = ParseKeyValueLines(section);
            QuestType? type = fields.TryGetValue("type", out var typeStr)
                ? ParseQuestType(typeStr)
                : null;

            return new QuestAbandonClaim { Type = type };
        }

        /// <summary>
        ///   Maps the LLM's quest-type token (snake_case or the enum name) to a
        ///   <see cref="QuestType" />. Returns null for unrecognized tokens.
        /// </summary>
        private static QuestType? ParseQuestType(string raw)
        {
            var v = raw.TrimStart(trimChars: '#').Trim().ToLowerInvariant();

            if (Enum.TryParse(v, true, out QuestType direct)) return direct;

            return v switch {
                "bandit_clear" or "bandits" or "clear_bandits"        => QuestType.BanditClear,
                "bandit_hideout" or "hideout" or "clear_hideout"      => QuestType.BanditHideout,
                "attack_faction" or "raid_faction" or "war_faction"   => QuestType.AttackFaction,
                "attack_lord" or "defeat_lord" or "fight_lord"        => QuestType.AttackLord,
                "raid_village" or "burn_village"                      => QuestType.RaidVillage,
                "attack_caravan" or "raid_caravan"                    => QuestType.AttackCaravan,
                "siege" or "besiege" or "siege_town"                  => QuestType.Siege,
                "capture_prisoner" or "capture" or "take_prisoner"    => QuestType.CapturePrisoner,
                "execute_enemy" or "execute" or "kill_enemy"          => QuestType.ExecuteEnemy,
                "rescue_prisoner" or "rescue" or "free_prisoner"      => QuestType.RescuePrisoner,
                "deliver_letter" or "letter" or "carry_letter"
                    or "deliver_message" or "carry_message"          => QuestType.DeliverLetter,
                "provide_gold" or "child_support" or "support_child"
                    or "give_gold_quest"                             => QuestType.ProvideGold,
                "scout_army" or "scout" or "spy_army"
                    or "locate_army" or "find_army"                  => QuestType.ScoutArmy,
                _ => (QuestType?)null
            };
        }

        private static string? GetField(IReadOnlyDictionary<string, string> fields, string key)
            => fields.TryGetValue(key, out var v) ? v : null;

        private static string? NullIfBlank(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value!.Trim();

        private static int ClampNonNegative(int? value)
            => value.HasValue && value.Value > 0 ? value.Value : 0;

        private static ParsedEventData? ParseEventData(string? section)
        {
            if (string.IsNullOrWhiteSpace(section)) return null;
            Dictionary<string, string> fields = ParseKeyValueLines(section!);

            if (!fields.TryGetValue(EventTypeKey, out var typeStr)) return null;
            if (!fields.TryGetValue(SummaryKey, out var summary)) return null;

            return new ParsedEventData(ParseEventType(typeStr), summary);
        }

        private static NotableEventType ParseEventType(string raw)
        {
            var v = raw.TrimStart(trimChars: '#').Trim();

            if (Enum.TryParse(v, true, out NotableEventType direct)) return direct;

            return v.ToLowerInvariant() switch {
                "first_meeting" or "meeting" => NotableEventType.FirstMeeting,
                "farewell" or "goodbye" or "parting" or "departure" => NotableEventType.Farewell,
                "conflict" => NotableEventType.Conflict,
                "collaboration" => NotableEventType.Collaboration,
                "agreement" or "promise" or "oath" or "contract" or "mission" => NotableEventType.Agreement,
                "flirt" => NotableEventType.Flirt,
                "intimacy" => NotableEventType.Intimacy,
                "betrayal" => NotableEventType.Betrayal,
                "confrontation" => NotableEventType.Confrontation,
                _ => NotableEventType.Other
            };
        }

        /// <summary>
        ///   Parses lines of the form "key: value" into a dictionary.
        ///   Tolerant: skips lines without ':', strips optional '#' value prefix,
        ///   trims whitespace, comparison is case-insensitive.
        /// </summary>
        private static Dictionary<string, string> ParseKeyValueLines(string section)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in section.Split(separator: '\n'))
            {
                var line = rawLine.Trim();

                if (line.Length == 0) continue;

                var colonIdx = line.IndexOf(':');

                if (colonIdx <= 0) continue;

                var key = line.Substring(0, colonIdx).Trim();
                var value = line.Substring(colonIdx + 1).Trim();

                if (value.StartsWith("#", StringComparison.Ordinal)) value = value.Substring(1);

                dict[key] = value;
            }

            return dict;
        }

        private static ConversationMemory? ParseMemory(string? section)
        {
            if (string.IsNullOrWhiteSpace(section)) return null;
            Dictionary<string, string> fields = ParseKeyValueLines(section!);

            if (!fields.TryGetValue(TopicKey, out var topic)) return null;
            if (!fields.TryGetValue(SentimentKey, out var sentiment)) return null;
            fields.TryGetValue(DecisionKey, out var decision);

            return new ConversationMemory(topic, sentiment, decision);
        }

        private static ReputationDelta? ParseReputation(string? section)
        {
            if (string.IsNullOrWhiteSpace(section)) return null;
            Dictionary<string, string> fields = ParseKeyValueLines(section!);

            var clanDelta = TryParseSignedInt(fields, ClanDeltaKey);
            var factionDelta = TryParseSignedInt(fields, FactionDeltaKey);

            if (clanDelta is null && factionDelta is null) return null;

            return new ReputationDelta(clanDelta, factionDelta);
        }

        private static int? TryParseSignedInt(IReadOnlyDictionary<string, string> fields, string key)
        {
            if (!fields.TryGetValue(key, out var raw)) return null;

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
                ? v
                : null;
        }

        #endregion
    }
}
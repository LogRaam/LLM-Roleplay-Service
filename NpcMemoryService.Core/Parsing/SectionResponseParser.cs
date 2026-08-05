// Code written by Gabriel Mailhot, 22/06/2026.

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

      /// <summary>Both separators a model writes between a key and its value. The first one on the line wins.</summary>
      private static readonly char[] KeyValueSeparators = {':', '='};

      /// <summary>The one action a stray, self-invented block is worth recovering into. See <see cref="RecoverStrayRelationChange" />.</summary>
      private const string ChangeRelationAction = "change_relation";

      /// <summary>
      ///   A bracketed ALL-CAPS token sitting ALONE on its own line. That is exactly what a section tag looks
      ///   like, and exactly what a weaker model writes when it INVENTS one instead of using the taught format
      ///   (a player was shown "[RELATION CHANGE]" spoken aloud by an NPC while the relation never moved:
      ///   Fakade, 2026-07-13). Legitimate dialogue never takes this shape: the prompt teaches *asterisks* for
      ///   stage directions and never [brackets], and prose brackets ("[unreadable]") are inline and lower-case.
      ///   So a line of this shape is MACHINERY, whatever it is called, and machinery must never reach the player.
      ///   Matched CASE-SENSITIVELY on purpose: that is the whole discriminator.
      /// </summary>
      private const string StandaloneTagLine = @"(?m)^[ \t]*\[[A-Z][A-Z0-9_ ]{1,30}\][ \t]*$";

      /// <summary>
      ///   A function-call envelope: <c>{"name": "change_relation", "parameters": {"delta": -5}}</c>, the shape
      ///   a tool-calling model reaches for instead of the taught [ACTION] block. Narrow on purpose (it must
      ///   carry BOTH keys) so ordinary prose containing a brace can never match.
      /// </summary>
      private const string JsonActionCall =
         @"\{\s*""name""\s*:\s*""(?<name>[A-Za-z_][A-Za-z0-9_]*)""\s*,\s*""(?:parameters|arguments)""\s*:\s*\{(?<args>[^{}]*)\}\s*\}";

      /// <summary>
      ///   A standalone tag line PLUS the directive lines that follow it, which is the whole shape of an invented
      ///   block ("[RELATION CHANGE]" then "delta = 2"). Removed as one piece, so no orphan fragment is left
      ///   behind for the player to read out of the NPC's mouth.
      /// </summary>
      private const string MachineryBlock =
         @"(?m)^[ \t]*\[/?[A-Z][A-Z0-9_ ]{0,30}\][ \t]*\r?\n?(?:[ \t]*[A-Za-z_][A-Za-z_ ]{0,24}[ \t]*[:=][^\r\n]*\r?\n?)*";

      /// <summary>An upper bound on a quest deadline: a model that writes an absurd "365" is capped to a sane maximum rather than persisting a year-long task.</summary>
      private const int MaxDeadlineDays = 90;

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
      private const string ResolutionTag = "RESOLUTION";
      private const string SentimentKey = "sentiment";
      private const string StanceTag = "STANCE";
      private const string SummaryKey = "summary";

      // Property keys inside sections (matched case-insensitively).
      private const string TopicKey = "topic";
      private const string WitnessReactionTag = "WITNESS_REACTION";

      public ParsedResponse Parse(string rawResponse)
      {
         if (string.IsNullOrWhiteSpace(rawResponse)) return new ParsedResponse {Dialogue = string.Empty};

         string dialogue = ExtractDialogue(rawResponse);

         // Adult-prompt audit M8: [NARRATION] was the ONE channel with no open-tag tolerance, while the QUEST
         // family had it and DIALOGUE has its own. A climactic beat is exactly the longest thing the model
         // writes, so it is the likeliest to hit the token ceiling before reaching [/NARRATION]: the whole
         // beat then vanished, and the scene could render as nothing at all.
         string? narrationSection = ExtractSectionTolerant(rawResponse, NarrationTag);
         string? memorySection = ExtractSection(rawResponse, MemoryTag);
         string? eventSection = ExtractSection(rawResponse, EventTag);
         string? reputationSection = ExtractSection(rawResponse, ReputationTag);
         string? stanceSection = ExtractSection(rawResponse, StanceTag);
         string? discoverySection = ExtractSection(rawResponse, DiscoveryTag);
         // The QUEST family is promise-shaped: a block dropped for want of a close tag is a broken word the
         // player never sees. Tolerate the truncation (a max-tokens cut) so a partial block still parses, or
         // (if unparseable) still trips QuestBlockMalformed and tells the player, instead of vanishing.
         string? questSection = ExtractSectionTolerant(rawResponse, QuestTag);
         string? questCompleteSection = ExtractSectionTolerant(rawResponse, QuestCompleteTag);
         string? questAbandonSection = ExtractSectionTolerant(rawResponse, QuestAbandonTag);
         IReadOnlyList<GameAction> actions = ParseActions(rawResponse);
         IReadOnlyList<WitnessReaction> witnessReactions = ParseWitnessReactions(rawResponse);
         IReadOnlyList<ResolutionProposal> resolutions = ParseResolutions(rawResponse);
         QuestProposal? questGiven = ParseQuestProposal(questSection);

         return new ParsedResponse {
            // A model that answers with a function-CALL rather than our [ACTION] block leaves the raw JSON
            // sitting in the spoken line, and the player watches the NPC recite our plumbing (reported on
            // Nexus, 2026-07-19). ParseActions recovers the call so the deed still happens; this takes it
            // out of the character's mouth. Both halves are needed: stripping alone would silence a regard
            // change the NPC plainly meant, recovering alone would still show the JSON.
            Dialogue = StripJsonActionEnvelopes(dialogue).Trim(),
            // netstandard2.0 has no NotNullWhen annotation on IsNullOrWhiteSpace,
            // so the compiler can't see the guard — hence the '!'.
            Narration = string.IsNullOrWhiteSpace(narrationSection)
               ? null
               : narrationSection!.Trim(),
            NarrationBeforeDialogue = IsNarrationBeforeDialogue(rawResponse),
            Memory = ParseMemory(memorySection),
            NewEventData = ParseEventData(eventSection),
            Reputation = ParseReputation(reputationSection),
            StanceShift = ParseStanceShift(stanceSection),
            Actions = actions,
            Discovery = ParseDiscovery(discoverySection),
            QuestGiven = questGiven,
            // The model DID emit a [QUEST] block but it could not be parsed (missing/unknown type). A block cut
            // off before its close is now caught too: questSection comes from ExtractSectionTolerant, so a
            // TRUNCATED [QUEST] arrives non-empty and trips this flag instead of vanishing (it used to be
            // dropped silently, which this comment wrongly claimed it covered). Surfaced so the host can tell
            // the player the spoken offer was NOT recorded, instead of the two silently diverging.
            QuestBlockMalformed = questGiven == null && !string.IsNullOrWhiteSpace(questSection),
            QuestCompleted = ParseQuestCompletion(questCompleteSection),
            QuestAbandoned = ParseQuestAbandon(questAbandonSection),
            WitnessReactions = witnessReactions,
            Resolutions = resolutions
         };
      }

      #region private

      /// <summary>
      ///   True when the raw reply opens its [NARRATION] block BEFORE its [DIALOGUE] block — a
      ///   narration-led turn the host should render in the emitted order instead of its usual
      ///   dialogue-first layout. Plain position comparison on the open tags (case-insensitive,
      ///   like every other tag match here); false when either tag is absent, since the order is
      ///   then moot. The close tags can never match: "[/DIALOGUE]" does not contain "[DIALOGUE]".
      /// </summary>
      private static bool IsNarrationBeforeDialogue(string text)
      {
         int narrationIndex = text.IndexOf($"[{NarrationTag}]", StringComparison.OrdinalIgnoreCase);
         int dialogueIndex = text.IndexOf($"[{DialogueTag}]", StringComparison.OrdinalIgnoreCase);

         return narrationIndex >= 0 && dialogueIndex >= 0 && narrationIndex < dialogueIndex;
      }

      private static int ClampNonNegative(int? value)
         => value is > 0
            ? value.Value
            : 0;

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
         body = Regex.Replace(body, @"\[/?DIALOGUE\]", "", RegexOptions.IgnoreCase);

         // The guarantee: whatever the model invents, the player never reads our plumbing. A block the parser
         // does not recognise is still MACHINERY, and dropping it from the spoken line is not optional. See
         // MachineryBlock: a standalone ALL-CAPS bracketed line and the directive lines under it, removed whole.
         body = Regex.Replace(body, MachineryBlock, "");

         // Weaker models sometimes prefix the line with a stray bracketed label — their own
         // name, or a tag they invented (e.g. "[Vesha the Crow]"). Legitimate dialogue uses
         // *asterisks* for action, never [brackets] as a speaker label, so strip only a
         // LEADING bracketed token (after optional whitespace), at most once. A bracketed
         // token elsewhere in the body (e.g. a quoted "[unreadable]" in prose) is dialogue
         // content and must survive.
         body = Regex.Replace(body, @"^\s*\[[^\]\n]{1,40}\]", "");

         return body;
      }

      /// <summary>
      ///   Fallback when no [DIALOGUE] tag is found: returns everything before
      ///   the first recognized section tag, or the whole text if none present.
      ///   One exception: when what survives is un-tagged META-REASONING (the model thinking out loud —
      ///   "Looking at the context: ...", "The user wants me to ...") and the reply holds no closed
      ///   [DIALOGUE] at all, the model said NOTHING in character, so the fallback returns empty and the
      ///   host shows no bubble rather than reading the model's homework aloud as the NPC's words.
      /// </summary>
      private static string ExtractDialogueFallback(string text)
      {
         var pattern = @"\[(?:NARRATION|MEMORY|EVENT|REPUTATION|STANCE|ACTION|DISCOVERY|QUEST_COMPLETE|QUEST_ABANDON|QUEST|WITNESS_REACTION|RESOLUTION)\]";
         Match known = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

         // An INVENTED section ends the dialogue just as surely as a known one. Without this, a model that wrote
         // "[RELATION CHANGE]" (a tag we never taught) left it outside every boundary we recognised, so it fell
         // through into the spoken line and the NPC read our plumbing aloud. Case-sensitive: only ALL-CAPS.
         Match invented = Regex.Match(text, StandaloneTagLine);

         int cut = known.Success
            ? known.Index
            : -1;

         if (invented.Success && (cut < 0 || invented.Index < cut)) cut = invented.Index;

         string fallback = cut >= 0
            ? text.Substring(0, cut)
            : text;

         // Detection only, never rewriting: meta-reasoning is not speech. A VALID closed [DIALOGUE] anywhere
         // in the reply means this fallback never runs anyway (ExtractDialogue takes that branch first), but
         // the guard is spelled out so the rule reads as "never touch a valid [DIALOGUE]".
         if (MetaReasoningGuard.IsMetaReasoning(fallback)
             && !Regex.IsMatch(text, $@"\[{DialogueTag}\].*?\[/{DialogueTag}\]",
                RegexOptions.Singleline | RegexOptions.IgnoreCase))
            return string.Empty;

         return fallback;
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
      ///   Like <see cref="ExtractSection" /> but tolerant of a MISSING close tag, the way a reply cut off by
      ///   the token limit arrives (the model opened [QUEST] and never reached [/QUEST]). A closed block is
      ///   returned as before; an open block with no close returns its body up to the next section boundary
      ///   (<see cref="TrimAtFirstSection" />), so a truncated but promise-shaped block still reaches the
      ///   consumer instead of vanishing silently. Used for the sections where a silent loss is a broken word
      ///   (the QUEST family); DIALOGUE has its own open-tolerance in <see cref="ExtractDialogue" />.
      /// </summary>
      private static string? ExtractSectionTolerant(string text, string tag)
      {
         string? closed = ExtractSection(text, tag);
         if (closed != null) return closed;

         Match open = Regex.Match(text, $@"\[{Regex.Escape(tag)}\]", RegexOptions.IgnoreCase);

         return open.Success
            ? TrimAtFirstSection(text.Substring(open.Index + open.Length))
            : null;
      }

      private static string? GetField(IReadOnlyDictionary<string, string> fields, string key)
         => fields.TryGetValue(key, out string? v)
            ? v
            : null;

      private static string? NullIfBlank(string? value)
         => string.IsNullOrWhiteSpace(value)
            ? null
            : value!.Trim();

      /// <summary>
      ///   Extracts all [ACTION] sections (multiple allowed per response).
      ///   Each section must specify a <c>type</c>; sections missing it are skipped.
      /// </summary>
      private static IReadOnlyList<GameAction> ParseActions(string text)
      {
         var actions = new List<GameAction>();

         // A model that EXPLAINS the [ACTION] format inside its spoken [DIALOGUE] must not have its example
         // executed against the game. Scan the reply with the dialogue body removed, so only real,
         // out-of-dialogue action blocks (and any stray invented tag outside the dialogue) ever fire.
         string scanText = Regex.Replace(text, $@"\[{DialogueTag}\].*?\[/{DialogueTag}\]", " ",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

         var pattern = $@"\[{ActionTag}\](.*?)\[/{ActionTag}\]";

         foreach (Match match in Regex.Matches(scanText, pattern,
                     RegexOptions.Singleline | RegexOptions.IgnoreCase))
         {
            Dictionary<string, string> fields = ParseKeyValueLines(match.Groups[1].Value);

            if (!fields.TryGetValue(ActionTypeKey, out string? type) || string.IsNullOrWhiteSpace(type))
               continue;

            fields.TryGetValue(ActionContextKey, out string? context);

            // Everything else becomes parameters.
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kvp in fields)
            {
               if (string.Equals(kvp.Key, ActionTypeKey, StringComparison.OrdinalIgnoreCase)) continue;
               if (string.Equals(kvp.Key, ActionContextKey, StringComparison.OrdinalIgnoreCase)) continue;
               parameters[kvp.Key] = kvp.Value;
            }

            actions.Add(new GameAction {
               Type = NormalizeActionType(type),
               Context = context,
               Parameters = parameters
            });
         }

         // A trailing [ACTION] cut off before its [/ACTION] (a max-tokens truncation) is invisible to the
         // closed-only match above and would be lost silently. Catch the LAST [ACTION] open with no close after
         // it, and parse its body up to the next section boundary, so a truncated action still fires.
         MatchCollection opens = Regex.Matches(scanText, $@"\[{ActionTag}\]", RegexOptions.IgnoreCase);
         if (opens.Count > 0)
         {
            Match lastOpen = opens[opens.Count - 1];
            string afterOpen = scanText.Substring(lastOpen.Index + lastOpen.Length);

            if (!Regex.IsMatch(afterOpen, $@"\[/{ActionTag}\]", RegexOptions.IgnoreCase))
            {
               Dictionary<string, string> truncFields = ParseKeyValueLines(TrimAtFirstSection(afterOpen));

               if (truncFields.TryGetValue(ActionTypeKey, out string? truncType) && !string.IsNullOrWhiteSpace(truncType))
               {
                  truncFields.TryGetValue(ActionContextKey, out string? truncContext);
                  var truncParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                  foreach (KeyValuePair<string, string> kvp in truncFields)
                  {
                     if (string.Equals(kvp.Key, ActionTypeKey, StringComparison.OrdinalIgnoreCase)) continue;
                     if (string.Equals(kvp.Key, ActionContextKey, StringComparison.OrdinalIgnoreCase)) continue;
                     truncParameters[kvp.Key] = kvp.Value;
                  }

                  actions.Add(new GameAction {Type = NormalizeActionType(truncType), Context = truncContext, Parameters = truncParameters});
               }
            }
         }

         // Last resort: a model that invented its own tag instead of the taught [ACTION] block still MEANT to
         // move the regard. Only when it emitted no real change_relation of its own, so a recovered one can
         // never double-count a proper emission.
         // Deliberately scans the FULL text, dialogue included: an INVENTED [RELATION CHANGE] the NPC wrote
         // inside its own speech is the reported Fakade case (the regard the NPC promised must move), which is
         // the opposite of the taught [ACTION] example A7 excludes from the block scan above. A narrow recovery
         // the RelationGate still caps, so recovering an in-dialogue one can never become an exploit.
         if (!actions.Exists(a => string.Equals(a.Type, ChangeRelationAction, StringComparison.OrdinalIgnoreCase)))
         {
            GameAction? recovered = RecoverStrayRelationChange(text);
            if (recovered != null) actions.Add(recovered);
         }

         // A model tuned for tool-calling answers with JSON instead of our [ACTION] block. Nothing parsed it,
         // so the regard never moved AND the raw call was spoken aloud (Nexus, 2026-07-19).
         foreach (GameAction call in RecoverJsonActionCalls(text))
            if (!actions.Exists(a => string.Equals(a.Type, call.Type, StringComparison.OrdinalIgnoreCase)))
               actions.Add(call);

         return actions;
      }

      /// <summary>
      ///   Recovers actions a model emitted as a function CALL, <c>{"name": "change_relation", "parameters":
      ///   {"delta": 5}}</c>, rather than in the taught [ACTION] block.
      ///   <para>
      ///     Unlike <see cref="RecoverStrayRelationChange" /> this is not a guess and so is not restricted to
      ///     one action type: that JSON shape cannot occur in roleplay prose, so its presence IS the intent.
      ///     Safety is unchanged either way, because every recovered action still goes through the host's
      ///     bridge, which re-validates and (for regard) caps and rate-limits it. The prompt proposes; the
      ///     bridge rules.
      ///   </para>
      ///   An action the model ALSO emitted properly wins, so a well-formed block is never double-counted.
      /// </summary>
      private static List<GameAction> RecoverJsonActionCalls(string text)
      {
         var found = new List<GameAction>();

         foreach (Match call in Regex.Matches(text, JsonActionCall, RegexOptions.IgnoreCase))
         {
            string name = call.Groups["name"].Value.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match p in Regex.Matches(call.Groups["args"].Value,
                        "\"(?<k>[A-Za-z_][A-Za-z0-9_]*)\"[ \t]*:[ \t]*\"?(?<v>[^\",}]+)\"?"))
               parameters[p.Groups["k"].Value] = p.Groups["v"].Value.Trim();

            found.Add(new GameAction {Type = NormalizeActionType(name), Parameters = parameters});
         }

         return found;
      }

      /// <summary>Removes any recovered function-call JSON from a spoken line, so the NPC never recites it.</summary>
      private static string StripJsonActionEnvelopes(string dialogue)
         => string.IsNullOrEmpty(dialogue)
            ? dialogue
            : Regex.Replace(dialogue, JsonActionCall, "", RegexOptions.IgnoreCase);

      /// <summary>
      ///   Recovers a relation change a model wrote under a tag it invented. A player watched an NPC speak the
      ///   words "[RELATION CHANGE] delta = 2" aloud while the regard never moved (Fakade, 2026-07-13): the tag
      ///   was not one we teach, so nothing parsed it, and the block fell through into the spoken line.
      ///   <para>
      ///     Stripping it from the dialogue is the guarantee. Recovering it is the honesty: the NPC plainly meant
      ///     to move the regard, and silently dropping that would leave their word empty, which is the very
      ///     hollow-promise failure this codebase fights everywhere else. Safe to be generous here, because the
      ///     host's RelationGate still caps and rate-limits whatever comes through: the prompt proposes, the gate
      ///     rules. A hallucinated number cannot become an exploit.
      ///   </para>
      ///   Deliberately narrow: only an ALL-CAPS standalone tag whose letters contain "RELATION", and only when a
      ///   signed delta follows it. Anything else is stripped from view and left unrecovered rather than guessed.
      /// </summary>
      private static GameAction? RecoverStrayRelationChange(string text)
      {
         foreach (Match tag in Regex.Matches(text, StandaloneTagLine))
         {
            string name = tag.Value.Trim().Trim('[', ']').Replace(" ", "").Replace("_", "");

            if (name.IndexOf("RELATION", StringComparison.OrdinalIgnoreCase) < 0) continue;

            string rest = text.Substring(tag.Index + tag.Length);
            Match delta = Regex.Match(rest, @"delta[ \t]*[:=][ \t]*(?<n>[+-]?\d{1,3})", RegexOptions.IgnoreCase);

            if (!delta.Success) continue;

            return new GameAction {
               Type = ChangeRelationAction,
               Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                  ["delta"] = delta.Groups["n"].Value
               }
            };
         }

         return null;
      }

      private static DiscoveredTrait? ParseDiscovery(string? section)
      {
         if (string.IsNullOrWhiteSpace(section)) return null;
         Dictionary<string, string> fields = ParseKeyValueLines(section!);

         if (!fields.TryGetValue(DiscoveryKeyKey, out string? key)) return null;
         if (!fields.TryGetValue(DiscoveryDescriptionKey, out string? description)) return null;
         if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(description)) return null;

         // GameDay is 0 here — the consumer stamps the real day when persisting.
         return new DiscoveredTrait {Key = key.Trim(), Description = description.Trim(), GameDay = 0};
      }

      private static ParsedEventData? ParseEventData(string? section)
      {
         if (string.IsNullOrWhiteSpace(section)) return null;
         Dictionary<string, string> fields = ParseKeyValueLines(section!);

         if (!fields.TryGetValue(EventTypeKey, out string? typeStr)) return null;
         if (!fields.TryGetValue(SummaryKey, out string? summary)) return null;

         return new ParsedEventData(ParseEventType(typeStr), summary);
      }

      private static NotableEventType ParseEventType(string raw)
      {
         string v = raw.TrimStart(trimChars: '#').Trim();

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

         foreach (string rawLine in section.Split(separator: '\n'))
         {
            // Strip a leading markdown list / emphasis marker some models prefix ("- type:", "* type:",
            // "`type`:"): weaker models JSON-ify or bullet their blocks, and the marker must not become part
            // of the key or hide the line from the parser.
            string line = rawLine.Trim().TrimStart('-', '*', '`', ' ', '\t');

            if (line.Length == 0) continue;

            // Accept 'key = value' as well as 'key: value'. Models write both, and a player was shown a block
            // reading "delta = 2" (Fakade, 2026-07-13): had the model wrapped it in a correct [ACTION] tag, this
            // parser would STILL have dropped it silently, because it only ever looked for a colon. The first
            // separator wins, so a colon inside a value ("summary: he said: no") still keys on the first one.
            int sepIdx = line.IndexOfAny(KeyValueSeparators);

            if (sepIdx <= 0) continue;

            string key = NormalizeKey(line.Substring(0, sepIdx));
            string value = StripValueWrappers(line.Substring(sepIdx + 1).Trim());

            if (value.StartsWith("#", StringComparison.Ordinal)) value = value.Substring(1).Trim();
            if (key.Length == 0) continue;

            dict[key] = value;
         }

         return dict;
      }

      /// <summary>
      ///   Folds a key to the canonical form the lookups use: strips markdown emphasis / quotes around it and
      ///   turns runs of spaces and dashes into the underscore the canonical keys carry, so "reward gold",
      ///   "reward-gold" and "reward_gold" all resolve to the same field. Comparison is already case-insensitive.
      /// </summary>
      private static string NormalizeKey(string raw)
      {
         string k = raw.Trim().Trim('*', '`', '"', '\'', ' ', '\t');

         return Regex.Replace(k, @"[ \t\-]+", "_");
      }

      /// <summary>
      ///   Removes ONE matching pair of surrounding quotes or backticks a JSON-minded model wrapped a value in
      ///   (<c>type: "bandit_clear"</c> or <c>type: `bandit_clear`</c>), so the wrapper does not become part of
      ///   the value and defeat the alias tables. A value with no matching pair is returned unchanged.
      /// </summary>
      private static string StripValueWrappers(string raw)
      {
         string v = raw.Trim();

         if (v.Length >= 2)
         {
            char first = v[0];
            char last = v[v.Length - 1];

            if ((first == '"' && last == '"') || (first == '\'' && last == '\'') || (first == '`' && last == '`'))
               v = v.Substring(1, v.Length - 2).Trim();
         }

         return v;
      }

      /// <summary>
      ///   Folds an action type to the canonical snake_case the bridge matches on: runs of spaces and dashes
      ///   become one underscore, so "give gold" and "give-gold" both reach "give_gold". Deliberately NOT an
      ///   alias table: the bridge already lowercases, and synonyms like "pay" are ambiguous (player pays vs NPC
      ///   pays), so guessing one could fire the WRONG transfer. Only the unambiguous separator fold is applied.
      /// </summary>
      private static string NormalizeActionType(string raw)
         => Regex.Replace(raw.Trim(), @"[ \t\-]+", "_");

      private static ConversationMemory? ParseMemory(string? section)
      {
         if (string.IsNullOrWhiteSpace(section)) return null;
         Dictionary<string, string> fields = ParseKeyValueLines(section!);

         if (!fields.TryGetValue(TopicKey, out string? topic)) return null;
         if (!fields.TryGetValue(SentimentKey, out string? sentiment)) return null;
         fields.TryGetValue(DecisionKey, out string? decision);

         return new ConversationMemory(topic, sentiment, decision);
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
         bool typeNamed = fields.TryGetValue("type", out string? typeStr);
         QuestType? type = typeNamed
            ? ParseQuestType(typeStr!)
            : null;

         // A type that was NAMED but did not resolve is a hallucination, not the "no type given" wildcard: flag
         // it so the consumer ignores the claim instead of abandoning the first outstanding quest by accident.
         return new QuestAbandonClaim {Type = type, TypeUnrecognized = typeNamed && type == null};
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
         bool typeNamed = fields.TryGetValue("type", out string? typeStr);
         QuestType? type = typeNamed
            ? ParseQuestType(typeStr!)
            : null;

         // Same discipline as abandonment: a named-but-unrecognised type is flagged so the consumer ignores it
         // rather than paying out the first awaiting-reward quest on a hallucinated token.
         return new QuestCompletionClaim {Type = type, TypeUnrecognized = typeNamed && type == null};
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

         if (!fields.TryGetValue("type", out string? typeStr)) return null;
         QuestType? type = ParseQuestType(typeStr);

         if (type == null) return null;

         fields.TryGetValue("description", out string? description);

         int? deadline = TryParseSignedInt(fields, "deadline_days");
         if (deadline is <= 0) deadline = null;   // 0 or negative means "no deadline"
         if (deadline is > MaxDeadlineDays) deadline = MaxDeadlineDays; // a model that wrote "365" gets a sane cap

         return new QuestProposal {
            Type = type.Value,
            Description = description?.Trim() ?? "",
            TargetSettlement = NullIfBlank(GetField(fields, "target_settlement")),
            TargetHero = NullIfBlank(GetField(fields, "target_hero")),
            TargetFaction = NullIfBlank(GetField(fields, "target_faction")),
            DeadlineDays = deadline,
            RewardGold = ClampNonNegative(TryParseSignedInt(fields, "reward_gold")),
            RewardRelation = ClampNonNegative(TryParseSignedInt(fields, "reward_relation")),
            Reward = fields.TryGetValue("reward_grant", out string? grantStr)
               ? ParseRewardGrant(grantStr)
               : RewardGrant.None,
            RequiredValue = ClampNonNegative(TryParseSignedInt(fields, "required_value")),
            Category = NullIfBlank(GetField(fields, "category")),
            RequiredCount = ClampNonNegative(TryParseSignedInt(fields, "required_count")),
            MarriageSpouse = NullIfBlank(GetField(fields, "spouse"))
         };
      }

      /// <summary>
      ///   Maps the LLM's quest-type token (snake_case or the enum name) to a
      ///   <see cref="QuestType" />. Returns null for unrecognized tokens.
      /// </summary>
      private static QuestType? ParseQuestType(string raw)
      {
         string v = raw.TrimStart(trimChars: '#').Trim().ToLowerInvariant();

         if (Enum.TryParse(v, true, out QuestType direct)) return direct;

         return v switch {
            "bandit_clear" or "bandits" or "clear_bandits" => QuestType.BanditClear,
            "bandit_hideout" or "hideout" or "clear_hideout" => QuestType.BanditHideout,
            "attack_faction" or "raid_faction" or "war_faction" => QuestType.AttackFaction,
            "attack_lord" or "defeat_lord" or "fight_lord" => QuestType.AttackLord,
            "raid_village" or "burn_village" => QuestType.RaidVillage,
            "attack_caravan" or "raid_caravan" => QuestType.AttackCaravan,
            "siege" or "besiege" or "siege_town" => QuestType.Siege,
            "capture_prisoner" or "capture" or "take_prisoner" => QuestType.CapturePrisoner,
            "execute_enemy" or "execute" or "kill_enemy" => QuestType.ExecuteEnemy,
            "rescue_prisoner" or "rescue" or "free_prisoner" => QuestType.RescuePrisoner,
            "deliver_letter"
               or "letter"
               or "carry_letter"
               or "deliver_message"
               or "carry_message" => QuestType.DeliverLetter,
            "provide_gold"
               or "child_support"
               or "support_child"
               or "give_gold_quest" => QuestType.ProvideGold,
            "scout_army"
               or "scout"
               or "spy_army"
               or "locate_army"
               or "find_army" => QuestType.ScoutArmy,
            "deliver_items"
               or "deliver_goods"
               or "give_items"
               or "pay_in_goods"
               or "barter_items" => QuestType.DeliverItems,
            "deliver_prisoner"
               or "hand_over_prisoner"
               or "bring_prisoner"
               or "deliver_captive" => QuestType.DeliverPrisoner,
            "declare_war"
               or "declare_war_on"
               or "go_to_war"
               or "make_war" => QuestType.DeclareWar,
            "nemesis_bounty"
               or "bounty"
               or "hunt_nemesis" => QuestType.NemesisBounty,
            "provide_goods"
               or "provide_supplies"
               or "supply"
               or "supplies"
               or "bring_goods"
               or "bring_supplies" => QuestType.ProvideGoods,
            _ => null
         };
      }

      private static ReputationDelta? ParseReputation(string? section)
      {
         if (string.IsNullOrWhiteSpace(section)) return null;
         Dictionary<string, string> fields = ParseKeyValueLines(section!);

         int? clanDelta = TryParseSignedInt(fields, ClanDeltaKey);
         int? factionDelta = TryParseSignedInt(fields, FactionDeltaKey);

         if (clanDelta is null && factionDelta is null) return null;

         return new ReputationDelta(clanDelta, factionDelta);
      }

      /// <summary>
      ///   Maps the LLM's reward-grant token to a <see cref="RewardGrant" />. Returns
      ///   <see cref="RewardGrant.None" /> for an unrecognized or absent token — an
      ///   ordinary gold/relation quest.
      /// </summary>
      private static RewardGrant ParseRewardGrant(string raw)
      {
         string v = raw.TrimStart('#').Trim().ToLowerInvariant();

         if (Enum.TryParse(v, true, out RewardGrant direct)) return direct;

         return v switch {
            "join_party"
               or "join"
               or "recruit"
               or "take_service"
               or "companion"
               or "service" => RewardGrant.JoinParty,
            "give_item" or "gift_item" or "hand_over_item" => RewardGrant.GiveItem,
            "give_troops" or "grant_troops" or "lend_troops" => RewardGrant.GiveTroops,
            "marriage_consent"
               or "marriage"
               or "marry"
               or "consent_marriage"
               or "betrothal" => RewardGrant.MarriageConsent,
            "release_prisoner"
               or "free_prisoner"
               or "hand_over_prisoner" => RewardGrant.ReleasePrisoner,
            _ => RewardGrant.None
         };
      }

      private static StanceShiftData? ParseStanceShift(string? section)
      {
         if (string.IsNullOrWhiteSpace(section)) return null;
         Dictionary<string, string> fields = ParseKeyValueLines(section!);

         int trust = TryParseSignedInt(fields, "trust") ?? 0;
         int respect = TryParseSignedInt(fields, "respect") ?? 0;
         int fear = TryParseSignedInt(fields, "fear") ?? 0;

         if (trust == 0 && respect == 0 && fear == 0) return null;

         return new StanceShiftData {Trust = trust, Respect = respect, Fear = fear};
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

            if (!fields.TryGetValue("name", out string? name) || string.IsNullOrWhiteSpace(name)) continue;
            if (!fields.TryGetValue("text", out string? reactionText) || string.IsNullOrWhiteSpace(reactionText)) continue;

            reactions.Add(new WitnessReaction {
               Name = name.Trim(),
               Text = reactionText.Trim()
            });
         }

         return reactions;
      }

      /// <summary>
      ///   Extracts all [RESOLUTION] sections (multiple allowed per response), mirroring
      ///   <see cref="ParseActions" />'s conventions: a section missing <c>type</c> is skipped, a trailing
      ///   block cut off before its close tag (a max-tokens truncation) is still recovered, and a block a
      ///   model merely EXPLAINS inside its own [DIALOGUE] is not recorded as a real decision (the scan text
      ///   has the dialogue body blanked out first, same as an [ACTION] example would be).
      /// </summary>
      private static IReadOnlyList<ResolutionProposal> ParseResolutions(string text)
      {
         var resolutions = new List<ResolutionProposal>();

         string scanText = Regex.Replace(text, $@"\[{DialogueTag}\].*?\[/{DialogueTag}\]", " ",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

         var pattern = $@"\[{ResolutionTag}\](.*?)\[/{ResolutionTag}\]";

         foreach (Match match in Regex.Matches(scanText, pattern,
                     RegexOptions.Singleline | RegexOptions.IgnoreCase))
         {
            ResolutionProposal? resolution = BuildResolution(ParseKeyValueLines(match.Groups[1].Value));
            if (resolution != null) resolutions.Add(resolution);
         }

         // A trailing [RESOLUTION] cut off before its [/RESOLUTION] (max-tokens truncation) would otherwise be
         // lost silently, the same broken-word risk ParseActions and the QUEST family already guard against.
         MatchCollection opens = Regex.Matches(scanText, $@"\[{ResolutionTag}\]", RegexOptions.IgnoreCase);
         if (opens.Count > 0)
         {
            Match lastOpen = opens[opens.Count - 1];
            string afterOpen = scanText.Substring(lastOpen.Index + lastOpen.Length);

            if (!Regex.IsMatch(afterOpen, $@"\[/{ResolutionTag}\]", RegexOptions.IgnoreCase))
            {
               ResolutionProposal? truncated = BuildResolution(ParseKeyValueLines(TrimAtFirstSection(afterOpen)));
               if (truncated != null) resolutions.Add(truncated);
            }
         }

         return resolutions;
      }

      /// <summary>A [RESOLUTION] block missing "type" is not a decision at all: skipped, like a typeless [ACTION].</summary>
      private static ResolutionProposal? BuildResolution(Dictionary<string, string> fields)
      {
         if (!fields.TryGetValue(ActionTypeKey, out string? type) || string.IsNullOrWhiteSpace(type)) return null;

         fields.TryGetValue("actor", out string? actor);
         fields.TryGetValue("detail", out string? detail);

         return new ResolutionProposal {
            Type = NormalizeActionType(type),
            Actor = NullIfBlank(actor),
            Detail = NullIfBlank(detail),
            TargetSettlement = NullIfBlank(GetField(fields, "target_settlement")),
            TargetRole = NullIfBlank(GetField(fields, "target_role")),
            TargetMission = NullIfBlank(GetField(fields, "target_mission")),
            // Tolerant, like every other numeric field this parser reads (TryParseSignedInt): a model that wraps
            // the figure ("100 denars", "100/day") must not lose the amount, and an absent/unparseable value
            // simply leaves this null rather than throwing, mirroring deadline_days and the relation deltas.
            // Shared by "grant_stipend" (denars/day) and, Partie 1, "give_gold" (a one-off denars pledge): no
            // per-kind field needed, since both ground on the same single figure.
            TargetAmount = TryParseSignedInt(fields, "target_amount"),
            // "declare_war"'s own grounding field, mirroring target_settlement: the faction named, read back by
            // the mod's CouncilLift.SettleDeclareWar (the first FACTION-CENTRIC executor).
            TargetFaction = NullIfBlank(GetField(fields, "target_faction")),
            // "pledge_against"'s own grounding field (Partie 1, COUNCIL_ACTIONS.md's schemes block): the named
            // rival, read back by the mod's CouncilLift.SettlePledgeAgainst via CourtActionResolver's own
            // tolerant name match, exactly like the 1:1 pledge_against action already resolves its "target" param.
            TargetRival = NullIfBlank(GetField(fields, "target_rival")),
            // "arrange_marriage"'s own TWO grounding fields (Partie 1, COUNCIL_ACTIONS.md, "juste arrange_mariage
            // qui est valide"): the SAME player_kin/target_kin vocabulary the 1:1 arrange_marriage GameAction
            // already teaches, read back by the mod (CalradiaRemembers.Logic.Assemblies.ArrangeMarriageGroundingCodec
            // composes both into the entry's one TargetHint slot at record time, split again at the lift,
            // CouncilLift.SettleArrangeMarriage).
            PlayerKinName = NullIfBlank(GetField(fields, "player_kin")),
            TargetKinName = NullIfBlank(GetField(fields, "target_kin")),
            // "swap_fiefs"'s own TWO grounding fields (Partie 8, COUNCIL_ACTIONS.md's Kimi review): mirrors
            // player_kin/target_kin's own naming exactly, read back by the mod (FiefSwapGroundingCodec composes
            // both into the entry's one TargetHint slot at record time, split again at the lift,
            // CouncilLift.SettleSwapFiefs).
            PlayerFiefName = NullIfBlank(GetField(fields, "player_fief")),
            TargetFiefName = NullIfBlank(GetField(fields, "target_fief")),
            // "release_prisoner"'s own grounding field (Parley toolkit, rounding out make_peace + tribute): the
            // named hero captive the player holds, read back by the mod (CouncilLift.SettleReleasePrisoner) via
            // BuyPrisonerVerb's own tolerant name match over the player's held prisoners, exactly like
            // target_rival is read back by CourtActionResolver's own tolerant match for pledge_against.
            TargetPrisonerName = NullIfBlank(GetField(fields, "target_prisoner")),
            // "swear_oath"'s own grounding field (R7-light, COUNCIL_ACTIONS.md's Partie 8): WHICH whitelisted
            // oath kind (pay_gold/keep_peace/protect), read back by the mod's OathKindParser; that kind's own
            // TARGET value reuses target_amount (pay_gold) or target_faction (keep_peace) above, no new field.
            OathKind = NullIfBlank(GetField(fields, "oath_kind")),
            // "give_hostage"'s own grounding field (Kimi's "v1 invite d'honneur", COUNCIL_ACTIONS.md's Partie 8):
            // the named eligible relative of the seated Parley leader, read back by the mod
            // (CalradiaRemembers.Assemblies.HostageCandidateResolver.ResolveByName) via the same tolerant name
            // match pattern target_rival/target_prisoner already use.
            TargetHostageName = NullIfBlank(GetField(fields, "target_hostage"))
         };
      }

      /// <summary>Returns the text up to the first recognized section boundary (or all of it).</summary>
      private static string TrimAtFirstSection(string text)
      {
         const string pattern = @"\[(?:/DIALOGUE|NARRATION|MEMORY|EVENT|REPUTATION|STANCE|ACTION|DISCOVERY|QUEST_COMPLETE|QUEST_ABANDON|QUEST|WITNESS_REACTION|RESOLUTION)\]";
         Match known = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

         // An invented section closes an unclosed [DIALOGUE] too, for the same reason it ends the fallback.
         Match invented = Regex.Match(text, StandaloneTagLine);

         int cut = known.Success
            ? known.Index
            : -1;

         if (invented.Success && (cut < 0 || invented.Index < cut)) cut = invented.Index;

         return cut >= 0
            ? text.Substring(0, cut)
            : text;
      }

      private static int? TryParseSignedInt(IReadOnlyDictionary<string, string> fields, string key)
      {
         if (!fields.TryGetValue(key, out string? raw) || raw == null) return null;

         // Permissive: pull the first signed integer out of a value a model dressed up ("500 denars", "1,000",
         // "+3 relation"), dropping thousands separators first, so a well-meant number is not lost for its
         // wrapping. Every consumer clamps or gates the result (ClampNonNegative, RelationGate), so being
         // generous here can never become an exploit.
         Match m = Regex.Match(raw.Replace(",", ""), @"[+-]?\d+");

         return m.Success && int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? v
            : null;
      }

      #endregion
   }
}
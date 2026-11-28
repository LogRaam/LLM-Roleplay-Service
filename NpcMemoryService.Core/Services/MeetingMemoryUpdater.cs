// Code written by Gabriel Mailhot, 28/11/2026.
// Stage 2 of the group-scene plan, "what a meeting leaves". Gabriel, 28/11/2026: "go pour tout le système ... je testerai
// demain". The simulator's Remember call (Calradia Remembers, CalradiaRemembers.Tests/Conversation/
// GroupConversationSimulation.cs), ported for a talk between one character and the player: one background call at a
// chat's close UPDATES the character's MeetingMemory. It appends a lasting change, sets the bond, consolidates the whole
// list of unsettled matters, marks the secrets told and keeps a new one. It never rewrites the record whole.
//
// What was left out of the simulator's prompt, on purpose:
//  - the "event:" lines. The host already writes the meeting as an event (NotableEvent) through its end-of-chat summary;
//    a second account of it would compete with that one.
//  - "name its day ('on day N')". In the game no day number is said aloud and no appointment can be kept
//    (PromptBuilder.AppendNoAppointments), so the model is told to name no time to come at all.
//
// The bond is PROMPT TEXTURE ONLY: nothing here, or anywhere, feeds it to ReputationWithPlayer, RelationGate, progressive
// regard, a gate or an action.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;

#endregion

namespace NpcMemoryService.Core.Services
{
   /// <summary>What the update call answered, read from its reply (<see cref="MeetingMemoryUpdater.Parse" />).</summary>
   public sealed class MeetingMemoryReply
   {
      /// <summary>What the meeting changed that will last; empty for "nothing".</summary>
      public string Changed { get; init; } = "";

      /// <summary>The bond as the reply gave it; null when no bond line could be read (the old bond is then kept).</summary>
      public MeetingBond? Bond { get; init; }

      /// <summary>Whether the reply had an "open:" line at all. Without one the old list is kept; "open: nothing" clears it.</summary>
      public bool HasOpenLine { get; init; }

      /// <summary>The whole list of unsettled matters, consolidated by the model.</summary>
      public IReadOnlyList<string> Open { get; init; } = new List<string>();

      /// <summary>The numbers (1-based, as shown in the request) of the secrets told in this meeting.</summary>
      public IReadOnlyList<int> Told { get; init; } = new List<int>();

      /// <summary>A new secret this meeting gave the character; empty for "nothing".</summary>
      public string NewSecret { get; init; } = "";
   }

   /// <summary>The facts an update is applied with.</summary>
   public sealed class MeetingMemoryApplyFacts
   {
      /// <summary>The campaign day of the meeting.</summary>
      public int Day { get; init; }

      /// <summary>The player's name, as the record is about them and secrets are told to them.</summary>
      public string PlayerName { get; init; } = "";

      /// <summary>The meeting's memory-anchor id; applying the same meeting again replaces what it wrote.</summary>
      public string? AnchorId { get; init; }

      /// <summary>The secrets as numbered in the request (texts, in order), which the reply's "told:" numbers refer to.</summary>
      public IReadOnlyList<string> SecretsShown { get; init; } = new List<string>();
   }

   /// <summary>
   ///   Builds, sends, reads and applies the one background call that updates what a character carries away from a meeting
   ///   with the player (<see cref="MeetingMemory" />). The host runs it beside its end-of-chat summary.
   /// </summary>
   public sealed class MeetingMemoryUpdater
   {
      private static readonly string[] BondParts = {"understands", "forgives", "loves", "trusts"};

      private readonly ILlmClient _llmClient;

      public MeetingMemoryUpdater(ILlmClient llmClient)
      {
         _llmClient = llmClient;
      }

      /// <summary>The call's shape: short, steady, no reasoning (the simulator ran it at 0.3).</summary>
      public LlmParameters Parameters { get; init; } = new() {
         MaxTokens = 500,
         Creativity = 0.3f,
         ReasoningOverride = "off",
         AllowTruncationRetry = false
      };

      /// <summary>
      ///   Asks the model what this meeting left and applies it to <paramref name="npc" />'s record. Returns true when the
      ///   record was updated, false when the call failed or answered nothing readable (the record is then untouched).
      /// </summary>
      public async Task<bool> UpdateAsync(
         NpcProfile npc, string transcript, string playerName, int day, string? anchorId,
         string? replyLanguage = null, bool? playerIsFemale = null, CancellationToken ct = default)
      {
         if (npc == null || string.IsNullOrWhiteSpace(transcript)) return false;

         MeetingMemory? before = RecordFor(npc.MeetingMemory, playerName);
         List<string> shown = UntoldTo(before, playerName);
         var request = new LlmRequest {
            SystemPrompt = BuildSystemPrompt(npc.Name, playerName, replyLanguage, playerIsFemale),
            Messages = [new LlmMessage(MessageRole.User, BuildUserMessage(npc.Name, playerName, before, shown, transcript))],
            Parameters = Parameters
         };

         LlmResponse? response = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);
         if (response is not {IsSuccess: true} || string.IsNullOrWhiteSpace(response.Content)) return false;

         return ApplyReply(npc, response.Content!, new MeetingMemoryApplyFacts {Day = day, PlayerName = playerName, AnchorId = anchorId, SecretsShown = shown});
      }

      /// <summary>
      ///   Reads <paramref name="content" /> and applies it to the profile's record. The seam a test or a self-test feeds a
      ///   fixed reply through. False when the reply is the model thinking aloud or holds no line this call asks for.
      /// </summary>
      public static bool ApplyReply(NpcProfile npc, string content, MeetingMemoryApplyFacts facts)
      {
         if (npc == null || string.IsNullOrWhiteSpace(content) || MetaReasoningGuard.IsMetaReasoning(content)) return false;

         MeetingMemoryReply? reply = Parse(content);
         if (reply == null) return false;

         npc.MeetingMemory = Apply(npc.MeetingMemory, reply, facts);

         return true;
      }

      /// <summary>The secrets of the record not yet told to the player, in the order the request numbers them.</summary>
      public static List<string> UntoldTo(MeetingMemory? record, string playerName) => record?.UntoldTo(playerName) ?? new List<string>();

      /// <summary>The system prompt of the update call.</summary>
      public static string BuildSystemPrompt(string npcName, string playerName, string? replyLanguage, bool? playerIsFemale = null)
      {
         string who = string.IsNullOrWhiteSpace(playerName) ? "the player" : playerName;
         var sb = new StringBuilder();
         sb.AppendLine($"You write what {npcName} carries away from a conversation with {who}, as {npcName}'s own memory, for the next time they meet.");
         sb.AppendLine($"Lines beginning \"{npcName}:\" are {npcName}'s. \"I\" is always {npcName}.");
         string pronouns = PlayerPronounLine.For(who, playerIsFemale);
         if (pronouns.Length > 0) sb.AppendLine(pronouns);
         sb.AppendLine("Use only what happened in the conversation and what is given; invent nothing.");
         sb.AppendLine("Name no time to come: no date, no day, no \"tomorrow\" or \"in three days\", which will be false when this is read again.");
         sb.AppendLine("Answer exactly these lines:");
         sb.AppendLine($"changed: <what this meeting changed in me toward {who} that will last, consolidated into one or two sentences; or nothing>");
         sb.AppendLine($"bond: <toward {who}: understands yes/partly/no, forgives yes/partly/no, loves yes/partly/no, trusts yes/partly/no> (each moves only for a cause in this conversation; otherwise keep it as it was)");
         // The simulator's first three-meeting run: one list held the same matter three times, another ten, none ever settled.
         sb.AppendLine("open: <the WHOLE list of what stands unsettled between us now: every matter from UNSETTLED BEFORE that this conversation did not truly settle, the same matter written once, plus what this conversation left open (a question I still carry, a thing asked and not given, a promise, what I must still decide about them); at most six, separated by ' | '; or nothing>");
         sb.AppendLine($"told: <the number of each of MY SECRETS below that I told {who} in this conversation, in words or made plain by what I said; a secret contained in another one I told counts too; separated by ' | '; or none>");
         sb.AppendLine($"secret: <a new thing this conversation gave me that I now keep from {who}; or nothing>");
         sb.AppendLine("Keep the labels changed, bond, open, told and secret, and the words yes, partly, no, nothing and none, in English.");
         sb.Append("The rest, after each label: ");
         sb.AppendLine(MemoryLanguagePolicy.Directive(replyLanguage, "conversation"));

         return sb.ToString();
      }

      /// <summary>The user message of the update call: the record before, then the conversation.</summary>
      public static string BuildUserMessage(string npcName, string playerName, MeetingMemory? before, IReadOnlyList<string> secretsShown, string transcript)
      {
         string who = string.IsNullOrWhiteSpace(playerName) ? "the player" : playerName;
         var sb = new StringBuilder();
         if (before?.Bond?.HasContent == true) sb.AppendLine($"MY BOND WITH {who.ToUpperInvariant()} BEFORE THIS CONVERSATION: {BondLine(before.Bond)}").AppendLine();

         sb.AppendLine($"MY SECRETS, STILL KEPT FROM {who.ToUpperInvariant()} BEFORE THIS CONVERSATION:");
         if (secretsShown.Count == 0) sb.AppendLine("(none)");
         for (var i = 0; i < secretsShown.Count; i++) sb.AppendLine($"{i + 1}. {secretsShown[i]}");
         sb.AppendLine();

         List<string> open = before?.Unsettled ?? new List<string>();
         sb.AppendLine("UNSETTLED BEFORE:");
         if (open.Count == 0) sb.AppendLine("(nothing)");
         for (var i = 0; i < open.Count; i++) sb.AppendLine($"{i + 1}. {open[i]}");
         sb.AppendLine();

         sb.AppendLine("THE CONVERSATION:");
         sb.Append(transcript.Trim());

         return sb.ToString();
      }

      /// <summary>
      ///   Reads a reply. Null when it holds none of the lines asked for (nothing to apply). A field answered "nothing" or
      ///   "none" reads as empty.
      /// </summary>
      public static MeetingMemoryReply? Parse(string content)
      {
         if (string.IsNullOrWhiteSpace(content)) return null;

         bool any = false;
         string? Field(string name)
         {
            Match m = Regex.Match(content, $@"(?im)^[\s\*\-#>]*{name}[\s\*]*:[\s\*]*(.*)$");
            if (!m.Success) return null;
            any = true;
            string v = Clean(m.Groups[1].Value);

            return IsNothing(v) ? "" : v;
         }

         string changed = Field("changed") ?? "";
         string? bondText = Field("bond");
         string? openText = Field("open");
         string told = Field("told") ?? "";
         string secret = Field("secret") ?? "";
         if (!any) return null;

         return new MeetingMemoryReply {
            Changed = changed,
            Bond = bondText == null ? null : ParseBond(bondText),
            HasOpenLine = openText != null,
            Open = (openText ?? "").Split('|').Select(Clean).Where(o => o.Length > 0 && !IsNothing(o)).Take(MeetingMemory.MaxUnsettled).ToList(),
            Told = told.Split('|', ',', ';').Select(p => Regex.Match(p, @"\d+")).Where(n => n.Success).Select(n => int.Parse(n.Value)).Distinct().ToList(),
            NewSecret = secret
         };
      }

      /// <summary>
      ///   Applies a reply to a record and returns the record to keep (a fresh one when there was none, or when the old one
      ///   is about another player). Appends the lasting change, sets the bond (keeping a part the reply left out), replaces
      ///   the unsettled list whole, marks secrets told, keeps a new secret. The same meeting applied twice (same anchor)
      ///   replaces its change and its secret rather than adding them again.
      /// </summary>
      public static MeetingMemory Apply(MeetingMemory? record, MeetingMemoryReply reply, MeetingMemoryApplyFacts facts)
      {
         MeetingMemory m = RecordFor(record, facts.PlayerName) ?? new MeetingMemory();
         m.AboutPlayer = string.IsNullOrWhiteSpace(facts.PlayerName) ? m.AboutPlayer : facts.PlayerName;
         m.Changes ??= new List<MeetingChange>();
         m.Unsettled ??= new List<string>();
         m.Secrets ??= new List<MeetingSecret>();
         bool sameMeeting = !string.IsNullOrEmpty(facts.AnchorId);

         string changed = Cut(reply.Changed);
         if (changed.Length > 0)
         {
            if (sameMeeting) m.Changes.RemoveAll(c => c.AnchorId == facts.AnchorId);
            m.Changes.Add(new MeetingChange {Day = facts.Day, Text = changed, AnchorId = facts.AnchorId});
            if (m.Changes.Count > MeetingMemory.MaxChanges) m.Changes.RemoveRange(0, m.Changes.Count - MeetingMemory.MaxChanges);
         }

         if (reply.Bond?.HasContent == true)
         {
            MeetingBond old = m.Bond ?? new MeetingBond();
            m.Bond = new MeetingBond {
               Understands = Pick(reply.Bond.Understands, old.Understands),
               Forgives = Pick(reply.Bond.Forgives, old.Forgives),
               Loves = Pick(reply.Bond.Loves, old.Loves),
               Trusts = Pick(reply.Bond.Trusts, old.Trusts)
            };
         }

         // The one part consolidated whole; a reply with no such line keeps the old list.
         if (reply.HasOpenLine) m.Unsettled = reply.Open.Select(Cut).Where(o => o.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(MeetingMemory.MaxUnsettled).ToList();

         foreach (int n in reply.Told)
         {
            if (n < 1 || n > facts.SecretsShown.Count) continue;
            MeetingSecret? kept = m.Secrets.FirstOrDefault(s => s.Text == facts.SecretsShown[n - 1]);
            if (kept == null) continue;
            kept.ToldTo ??= new List<string>();
            if (!kept.WasToldTo(facts.PlayerName) && !string.IsNullOrWhiteSpace(facts.PlayerName)) kept.ToldTo.Add(facts.PlayerName);
            kept.ToldOnDay = facts.Day;
         }

         string secret = Cut(reply.NewSecret);
         // The simulator's notes: "a new secret sometimes duplicates one already kept".
         if (secret.Length > 0 && !m.Secrets.Any(s => string.Equals(s.Text, secret, StringComparison.OrdinalIgnoreCase) && s.AnchorId != facts.AnchorId))
         {
            if (sameMeeting) m.Secrets.RemoveAll(s => s.AnchorId == facts.AnchorId && !s.WasToldTo(facts.PlayerName));
            m.Secrets.Add(new MeetingSecret {Text = secret, SinceDay = facts.Day, AnchorId = facts.AnchorId});
            while (m.Secrets.Count > MeetingMemory.MaxSecrets)
            {
               // A secret already told goes first; then the oldest.
               MeetingSecret drop = m.Secrets.FirstOrDefault(s => (s.ToldTo?.Count ?? 0) > 0) ?? m.Secrets[0];
               m.Secrets.Remove(drop);
            }
         }

         m.LastUpdatedDay = facts.Day;
         m.LastAnchorId = facts.AnchorId;

         return m;
      }

      /// <summary>The bond as one line, as the update call and the prompt write it.</summary>
      public static string BondLine(MeetingBond bond)
         => string.Join(", ", new[] {("understands", bond.Understands), ("forgives", bond.Forgives), ("loves", bond.Loves), ("trusts", bond.Trusts)}
                                .Where(p => !string.IsNullOrEmpty(p.Item2)).Select(p => $"{p.Item1} {p.Item2}"));

      /// <summary>The record when it is about this player; null when there is none or it is about someone else.</summary>
      public static MeetingMemory? RecordFor(MeetingMemory? record, string playerName) => MeetingMemory.About(record, playerName);

      #region private

      private static MeetingBond? ParseBond(string text)
      {
         var found = new string[4];
         for (var i = 0; i < BondParts.Length; i++)
         {
            // "understands partly", "understands: partly", "Understands - yes"
            Match m = Regex.Match(text, BondParts[i].Substring(0, BondParts[i].Length - 1) + @"\w*\W{1,4}(yes|partly|no)\b", RegexOptions.IgnoreCase);
            found[i] = m.Success ? m.Groups[1].Value.ToLowerInvariant() : "";
         }

         var bond = new MeetingBond {Understands = found[0], Forgives = found[1], Loves = found[2], Trusts = found[3]};

         return bond.HasContent ? bond : null;
      }

      private static string Pick(string fresh, string old) => string.IsNullOrEmpty(fresh) ? old : fresh;

      private static bool IsNothing(string v)
         => v.Length == 0 || v.StartsWith("nothing", StringComparison.OrdinalIgnoreCase) || v.StartsWith("none", StringComparison.OrdinalIgnoreCase)
            || v.Equals("n/a", StringComparison.OrdinalIgnoreCase) || v == "-";

      private static string Clean(string v)
         => v.Trim().Trim('*', '"', '\'', '<', '>', '[', ']', '`').Trim();

      private static string Cut(string v)
      {
         string s = Clean(v ?? "");
         if (s.Length <= MeetingMemory.MaxItemChars) return s;
         int space = s.LastIndexOf(' ', MeetingMemory.MaxItemChars);

         return (space > MeetingMemory.MaxItemChars / 2 ? s.Substring(0, space) : s.Substring(0, MeetingMemory.MaxItemChars)).TrimEnd(',', ';', ' ') + "...";
      }

      #endregion
   }
}

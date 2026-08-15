// Code written by Gabriel Mailhot, 13/08/2026.

#region

using System.Collections.Generic;
using System.Text;
using NpcMemoryService.Core.Actions;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   Builds the lean system prompt for the PROSE + INTERPRETER spike's SECOND model: an ACTION INTERPRETER that
   ///   reads a roleplay reply already written by a first model and emits only the structured [ACTION]/[EVENT] tags
   ///   the prose implies. The emitted format mirrors what <see cref="Parsing.SectionResponseParser" /> already
   ///   parses, so the existing pipeline consumes the interpreter's output unchanged.
   ///   <para>
   ///     The prompt is assembled STABLE-PREFIX FIRST (<see cref="StablePrefix" />, identical on every call, so a
   ///     provider can prompt-cache it) followed by the VARIABLE per-turn tail (the caller's context facts and the
   ///     prose to analyze). Pure and stateless: no engine or per-turn data leaks into the cacheable prefix.
   ///   </para>
   /// </summary>
   public static class ActionInterpreterPromptBuilder
   {
      private static readonly string _stablePrefix = BuildStablePrefix();

      /// <summary>
      ///   The invariant head of every interpreter prompt: the action interpreter's role, the tag vocabulary, the
      ///   exact output format, and the hard "do NOT rewrite" rule. Exposed so a test can assert a built prompt
      ///   STARTS with it (the prefix-first ordering that lets a provider cache this part across calls).
      /// </summary>
      internal static string StablePrefix => _stablePrefix;

      /// <summary>
      ///   Assembles a full interpreter prompt: the stable prefix, then the caller's <paramref name="contextFacts" />
      ///   (a short line such as the NPC name and current regard), then a delimiter, then the
      ///   <paramref name="prose" /> to analyze. The result always begins with <see cref="StablePrefix" />.
      /// </summary>
      public static string Build(string prose, string contextFacts)
      {
         var sb = new StringBuilder();

         sb.Append(_stablePrefix);
         sb.AppendLine();
         sb.AppendLine(contextFacts?.Trim() ?? string.Empty);
         sb.AppendLine();
         sb.AppendLine("REPLY TO ANALYZE:");
         sb.Append(prose?.Trim() ?? string.Empty);

         return sb.ToString();
      }

      #region private

      /// <summary>
      ///   Renders every <see cref="GameActionCatalog" /> entry NOT already hand-taught above as a compact
      ///   reference block: one line per action, its description, and its parameters. Built once from the static
      ///   catalog (no per-turn data), so it stays part of the cacheable stable prefix. A LOCAL set, not a static
      ///   field: this method runs from <see cref="_stablePrefix" />'s own field initializer, which runs before any
      ///   static field declared later in the file would be assigned, so a static exclusion set here would still
      ///   read null at that point.
      /// </summary>
      private static void AppendOtherActions(StringBuilder sb)
      {
         sb.AppendLine("OTHER ACTIONS YOU MAY EMIT: ONLY when the prose UNAMBIGUOUSLY shows this exact concrete deed");
         sb.AppendLine("happening in the reply. When in doubt, do not emit. Format each as an [ACTION] block with a");
         sb.AppendLine("'type:' line and the listed parameters, exactly like the actions above:");
         sb.AppendLine();

         // The five reactive signals already taught above by hand, with carefully-tuned wording that must never be
         // diluted: excluded here so they are never taught twice. end_conversation is a ChatViewModel chat-flow
         // control (like witness_leaves/request_privacy), not a GameActionCatalog entry, so it never appears in
         // GameActionCatalog.Types in the first place; it is listed here only so the exclusion reads as the same
         // five signals by name.
         var coreTaughtTypes = new HashSet<string> {
            "change_relation", "end_conversation", "give_gold", "take_gold"
         };

         foreach (GameActionSpec spec in GameActionCatalog.All)
         {
            if (coreTaughtTypes.Contains(spec.Type)) continue;

            var line = new StringBuilder();
            line.Append("- ").Append(spec.Type).Append(": ").Append(spec.Description);

            if (spec.Parameters.Count > 0)
            {
               line.Append(" (params: ");
               for (var i = 0; i < spec.Parameters.Count; i++)
               {
                  if (i > 0) line.Append("; ");
                  line.Append(spec.Parameters[i].Name).Append('=').Append(spec.Parameters[i].Meaning);
               }
               line.Append(')');
            }

            sb.AppendLine(line.ToString());
         }

         sb.AppendLine();
      }

      private static string BuildStablePrefix()
      {
         var sb = new StringBuilder();

         sb.AppendLine("You are a structured-signal EXTRACTOR for a roleplay game. A first model has ALREADY written");
         sb.AppendLine("the NPC's reply. Your only job is to read that reply and emit the machine-readable tags it");
         sb.AppendLine("implies, so the game can record what happened. You never speak as the NPC.");
         sb.AppendLine();
         sb.AppendLine("Emit ONLY the blocks below, each label on its own line, opened as [LABEL] and closed as [/LABEL]:");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: change_relation");
         sb.AppendLine("delta: <integer>");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("change_relation records how the NPC's regard toward the player shifts in THIS reply, inferred");
         sb.AppendLine("from the emotional tenor of the prose: negative for anger, insult, or coldness; positive for");
         sb.AppendLine("warmth, gratitude, or affection. Use roughly -15 (a grave affront) to +10 (deep warmth). A");
         sb.AppendLine("value of 0 means no change, so emit nothing at all.");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: end_conversation");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("Emit end_conversation whenever the prose brings THIS exchange to a close, warm or cold: storming");
         sb.AppendLine("off, dismissing the player, turning away to your own business and leaving them, OR an amicable");
         sb.AppendLine("parting (a farewell, a goodnight, sending them off to rest, saying you will speak again later). It");
         sb.AppendLine("ends the current conversation, not the relationship, so a warm \"we will talk in the morning\" as the");
         sb.AppendLine("NPC leaves the player STILL closes it. If the reply plainly reads as a parting, emit it.");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: give_gold");
         sb.AppendLine("amount: <whole number of denars>");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("Emit give_gold when the prose shows the NPC HANDING money, coin, or a purse TO the player (a gift,");
         sb.AppendLine("reward, payment, or bribe). If the prose names an amount, use it; if it is vague (a purse, a few");
         sb.AppendLine("coins), estimate a modest, plausible sum that fits the description. This is ONLY the NPC giving to");
         sb.AppendLine("the player.");
         sb.AppendLine();
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: take_gold");
         sb.AppendLine("amount: <whole number of denars>");
         sb.AppendLine("[/ACTION]");
         sb.AppendLine("Emit take_gold when the prose shows the PLAYER paying, handing over, or surrendering money TO the");
         sb.AppendLine("NPC (a fee, tribute, ransom, or debt). Direction is what matters: money moving from the NPC to the");
         sb.AppendLine("player is give_gold; money moving from the player to the NPC is take_gold.");
         sb.AppendLine();
         sb.AppendLine("[EVENT]");
         sb.AppendLine("type: first_meeting|farewell|conflict|collaboration|agreement|flirt|intimacy|betrayal|confrontation|other");
         sb.AppendLine("summary: One sentence, FIRST PERSON and PAST TENSE. Use \"I\" for yourself (never your own name),");
         sb.AppendLine("and ONLY for what YOU personally did: if the reply shows someone ELSE performing the act (a soldier,");
         sb.AppendLine("a bandmate, anyone at your command) while you order, watch, or hold the prisoner, the deed is");
         sb.AppendLine("THEIRS, so write \"I had my man do X\" or \"my men did X while I watched\", never \"I did X\" for an act");
         sb.AppendLine("you did not perform yourself.");
         sb.AppendLine("Refer to the PLAYER by the name given in the facts below, never by an invented epithet");
         sb.AppendLine("(not \"the coward\", \"the stranger\") and never by a bare \"he\"/\"she\". Record what happened this");
         sb.AppendLine("reply and why it mattered; do NOT state how long ago it was.");
         sb.AppendLine("[/EVENT]");
         sb.AppendLine("These [EVENT] type words (first_meeting, farewell, conflict, collaboration, agreement, flirt,");
         sb.AppendLine("intimacy, betrayal, confrontation, other) label the [EVENT] block ONLY. NEVER emit one as an");
         sb.AppendLine("[ACTION]: an [ACTION] type is ALWAYS one of the action verbs (change_relation, end_conversation,");
         sb.AppendLine("give_gold, take_gold, or one listed under OTHER ACTIONS below). A moment that is intimate or a");
         sb.AppendLine("flirt is recorded as an [EVENT], never emitted as an action.");
         sb.AppendLine("first_meeting is ONLY for a genuine first-ever encounter: the two of you have never spoken");
         sb.AppendLine("before. If the facts below show any regard other than +0 toward the player, or name an");
         sb.AppendLine("existing bond (a spouse, consort, lover, or kin), you already know each other, so do NOT use");
         sb.AppendLine("first_meeting. Likewise, if this reply is a later turn of an exchange already under way rather");
         sb.AppendLine("than its opening line, do NOT use first_meeting either: when the facts below state the");
         sb.AppendLine("conversation is already under way, that IS this case, take it as decisive over anything else in");
         sb.AppendLine("the prose. Pick whichever type actually fits what happened this reply instead (collaboration,");
         sb.AppendLine("agreement, flirt, intimacy, confrontation, farewell, or other).");
         sb.AppendLine();
         AppendOtherActions(sb);
         sb.AppendLine("GROUND IN THE FACTS, DO NOT INVENT: the facts below give the setting (place, who is present, the");
         sb.AppendLine("player's standing). Use them to ANCHOR a memory (where it happened, who witnessed it) when it fits");
         sb.AppendLine("what occurred, but record ONLY what actually happened in the reply; never invent events, people, or");
         sb.AppendLine("details the reply does not contain (do not add the player's army or companions unless the reply does).");
         sb.AppendLine();
         sb.AppendLine("HARD RULE: You are given a roleplay reply that is ALREADY WRITTEN. Do NOT rewrite, continue, or");
         sb.AppendLine("comment on it. Read it and output ONLY the [ACTION] and [EVENT] blocks it implies, in the exact");
         sb.AppendLine("format above, and nothing else. If nothing is warranted, output nothing.");

         return sb.ToString();
      }

      #endregion
   }
}

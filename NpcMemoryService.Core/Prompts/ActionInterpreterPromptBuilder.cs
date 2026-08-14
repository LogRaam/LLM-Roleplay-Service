// Code written by Gabriel Mailhot, 13/08/2026.

#region

using System.Text;

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
         sb.AppendLine("Emit end_conversation ONLY when the prose shows the NPC ending or breaking off the exchange");
         sb.AppendLine("(turning away, dismissing the player, storming off, or a final farewell).");
         sb.AppendLine();
         sb.AppendLine("[EVENT]");
         sb.AppendLine("type: first_meeting|farewell|conflict|collaboration|agreement|flirt|intimacy|betrayal|confrontation|other");
         sb.AppendLine("summary: One sentence, FIRST PERSON and PAST TENSE. Use \"I\" for yourself (never your own");
         sb.AppendLine("name). Refer to the PLAYER by the name given in the facts below, never by an invented epithet");
         sb.AppendLine("(not \"the coward\", \"the stranger\") and never by a bare \"he\"/\"she\". Record what happened this");
         sb.AppendLine("reply and why it mattered; do NOT state how long ago it was.");
         sb.AppendLine("[/EVENT]");
         sb.AppendLine();
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

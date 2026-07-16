// Code written by Gabriel Mailhot, 15/07/2026.
// Some reasoning models (nvidia/nemotron, some DeepSeek/Qwen templates) emit their chain-of-thought INLINE in
// the message content, wrapped in <think>...</think> tags, instead of the separate "reasoning" field the parser
// already handles. Left in place, that trace leaks straight into the game: Bannerlord's text renderer treats
// <...> as markup and hides the tags themselves, so the player sees the bare reasoning prose ("The user wants
// me to write a memory line...") presented as the NPC's own words or, worse, stored as a memory (reported
// in-game against the conversation summarizer). This stripper removes the trace at the single point every
// consumer shares (response parsing), so chat replies, memory lines and captive scenes are all covered at once.

#region

using System.Text.RegularExpressions;

#endregion

namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
   /// <summary>
   ///   Removes inline chain-of-thought traces (<c>&lt;think&gt;...&lt;/think&gt;</c> and the common
   ///   <c>thinking</c>/<c>reasoning</c> variants) from a model reply, keeping only the prose meant for the
   ///   player. A trace whose closing tag never arrives (the model spent its whole budget thinking and was cut
   ///   mid-trace) strips to EMPTY on purpose: an empty reply feeds the caller's existing bigger-budget retry,
   ///   exactly as when the trace arrives in the separate reasoning field.
   /// </summary>
   public static class ReasoningTraceStripper
   {
      // The three tag names reasoning templates actually use. Matched as literal tags only: lower-case prose
      // brackets ("[unreadable]") and ordinary angle-bracket text without these exact names pass untouched.
      private const string TagNames = "think|thinking|reasoning|thought";

      /// <summary>A complete trace: opening tag through its matching closing tag, shortest match.</summary>
      private static readonly Regex CompleteBlock = new(
         $@"<({TagNames})\s*>.*?</\1\s*>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

      /// <summary>
      ///   A stray closing tag: some templates eat the opening tag, so everything up to the LAST closing tag
      ///   (greedy) is trace, and the reply follows it.
      /// </summary>
      private static readonly Regex StrayClosing = new(
         $@"^.*</({TagNames})\s*>",
         RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

      /// <summary>
      ///   An opening tag never closed: the trace was cut off by the token limit, so everything from the tag to
      ///   the end is trace.
      /// </summary>
      private static readonly Regex UnclosedOpening = new(
         $@"<({TagNames})\s*>.*$",
         RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

      /// <summary>
      ///   Returns <paramref name="content" /> with any inline reasoning trace removed and the remainder
      ///   trimmed. Content carrying no trace tags is returned VERBATIM (not even trimmed), so the ordinary
      ///   reply path is byte-for-byte unchanged.
      /// </summary>
      public static string Strip(string? content)
      {
         if (string.IsNullOrEmpty(content)) return content ?? string.Empty;

         string result = CompleteBlock.Replace(content, string.Empty);
         result = StrayClosing.Replace(result, string.Empty);
         result = UnclosedOpening.Replace(result, string.Empty);

         return ReferenceEquals(result, content) || result == content
            ? content
            : result.Trim();
      }
   }
}

// Code written by Gabriel Mailhot, 18/07/2026.
// Companion to ReasoningTraceStripper: the stripper removes TAGGED chain-of-thought (<think>...</think>),
// but a model can also think out loud with NO tag at all — the reply simply OPENS with its analysis of the
// task ("Looking at the context:", "The user wants me to write a memory line", "Let me analyze what
// happened: 1. ..."). That un-tagged meta-reasoning sails through the stripper untouched, then leaks into
// the game as spoken dialogue or, worse, gets STORED as the NPC's memory. This guard DETECTS that opening
// shape so every consumer can refuse it. Detection only, never rewriting: a reply that opens by narrating
// the task instead of doing it is unusable as content, and trimming around it would be guessing.

#region

using System.Text.RegularExpressions;

#endregion

namespace NpcMemoryService.Core.Parsing
{
   /// <summary>
   ///   Detects un-tagged meta-reasoning: a text that OPENS with the model analyzing its task or the
   ///   situation ("Looking at the context", "The user wants", "The player might be") instead of
   ///   producing the content it was asked for. Consumers use it to REJECT the text outright (drop the
   ///   memory line, silence the fallback dialogue) — never to rewrite it. Deliberately anchored at the
   ///   very start of the text and matched on a few explicit openers, so legitimate in-character prose
   ///   ("Let me think... yes, I remember") never trips it.
   /// </summary>
   public static class MetaReasoningGuard
   {
      private static readonly Regex MetaOpeners = new(
         @"^\s*(Looking at the context|Let me (analyze|summarize|review|break down|consider what)|The user (wants|asked|is asking)|The player (might be|is trying|seems to)|Wait, the player)",
         RegexOptions.IgnoreCase | RegexOptions.Compiled);

      /// <summary>True when the text OPENS with un-tagged meta-reasoning (analysis of the task/situation) rather than content.</summary>
      public static bool IsMetaReasoning(string? text)
         => !string.IsNullOrWhiteSpace(text) && MetaOpeners.IsMatch(text!);
   }
}

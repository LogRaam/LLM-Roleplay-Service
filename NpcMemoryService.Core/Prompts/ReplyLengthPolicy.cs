// Code written by Gabriel Mailhot, 29/10/2026.
// How long a character's replies run, as the player chooses. RaveN017Mod (Nexus PM, 29/10/2026) found the replies too
// long, "like a novel", next to the short replies he was used to, and asked for a minimum and maximum word count.
// Counted in sentences rather than words: a word cap cuts a reply off mid-sentence, and a model counts sentences far
// better than words. Full is the length the mod has always taught, so nothing changes for a player who never touches it.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>How long a character's spoken replies run.</summary>
   public enum ReplyLength
   {
      /// <summary>The shipped length: 3 to 7 sentences, longer for a story or strong feeling.</summary>
      Full,

      /// <summary>2 to 4 sentences, up to 6 for a story or strong feeling.</summary>
      Balanced,

      /// <summary>1 to 3 sentences, always.</summary>
      Short
   }

   /// <summary>The length lines DIALOGUE STYLE teaches for each <see cref="ReplyLength" />. Pure.</summary>
   public static class ReplyLengthPolicy
   {
      /// <summary>The lines that say how long a reply runs, in the order they are written.</summary>
      public static IReadOnlyList<string> Lines(ReplyLength length) => length switch {
         ReplyLength.Short => new[] {
            "- 1 to 3 sentences, always: the player chose short replies. Say the one thing that matters.",
            "- Even a story, a memory or a strong feeling stays within three sentences.",
            "- One sentence is often enough for a confirmation or a quick question.",
            "- A stage direction counts toward that length."
         },
         ReplyLength.Balanced => new[] {
            "- 2 to 4 sentences for normal exchanges: the player chose replies of middling length.",
            "- Up to 6 only when telling a story, recalling the past or expressing strong emotion.",
            "- One sentence for trivial confirmations or quick questions.",
            "- A stage direction counts toward that length."
         },
         _ => new[] {
            "- 3-7 sentences for normal exchanges.",
            "- Longer when telling stories, recalling past events, explaining politics,",
            "  or expressing strong emotion. Take your time when the moment warrants it.",
            "- Brief one or two sentences only for trivial confirmations or quick questions."
         }
      };

      /// <summary>
      ///   True when a narrative style may lengthen replies beyond the lines above. Only at Full: a length the player
      ///   set on purpose holds, whatever the style's own rhythm.
      /// </summary>
      public static bool StyleMayLengthen(ReplyLength length) => length == ReplyLength.Full;
   }
}

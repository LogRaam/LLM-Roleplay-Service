// Code written by Gabriel Mailhot, 08/10/2026.
// A stage direction written twice in one reply is shown once (tashmetu, Nexus, 08/10/2026: "*A dry, bitter laugh escapes
// me.*" before the quotes and again inside them). Only an identical gesture (case and spacing aside) is dropped.

#region

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#endregion

namespace NpcMemoryService.Core.Parsing
{
   /// <summary>Drops the second and later copies of the same *stage direction* in a reply. Pure.</summary>
   public static class StageDirectionDedupe
   {
      private static readonly Regex Gesture = new(@"\*([^*\r\n]{1,300})\*", RegexOptions.Compiled);
      private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

      public static string Apply(string reply)
      {
         if (string.IsNullOrEmpty(reply) || reply.IndexOf('*') < 0) return reply;

         var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
         string cleaned = Gesture.Replace(reply, m =>
         {
            string key = Spaces.Replace(m.Groups[1].Value, " ").Trim();

            return seen.Add(key) ? m.Value : "\u0000";
         });
         if (cleaned.IndexOf('\u0000') < 0) return reply;

         // Take the dropped copy out with the space that followed it (inside quotes) or preceded it (at the end).
         cleaned = Regex.Replace(cleaned, "\u0000[ \t]*", "");
         cleaned = Regex.Replace(cleaned, "[ \t]+(?=[\r\n]|$)", "");
         cleaned = Regex.Replace(cleaned, "[ \t]{2,}", " ");

         return cleaned;
      }
   }
}

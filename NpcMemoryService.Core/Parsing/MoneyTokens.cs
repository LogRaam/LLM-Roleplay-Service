// Code written by Gabriel Mailhot, 01/11/2026.
// RaveN017Mod (Nexus, 01/11/2026): Rhagaea offered 20,000 denars, then sent 1,000. Gabriel, same day: "est-ce que le LLM peut
// proposer avec un placeholder que nous remplissons ensuite en code?", and on the memory: "le code fixe le montant qui est
// reinjecte ensuite dans la memoire du NPC". The model sees each figure, so it can react to it, and writes its token; the
// host fills the token once in the raw reply, so dialogue, narration, [EVENT] and action parameters carry the one sum.

#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

#endregion

namespace NpcMemoryService.Core.Parsing
{
   /// <summary>Fills the sums a model wrote as tokens. Pure.</summary>
   public static class MoneyTokens
   {
      /// <summary>The token for what the character would give with nothing in return.</summary>
      public const string Gift = "gift";

      /// <summary>What a token nobody holds reads as: a figure could only be invented, and braces must never reach the screen.</summary>
      public const string NoFigure = "a fair sum";

      private static readonly Regex Token = new(@"\{\s*(gift|low|fair|most)\s*(?::\s*([^{}\r\n]*?)\s*)?\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

      /// <summary>The table's key for this kind of sum on this subject: "fair:Monchug", or "gift" with no subject.</summary>
      public static string Key(string kind, string? subject)
         => string.IsNullOrWhiteSpace(subject) ? kind : kind + ":" + subject!.Trim();

      /// <summary>
      ///   Writes each token's figure, in plain digits because an action's amount must parse, or <see cref="NoFigure" /> when
      ///   the table has no such sum. A null table fills nothing and only clears what would otherwise reach the screen.
      /// </summary>
      public static string Fill(string? text, IReadOnlyDictionary<string, int>? table)
      {
         if (string.IsNullOrEmpty(text) || text!.IndexOf('{') < 0) return text ?? string.Empty;

         return Token.Replace(text, m => {
            string kind = m.Groups[1].Value.ToLowerInvariant();
            string key = Key(kind, m.Groups[2].Value);
            if (table == null) return NoFigure;
            if (table.TryGetValue(key, out int sum) || TryLooseSubject(table, kind, m.Groups[2].Value, out sum))
               return sum.ToString(CultureInfo.InvariantCulture);

            return NoFigure;
         });
      }

      /// <summary>
      ///   The sum for a subject written its own way: "{most:Monchug of the Arkits}" for the captive taught as "Monchug"
      ///   (RaveN017Mod, Nexus, 05/11/2026: sales "still don't work every single time"; a token the table missed read "a fair
      ///   sum", which no price can parse, so the sale paid the floor). The one subject of this kind whose whole name stands
      ///   as words in what was written, or that contains what was written; two candidates are no answer.
      /// </summary>
      private static bool TryLooseSubject(IReadOnlyDictionary<string, int> table, string kind, string written, out int sum)
      {
         sum = 0;
         string query = written?.Trim() ?? string.Empty;
         if (query.Length == 0) return false;

         string prefix = kind + ":";
         int found = 0;
         foreach (KeyValuePair<string, int> entry in table)
         {
            if (!entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            string subject = entry.Key.Substring(prefix.Length).Trim();
            if (subject.Length == 0 || !(subject.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || StandsAsWords(subject, query))) continue;

            if (++found > 1) return false;
            sum = entry.Value;
         }

         return found == 1;
      }

      /// <summary>True when <paramref name="name" /> stands as whole words inside <paramref name="text" />, never inside a longer word.</summary>
      private static bool StandsAsWords(string name, string text)
      {
         for (int at = text.IndexOf(name, StringComparison.OrdinalIgnoreCase); at >= 0; at = text.IndexOf(name, at + 1, StringComparison.OrdinalIgnoreCase))
         {
            int end = at + name.Length;
            if ((at == 0 || !char.IsLetter(text[at - 1])) && (end == text.Length || !char.IsLetter(text[end]))) return true;
         }

         return false;
      }
   }
}

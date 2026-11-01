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
            string key = Key(m.Groups[1].Value.ToLowerInvariant(), m.Groups[2].Value);

            return table != null && table.TryGetValue(key, out int sum) ? sum.ToString(CultureInfo.InvariantCulture) : NoFigure;
         });
      }
   }
}

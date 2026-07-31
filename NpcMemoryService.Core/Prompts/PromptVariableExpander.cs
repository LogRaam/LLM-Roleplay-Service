// Code written by Gabriel Mailhot, 30/07/2026.
// Expands SillyTavern-style {{token}} template variables (e.g. {{char}}, {{user}}) inside player-authored
// prompt text (narrative styles, world.txt, player_description.txt, post_history_instructions.txt). Kept as
// a pure, static, unit-tested function so the substitution logic can be pinned in isolation from the rest of
// PromptBuilder. MUST be called at prompt-BUILD time (inside PromptBuilder, where the current NPC and live
// player values are known), never at file-READ/cache time: a style or override file is loaded once and
// reused for every NPC in the session, so resolving {{char}} when the file is first read would freeze the
// first NPC's name into a file every later NPC's prompt then repeats verbatim.

#region

using System.Collections.Generic;
using System.Text.RegularExpressions;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   Expands <c>{{token}}</c> placeholders in player-authored prompt text against a lookup of known
   ///   variables (NPC name, player name, and the like), matching the "macro" convention SillyTavern users
   ///   already know from <c>{{char}}</c>/<c>{{user}}</c>.
   /// </summary>
   public static class PromptVariableExpander
   {
      // Compiled once: this runs on every prompt build (every player turn), so a compiled instance avoids
      // re-parsing the pattern per call. Inner whitespace is optional ({{ user }} == {{user}}) and token
      // characters are restricted to [A-Za-z0-9_] so the regex can never straddle unrelated braces.
      private static readonly Regex TokenPattern =
         new Regex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

      /// <summary>
      ///   Replaces every <c>{{token}}</c> occurrence in <paramref name="text" /> with the matching entry of
      ///   <paramref name="variables" />, case-insensitively regardless of the comparer the caller built the
      ///   dictionary with (this method copies it into its own <see cref="System.StringComparer.OrdinalIgnoreCase" />
      ///   lookup first, so the case-insensitive guarantee lives here and never depends on the caller
      ///   remembering to pick the right comparer).
      ///   <para>
      ///     A KNOWN token whose value is null or empty resolves to an empty string (e.g. no spouse set).
      ///     An UNKNOWN token is left exactly as written, e.g. <c>{{unknown}}</c>, matching SillyTavern's own
      ///     behaviour of never silently eating a macro it does not recognise, so a player's typo or a future
      ///     variable stays visible instead of vanishing.
      ///   </para>
      ///   Never throws: null/empty <paramref name="text" /> or a null/empty <paramref name="variables" />
      ///   simply return <paramref name="text" /> unchanged.
      /// </summary>
      public static string Expand(string text, IReadOnlyDictionary<string, string> variables)
      {
         if (string.IsNullOrEmpty(text) || variables == null || variables.Count == 0) return text;

         var lookup = new Dictionary<string, string>(variables.Count, System.StringComparer.OrdinalIgnoreCase);
         foreach (KeyValuePair<string, string> entry in variables) lookup[entry.Key] = entry.Value;

         return TokenPattern.Replace(text, match =>
         {
            string token = match.Groups[1].Value;
            return lookup.TryGetValue(token, out string value) ? value ?? "" : match.Value;
         });
      }
   }
}

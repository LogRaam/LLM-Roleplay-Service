// Code written by Gabriel Mailhot, 20/09/2026.
// A memory the model writes is read back, turn after turn, for the rest of the campaign, so an ambiguous subject
// in one does not go away: it is re-read every time.
//
// reminensce (Nexus, 19/09/2026) watched Chief Rolan claim the PLAYER's defeat at Vladiv as his own. The prompt
// had a pronoun collision at the root of it (see PromptBuilder.AppendHistory), and the stored memory lines share
// the blame: "you" inside a memory means the player, while "you" everywhere else in the prompt means the
// character. The mod's own memory templates now name the player outright. This does the same, as far as it
// safely can, for the summaries the MODEL writes: "the player" becomes their actual name.
//
// Deliberately narrow, in the spirit of DiscoverySubjectGuard: one explicit, mechanical substitution and nothing
// else. No attempt to rewrite pronouns, because "you" in a model-written memory is usually correct English from
// the character's own mouth, and a clumsy rewrite would corrupt a memory worse than leaving it alone. The prompt
// asks for the name; this catches the commonest miss.

#region

using System;
using System.Text.RegularExpressions;

#endregion

namespace NpcMemoryService.Core.Parsing
{
   /// <summary>Makes a stored <c>[EVENT]</c> summary name the player rather than call them "the player".</summary>
   public static class EventSubjectNormalizer
   {
      private static readonly Regex ThePlayer = new(@"\bthe player\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

      /// <summary>
      ///   Returns <paramref name="summary" /> with the words "the player" replaced by
      ///   <paramref name="playerName" />, preserving the rest exactly. Null or blank input comes back unchanged,
      ///   and so does everything else when no name is supplied: a memory is never worth mangling to enforce a
      ///   convention.
      /// </summary>
      public static string? Normalize(string? summary, string? playerName)
      {
         if (string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(playerName)) return summary;

         string name = playerName!.Trim();

         // A name that already IS the word "player" (a test double, or a player who named themselves that) would
         // turn the substitution into a no-op with extra steps.
         if (name.Equals("the player", StringComparison.OrdinalIgnoreCase) || name.Equals("player", StringComparison.OrdinalIgnoreCase))
            return summary;

         return ThePlayer.Replace(summary!, name);
      }
   }
}

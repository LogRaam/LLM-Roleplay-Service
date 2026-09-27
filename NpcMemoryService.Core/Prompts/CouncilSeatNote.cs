// Code written by Gabriel Mailhot, 27/09/2026.
// The council's door for another mod's text, one seat at a time (tashmetu, Bellum Civile bridge, 26/09/2026: his
// bc_temper variable worked in a private word, and he left the council alone "until there's a hook there", because
// the council prompt carried no modder text at all).
//
// The host loads a council_seat.txt template (Custom-first, cr_patch contributions merged, exactly like
// behavior_guidelines.txt) and, for each seated member, resolves that member's variables on the game thread and hands
// them here. What comes out rides on CouncilMemberInput.ModderNote, rendered under that member's own roster line.

#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>Composes one council seat's modder note from the council_seat template and that seat's variables.</summary>
   public static class CouncilSeatNote
   {
      /// <summary>
      ///   The most one seat may carry. A table seats up to six, so this is up to six times what it costs in one
      ///   private word, in a prompt the Compact budget already fights for.
      /// </summary>
      public const int MaxChars = 700;

      /// <summary>
      ///   The template expanded with THIS seat's variables, headings dropped (they structure the big prompt, not one
      ///   seat's line), blank lines removed, and cut at a sentence within <see cref="MaxChars" />. Null when there is
      ///   no template or nothing is left, so a seat whose variables all came back empty carries no note at all.
      /// </summary>
      public static string? Compose(string? template, IReadOnlyDictionary<string, string> seatVariables)
      {
         if (string.IsNullOrWhiteSpace(template)) return null;

         string expanded = PromptVariableExpander.Expand(template!, seatVariables ?? new Dictionary<string, string>());

         string[] lines = expanded.Replace("\r\n", "\n").Split('\n')
                                  .Select(l => l.Trim())
                                  .Where(l => l.Length > 0 && !l.StartsWith("===", StringComparison.Ordinal))
                                  .ToArray();
         if (lines.Length == 0) return null;

         return Cap(string.Join("\n", lines));
      }

      private static string Cap(string note)
      {
         if (note.Length <= MaxChars) return note;

         string head = note.Substring(0, MaxChars);
         int sentence = new[] {head.LastIndexOf(". ", StringComparison.Ordinal), head.LastIndexOf(".\n", StringComparison.Ordinal)}.Max();
         if (sentence > 0) return head.Substring(0, sentence + 1);

         int word = head.LastIndexOf(' ');

         return (word > 0 ? head.Substring(0, word) : head).TrimEnd();
      }
   }
}

// Code written by Gabriel Mailhot, 12/10/2026.
// The Prompt Composer, increment 3 (docs/design/prompt-composer/DESIGN.md in the mod repository; Gabriel, 12/10/2026:
// "go"). A prompt profile may reword a section (sections/<key>.txt): the file REPLACES the stock text, "!append" adds to
// it, "!remove <text>" drops the stock lines holding that text. The host reads the files once and hands the texts in the
// EncounterContext; this decides how a text meets the stock section. The section's own gate comes first, always: a
// section that would write nothing writes nothing, so a rewording never brings back what a setting has shut (adult
// content off stays off). Sealed sections are never reworded; a required one may be, but never emptied.

#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>A profile's rewording of one section (a sections/&lt;key&gt;.txt).</summary>
   public sealed class PromptSectionText
   {
      /// <summary>The words, {{variables}} allowed; null when the file only removes lines.</summary>
      public string? Text { get; init; }

      /// <summary>"!append": the text follows the stock one instead of replacing it.</summary>
      public bool Append { get; init; }

      /// <summary>"!remove &lt;text&gt;": stock lines holding any of these are dropped.</summary>
      public IReadOnlyList<string> RemoveLines { get; init; } = Array.Empty<string>();

      /// <summary>Where it came from, for the log and cr.prompt_check.</summary>
      public string? Source { get; init; }
   }

   /// <summary>Applies a section's rewording to the stock text the section rendered. Pure.</summary>
   public static class PromptSectionTextApplier
   {
      /// <summary>
      ///   The section as written: <paramref name="stock" /> when there is no rewording, when the gate is shut (stock is
      ///   empty), when the section is sealed, or when a required section would be emptied; otherwise the rewording, its
      ///   variables expanded, ending the way the stock section ended so the sections around it keep their spacing.
      /// </summary>
      public static string Apply(string key, string stock, PromptSectionText? text, PromptSectionLock @lock,
                                 IReadOnlyDictionary<string, string>? vars, List<string>? problems = null)
      {
         if (text == null || string.IsNullOrEmpty(stock)) return stock;
         if (@lock == PromptSectionLock.Sealed)
         {
            problems?.Add($"'{key}' is sealed: its stock text stays.");
            return stock;
         }

         string body = stock.TrimEnd('\r', '\n');
         string ending = stock.Substring(body.Length);
         if (ending.Length == 0) ending = "\n";

         string result = body;
         if (text.RemoveLines.Count > 0)
            result = string.Join("\n", result.Split('\n').Where(line => !text.RemoveLines.Any(r => !string.IsNullOrEmpty(r) && line.Contains(r))));

         string words = text.Text == null ? null : PromptVariableExpander.Expand(text.Text, vars ?? new Dictionary<string, string>()).Trim('\r', '\n');
         if (words != null) result = text.Append ? (string.IsNullOrWhiteSpace(result) ? words : result + "\n" + words) : words;

         if (string.IsNullOrWhiteSpace(result))
         {
            if (@lock == PromptSectionLock.Required)
            {
               problems?.Add($"'{key}' is required and would be left empty: its stock text stays.");
               return stock;
            }

            return "";
         }

         return result + ending;
      }
   }
}

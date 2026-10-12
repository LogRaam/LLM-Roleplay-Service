// Code written by Gabriel Mailhot, 12/10/2026.
// The Prompt Composer, increment 2 (docs/design/prompt-composer/DESIGN.md in the mod repository). A LAYOUT is a power
// user's or a mod's order for the character prompt's keyed sections (PromptBuilder.CharacterSections): what goes above
// the cache marker, what below, and what is off. The host chooses the layout (by profile and kind of character) and
// hands it in the EncounterContext; this file decides what a layout may and may not do. Gabriel (11/10/2026): the most
// freedom, locks only where the game would break or the player's safety is at stake, a broken cache warned about and
// never forbidden.

#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>One order for the character prompt's sections (a layouts/&lt;kind&gt;.json of a prompt profile).</summary>
   public sealed class PromptLayout
   {
      /// <summary>The sections above the cache marker, in order.</summary>
      public IReadOnlyList<string> Prefix { get; init; } = Array.Empty<string>();

      /// <summary>The sections below the cache marker, in order.</summary>
      public IReadOnlyList<string> Tail { get; init; } = Array.Empty<string>();

      /// <summary>The sections turned off.</summary>
      public IReadOnlyCollection<string> Off { get; init; } = Array.Empty<string>();

      /// <summary>Where it came from ("profile 'x', layout 'lord'"), for the log and cr.prompt_check.</summary>
      public string? Source { get; init; }
   }

   /// <summary>Turns a layout into the sections written, in order, keeping the locks. Pure.</summary>
   public static class PromptLayoutResolver
   {
      /// <summary>
      ///   The sections to write, in order. No layout: the stock order. Otherwise the layout's prefix, the encounter
      ///   context (the cache marker, always between), then its tail; with these rules, each reported in
      ///   <paramref name="problems" /> when given:
      ///   an unknown key is ignored; a section the layout does not list keeps its stock place (only "off" turns one
      ///   off); a sealed section is never off and never moved; a required one is never off and stays in its own zone; a
      ///   free section may go to either zone, with a warning about the cache.
      /// </summary>
      public static IReadOnlyList<PromptSectionSpec> Order(IReadOnlyList<PromptSectionSpec> stock, PromptLayout? layout, List<string>? problems = null)
      {
         if (layout == null) return stock;

         var byKey = stock.ToDictionary(s => s.Key, StringComparer.Ordinal);
         PromptSectionSpec? marker = stock.FirstOrDefault(s => s.Zone == PromptZone.Marker);
         var off = new HashSet<string>(StringComparer.Ordinal);
         foreach (string key in layout.Off ?? Array.Empty<string>())
         {
            if (!byKey.TryGetValue(key ?? "", out PromptSectionSpec? spec))
            {
               problems?.Add($"'{key}' is turned off but is an unknown section: ignored.");
               continue;
            }

            if (spec.Lock != PromptSectionLock.None || spec.Zone == PromptZone.Marker)
               problems?.Add($"'{key}' cannot be turned off ({LockWord(spec)}): it stays.");
            else off.Add(key!);
         }

         var placed = new HashSet<string>(StringComparer.Ordinal);
         List<PromptSectionSpec> prefix = Take(layout.Prefix, PromptZone.Prefix, byKey, off, placed, problems);
         List<PromptSectionSpec> tail = Take(layout.Tail, PromptZone.Tail, byKey, off, placed, problems);

         // Every section the layout neither placed nor turned off keeps its stock place (sealed and misplaced required
         // ones were left unplaced by Take, so they come back here too).
         for (var i = 0; i < stock.Count; i++)
         {
            PromptSectionSpec s = stock[i];
            if (s.Zone == PromptZone.Marker || placed.Contains(s.Key) || off.Contains(s.Key)) continue;

            List<PromptSectionSpec> zone = s.Zone == PromptZone.Prefix ? prefix : tail;
            zone.Insert(StockSlot(stock, i, zone), s);
            if (s.Lock != PromptSectionLock.Sealed && !layout.Prefix.Contains(s.Key) && !layout.Tail.Contains(s.Key))
               problems?.Add($"'{s.Key}' is not in the layout: kept at its stock place (list it under \"off\" to turn it off).");
         }

         var order = new List<PromptSectionSpec>(prefix.Count + tail.Count + 1);
         order.AddRange(prefix);
         if (marker != null) order.Add(marker);
         order.AddRange(tail);

         return order;
      }

      private static List<PromptSectionSpec> Take(IReadOnlyList<string>? keys, PromptZone zone, Dictionary<string, PromptSectionSpec> byKey,
                                                  HashSet<string> off, HashSet<string> placed, List<string>? problems)
      {
         var taken = new List<PromptSectionSpec>();
         foreach (string key in keys ?? Array.Empty<string>())
         {
            if (!byKey.TryGetValue(key ?? "", out PromptSectionSpec? spec))
            {
               problems?.Add($"'{key}' is an unknown section: ignored.");
               continue;
            }

            if (spec.Zone == PromptZone.Marker || off.Contains(key!)) continue;
            if (!placed.Add(key!))
            {
               problems?.Add($"'{key}' is listed twice: the first place is kept.");
               continue;
            }

            if (spec.Lock == PromptSectionLock.Sealed)
            {
               placed.Remove(key!); // put back at its stock place by Order
               continue;
            }

            if (spec.Zone != zone)
            {
               if (spec.Lock == PromptSectionLock.Required)
               {
                  placed.Remove(key!);
                  problems?.Add($"'{key}' is required and may move only within its own zone: kept at its stock place.");
                  continue;
               }

               problems?.Add(zone == PromptZone.Prefix
                  ? $"'{key}' changes every turn and sits above the marker: every reply re-sends the whole prompt (no cache)."
                  : $"'{key}' is stable and sits below the marker: it is re-sent uncached every turn.");
            }

            taken.Add(spec);
         }

         return taken;
      }

      // Where a section goes back in its zone: after the nearest section before it in the stock order that the zone holds.
      private static int StockSlot(IReadOnlyList<PromptSectionSpec> stock, int stockIndex, List<PromptSectionSpec> zone)
      {
         for (int j = stockIndex - 1; j >= 0; j--)
         {
            int at = zone.IndexOf(stock[j]);
            if (at >= 0) return at + 1;
         }

         return 0;
      }

      private static string LockWord(PromptSectionSpec s)
         => s.Zone == PromptZone.Marker ? "the cache marker" : s.Lock == PromptSectionLock.Sealed ? "sealed" : "required";
   }
}

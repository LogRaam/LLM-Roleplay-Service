// Code written by Gabriel Mailhot, 01/11/2026.
// kurushimi00 (Nexus, 01/11/2026): "The same character, in every meeting almost, will marvel that I 'cleared the road of
// bandits'". Nothing told a character what it had already said to the player. Gabriel, same day: "Lorsque un NPC relate un
// souvenir, il devrait l'indiquer avec un TAG. Alors nous pourrions modifier le souvenir en y ajoutant a la fin (j'ai deja
// discute de ce sujet avec {player}). Pour bien suivre la memoire, chaque enregistrement pourrait avoir un shortUID."
//
// The mark is a field and not text in the summary: a summary reaches witnesses, the Memories panel and the compressor, and
// enrichment replaces it. Set in place on the event, never through a copy: a compression pass holds these same objects.

#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Memory
{
   /// <summary>Which memories carry a short code, and which of them a character has already discussed with the player.</summary>
   public static class MemoryRecall
   {
      /// <summary>The letter that opens a short code ("m12").</summary>
      public const string ShortUidPrefix = "m";

      /// <summary>
      ///   Gives a code to every memory without one, keeping the ones it has. The prompt's lines must not change during a
      ///   conversation, so the host calls this once, when a speaker opens. Taken past the highest code held: a number
      ///   freed by compression can come back, which is harmless since a code only means something in the prompt that showed it.
      /// </summary>
      public static void AssignShortUids(NpcProfile profile)
      {
         if (profile?.Events == null) return;

         int next = profile.Events.Select(e => NumberOf(e?.ShortUid)).DefaultIfEmpty(0).Max();
         foreach (NotableEvent ev in profile.Events)
            if (ev != null && string.IsNullOrEmpty(ev.ShortUid))
               ev.ShortUid = ShortUidPrefix + (++next).ToString(CultureInfo.InvariantCulture);
      }

      /// <summary>Marks the memories these codes name as discussed on this day. A code nobody holds is a model's slip and is ignored.</summary>
      public static void MarkDiscussed(NpcProfile profile, IReadOnlyList<string>? codes, int gameDay)
      {
         if (profile?.Events == null || codes == null || codes.Count == 0) return;

         foreach (NotableEvent ev in profile.Events)
            if (ev?.ShortUid != null && codes.Contains(ev.ShortUid, StringComparer.OrdinalIgnoreCase))
               ev.DiscussedDay = gameDay;
      }

      /// <summary>The codes of the memories already marked: what a conversation captures when it opens.</summary>
      public static IReadOnlyCollection<string> DiscussedSoFar(NpcProfile profile)
         => new HashSet<string>((profile?.Events ?? Enumerable.Empty<NotableEvent>())
            .Where(e => e?.DiscussedDay != null && e.ShortUid != null).Select(e => e.ShortUid!), StringComparer.OrdinalIgnoreCase);

      /// <summary>
      ///   True when this memory reads as already discussed. Inside a conversation only what was marked before it opened
      ///   counts, or the cached prompt would change on the turn after a tag. Null is a prompt outside a conversation: the field decides.
      /// </summary>
      public static bool ReadsAsDiscussed(NotableEvent ev, IReadOnlyCollection<string>? discussedAtOpening)
      {
         if (ev?.DiscussedDay == null) return false;

         return discussedAtOpening == null || (ev.ShortUid != null && discussedAtOpening.Contains(ev.ShortUid));
      }

      private static int NumberOf(string? code)
         => code != null && code.StartsWith(ShortUidPrefix, StringComparison.Ordinal)
            && int.TryParse(code.Substring(ShortUidPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int n)
               ? n
               : 0;
   }
}

// Code written by Gabriel Mailhot, 06/10/2026.
// Which memories a character is shown, and which compression must keep, once lived memories and hearsay are told
// apart (tashmetu, Nexus, 06/10/2026: a mod writing a memory for every piece of news a noble hears). Recency alone
// decided both, so heard news pushed lived history out of the Compact window and out of compression's protected tail.
// A lived memory is counted only against lived memories; hearsay has its own small allowance.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>Splits a character's memories into what they lived with the player and what they only heard.</summary>
   public static class HistoryWindowPolicy
   {
      /// <summary>TUNE: how many heard memories the Compact prompt shows, apart from the lived ones. Fixed so its cost cannot grow.</summary>
      public const int LeanHearsayLimit = 2;

      /// <summary>The header under which heard memories are shown.</summary>
      public const string HeardHeader = "WHAT HAS REACHED YOUR EARS (hearsay, not something you lived with this player):";

      /// <summary>True for a memory that is only word that reached the character.</summary>
      public static bool IsHeard(NotableEvent e) => e?.type == NotableEventType.Hearsay;

      /// <summary>The indices, oldest first, of the latest <paramref name="max" /> lived memories among the first <paramref name="end" />.</summary>
      public static IReadOnlyList<int> LatestLived(IReadOnlyList<NotableEvent> events, int max, int? end = null)
         => Latest(events, max, end, heard: false);

      /// <summary>The indices, oldest first, of the latest <paramref name="max" /> heard memories among the first <paramref name="end" />.</summary>
      public static IReadOnlyList<int> LatestHeard(IReadOnlyList<NotableEvent> events, int max, int? end = null)
         => Latest(events, max, end, heard: true);

      private static IReadOnlyList<int> Latest(IReadOnlyList<NotableEvent> events, int max, int? end, bool heard)
      {
         var picked = new List<int>();
         if (events == null || max <= 0) return picked;

         int stop = end is int e && e >= 0 && e <= events.Count ? e : events.Count;
         for (int i = stop - 1; i >= 0 && picked.Count < max; i--)
            if (IsHeard(events[i]) == heard) picked.Add(i);

         picked.Reverse();

         return picked;
      }
   }
}

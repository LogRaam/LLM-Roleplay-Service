// Code written by Gabriel Mailhot, 06/10/2026.
// Which memories a character is shown, and which compression must keep, once lived memories are told apart from what the
// character only heard (tashmetu, Nexus, 06/10/2026: a mod writing a memory for every piece of news a noble hears) and
// from their own life apart from the player (tashmetu, 08/10/2026: attachments, grudges, affairs between nobles).
// Recency alone decided both, so such memories pushed lived history out of the Compact window and out of compression's
// protected tail. A lived memory is counted only against lived memories; the others have their own small allowances.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>What a memory is to the character's history with the player.</summary>
   public enum MemoryStrand
   {
      /// <summary>Something lived with the player.</summary>
      Lived,

      /// <summary>Word that reached the character.</summary>
      Heard,

      /// <summary>The character's own life, apart from the player.</summary>
      OwnLife
   }

   /// <summary>Splits a character's memories into what they lived with the player, what they heard, and their own life.</summary>
   public static class HistoryWindowPolicy
   {
      /// <summary>TUNE: how many heard memories the Compact prompt shows, apart from the lived ones. Fixed so its cost cannot grow.</summary>
      public const int LeanHearsayLimit = 2;

      /// <summary>TUNE: how many memories of the character's own life the Compact prompt shows, apart from the lived ones.</summary>
      public const int LeanOwnLifeLimit = 2;

      /// <summary>The header under which heard memories are shown.</summary>
      public const string HeardHeader = "WHAT HAS REACHED YOUR EARS (hearsay, not something you lived with this player):";

      /// <summary>The header under which the character's own life is shown.</summary>
      public const string OwnLifeHeader = "YOUR OWN LIFE, APART FROM THIS PLAYER (your own affairs, not something you lived with them):";

      /// <summary>Which strand a memory belongs to.</summary>
      public static MemoryStrand StrandOf(NotableEvent e)
         => e?.type switch {
            NotableEventType.Hearsay => MemoryStrand.Heard,
            NotableEventType.OwnLife => MemoryStrand.OwnLife,
            _ => MemoryStrand.Lived
         };

      /// <summary>True for a memory that is only word that reached the character.</summary>
      public static bool IsHeard(NotableEvent e) => StrandOf(e) == MemoryStrand.Heard;

      /// <summary>The indices, oldest first, of the latest <paramref name="max" /> lived memories among the first <paramref name="end" />.</summary>
      public static IReadOnlyList<int> LatestLived(IReadOnlyList<NotableEvent> events, int max, int? end = null)
         => Latest(events, max, end, MemoryStrand.Lived);

      /// <summary>The indices, oldest first, of the latest <paramref name="max" /> heard memories among the first <paramref name="end" />.</summary>
      public static IReadOnlyList<int> LatestHeard(IReadOnlyList<NotableEvent> events, int max, int? end = null)
         => Latest(events, max, end, MemoryStrand.Heard);

      /// <summary>The indices, oldest first, of the latest <paramref name="max" /> memories of the character's own life.</summary>
      public static IReadOnlyList<int> LatestOwnLife(IReadOnlyList<NotableEvent> events, int max, int? end = null)
         => Latest(events, max, end, MemoryStrand.OwnLife);

      private static IReadOnlyList<int> Latest(IReadOnlyList<NotableEvent> events, int max, int? end, MemoryStrand strand)
      {
         var picked = new List<int>();
         if (events == null || max <= 0) return picked;

         int stop = end is int e && e >= 0 && e <= events.Count ? e : events.Count;
         for (int i = stop - 1; i >= 0 && picked.Count < max; i--)
            if (StrandOf(events[i]) == strand) picked.Add(i);

         picked.Reverse();

         return picked;
      }
   }
}

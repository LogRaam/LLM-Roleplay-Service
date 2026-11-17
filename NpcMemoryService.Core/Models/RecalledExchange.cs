// Code written by Gabriel Mailhot, 17/11/2026.
// Goeth (Discord, 17/11/2026): "recall the actual words of earlier talks, not only the summary". The host finds, in its own
// record of the story, a few short passages of earlier conversations that bear on what is being said now, and hands them to
// the prompt word for word. The host decides which passages a character may recall (only words the character heard); the
// prompt only frames them.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>One line of a recalled passage.</summary>
   public sealed class RecalledLine
   {
      /// <summary>Who said it, as the prompt labels it: "YOU" (the character), "PLAYER", or a name.</summary>
      public string Speaker { get; init; } = "";

      /// <summary>The words, exactly as said.</summary>
      public string Text { get; init; } = "";
   }

   /// <summary>A short passage of an earlier conversation the character took part in or heard.</summary>
   public sealed class RecalledExchange
   {
      /// <summary>When, told as elapsed time ("12 days ago", "earlier today").</summary>
      public string When { get; init; } = "";

      /// <summary>Where, when known ("Poros").</summary>
      public string? Place { get; init; }

      /// <summary>The others who were there and heard it, besides the character and the player.</summary>
      public IReadOnlyList<string> AlsoPresent { get; init; } = new List<string>();

      /// <summary>
      ///   The matter this passage shares with the turn, when it is one of several conversations on it ("Varcheg").
      ///   Gabriel (17/11/2026): a mission spoken of over several conversations must come back linked.
      /// </summary>
      public string? About { get; init; }

      /// <summary>The newest passage on that matter: it says how things stand now.</summary>
      public bool Latest { get; init; }

      /// <summary>The lines, in the order they were said.</summary>
      public IReadOnlyList<RecalledLine> Lines { get; init; } = new List<RecalledLine>();
   }
}

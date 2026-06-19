// Code written by Gabriel Mailhot, 18/06/2026.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Settlement-level knowledge injected into a commoner's slim system prompt.
   ///   Built game-side from the settlement's live state; tells the commoner
   ///   what they would plausibly know — who rules here, how things are going,
   ///   and what a street-level person would have heard recently.
   /// </summary>
   public sealed class CommonsKnowledge
   {
      /// <summary>Settlement name, e.g. "Pravend".</summary>
      public string? SettlementName { get; init; }

      /// <summary>Settlement type label: "town", "castle", or "village".</summary>
      public string? SettlementType { get; init; }

      /// <summary>Name of the holding lord, or null if the settlement has no lord.</summary>
      public string? HolderName { get; init; }

      /// <summary>Name of the kingdom this settlement belongs to, or null if independent.</summary>
      public string? KingdomName { get; init; }

      /// <summary>Cultural label, e.g. "Vlandian", "Sturgian", "Aserai".</summary>
      public string? Culture { get; init; }

      /// <summary>
      ///   Describes how the settlement is faring: "thriving", "stable",
      ///   "struggling", or "suffering".
      /// </summary>
      public string? ProsperityMood { get; init; }

      /// <summary>
      ///   A short note about notable threats or instability — e.g.
      ///   "under siege" or "troubled by bandits on the roads". Null when
      ///   security is ordinary.
      /// </summary>
      public string? SecurityNote { get; init; }

      /// <summary>
      ///   A brief note about active wars the settlement's kingdom is engaged in,
      ///   or null if there are none (or the settlement belongs to no kingdom).
      /// </summary>
      public string? ActiveWarsNote { get; init; }

      /// <summary>
      ///   Comma-separated names of notable lords currently visiting the settlement,
      ///   or null if none are present.
      /// </summary>
      public string? LordsPresent { get; init; }

      /// <summary>
      ///   Pre-formatted rumor lines (one per line, each prefixed with "- ") derived
      ///   from live game state: captured lords, recent deaths, war news, player
      ///   presence. Null when no relevant events are available. Injected into the
      ///   commoner prompt under "WHAT PEOPLE ARE TALKING ABOUT".
      /// </summary>
      public string? RumorsBlock { get; init; }
   }
}

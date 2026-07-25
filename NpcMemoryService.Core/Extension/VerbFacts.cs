// Code written by Gabriel Mailhot, 03/07/2026.
// Extension Surface, Actions volet (spike): the engine-agnostic FACTS an external verb's eligibility and
// teaching callbacks reason over. No TaleWorlds dependency: ids are plain strings, exactly like
// ExternalWorldEvent keeps the Volet A (world-event) ingestion door free of Bannerlord types. Mirrors that
// shape for a future "Volet B, Actions": external mods register conversation VERBS instead of pushing
// world facts.

using System.Collections.Generic;

namespace NpcMemoryService.Core.Extension
{
   /// <summary>
   ///   The engine-agnostic facts about the conversation partner an <see cref="ExternalVerbContract" />'s
   ///   callbacks reason over: who they are, their standing, and their relation to the player. The host
   ///   mod (Calradia Remembers) projects its own live engine state down to this shape before calling the
   ///   contract's callbacks; the external mod never sees, nor needs, the host's engine types.
   /// </summary>
   public sealed class VerbFacts
   {
      /// <summary>The conversation partner's stable engine id (a Hero StringId on the Bannerlord side).</summary>
      public string NpcId { get; set; } = string.Empty;

      /// <summary>The partner's personal relation score toward the player.</summary>
      public int RelationToPlayer { get; set; }

      /// <summary>The partner's faction id (kingdom/clan StringId), or null when they belong to none.</summary>
      public string? FactionId { get; set; }

      /// <summary>True when the partner holds a lord's rank (governs many "court" verbs' eligibility).</summary>
      public bool IsLord { get; set; }

      /// <summary>True when the partner is currently a captive (of anyone): most verbs exclude this.</summary>
      public bool IsPrisoner { get; set; }

      /// <summary>
      ///   Free-form extra facts an external verb may need that the base shape doesn't name, keyed by the
      ///   verb's own convention (mirrors how <c>ExternalWorldEvent</c> leaves category/source free-form).
      /// </summary>
      public IReadOnlyDictionary<string, string>? Extra { get; set; }
   }
}

// Code written by Gabriel Mailhot, 03/06/2026.
// Sprint multi-NPC: lightweight per-encounter witness descriptor.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Describes a single witness present during an encounter. Injected into the
   ///   system prompt so the NPC adjusts candor and behaviour based on who is watching.
   ///   Kept intentionally lightweight — name and relation only; full profiles are not
   ///   needed for passive witnesses.
   /// </summary>
   public sealed class WitnessEntry
   {
      /// <summary>Display name as it appears in the prompt (e.g. "Derthert").</summary>
      public required string Name { get; init; }

      /// <summary>
      ///   How this witness relates to the NPC being spoken to, from the NPC's
      ///   perspective. Examples: "your liege", "a rival lord", "your clan member",
      ///   "a lord you regard with quiet distrust".
      /// </summary>
      public required string RelationToNpc { get; init; }

      /// <summary>True when the witness is one of the player's own companions.</summary>
      public bool IsPlayerCompanion { get; init; }

      /// <summary>
      ///   Bannerlord <c>Hero.StringId</c> — used by the mod to resolve the witness's
      ///   portrait when displaying <c>[WITNESS_REACTION]</c> messages in the chat window.
      ///   Null for manually seeded console witnesses.
      /// </summary>
      public string? HeroStringId { get; init; }

      /// <summary>
      ///   A short character descriptor (archetype / trait) so the main NPC can voice
      ///   this witness's reactions true to their nature — an aloof witness reacts
      ///   differently from an impulsive one. Null when no profile is available.
      /// </summary>
      public string? Persona { get; init; }
   }
}

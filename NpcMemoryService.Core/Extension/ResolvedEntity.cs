// Code written by Gabriel Mailhot, 30/08/2026.
// Extension Surface, Actions guidance: one entity (hero, faction, or settlement) the PLAYER named in their
// message, as resolved by the host's own natural-language matching, engine-agnostic like every other shape
// this door exchanges with a third-party mod (VerbFacts, ActionRequest). Plain ids/strings only, so a modder
// never needs the host's Hero/Settlement/IFaction types to reason about what the player just referenced.

namespace NpcMemoryService.Core.Extension
{
   /// <summary>
   ///   One entity the player mentioned, as the host resolved it: its stable engine id, a display name, and
   ///   an optional short note (e.g. a hero's relation category to the NPC). Immutable.
   /// </summary>
   public sealed class ResolvedEntity
   {
      public ResolvedEntity(string id, string name, string? note = null)
      {
         Id = id;
         Name = name;
         Note = note;
      }

      /// <summary>The entity's stable engine id (a Hero/Settlement StringId, or a Kingdom/Clan StringId).</summary>
      public string Id { get; }

      /// <summary>A display name, ready to surface in guidance text or logs.</summary>
      public string Name { get; }

      /// <summary>A short optional note about the entity (e.g. a hero's relation category), or null when none applies.</summary>
      public string? Note { get; }
   }
}

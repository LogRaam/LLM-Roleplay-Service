// Code written by Gabriel Mailhot, 03/06/2026.
// Sprint multi-NPC: a visible reaction emitted by a witness during an encounter.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   A brief visible reaction (gesture, expression, one short line) from a
   ///   witness present during a conversation. Parsed from <c>[WITNESS_REACTION]</c>
   ///   blocks in the LLM response and displayed as a separate chat message with
   ///   the witness's own portrait.
   ///   Witnesses do not hold the floor — reactions are one sentence or less.
   /// </summary>
   public sealed class WitnessReaction
   {
      /// <summary>Witness name exactly as listed in the WITNESSES PRESENT section.</summary>
      public required string Name { get; init; }

      /// <summary>Stage direction or one short spoken line (e.g. "*She frowns.*").</summary>
      public required string Text { get; init; }
   }
}

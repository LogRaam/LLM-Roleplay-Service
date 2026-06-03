// Code written by Gabriel Mailhot, 03/06/2026.
// Sprint 12d: player-to-NPC letter model.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   A letter composed by the player and addressed to a specific NPC.
   ///   Stored on the recipient's <see cref="NpcProfile.ReceivedPlayerLetters" />.
   ///   The delivery pipeline marks <see cref="IsDelivered" /> when the courier
   ///   arrives; the letter is injected into the NPC's system prompt until
   ///   <see cref="HasBeenRead" /> is set after the first dialogue response
   ///   that follows delivery.
   /// </summary>
   public sealed class PlayerLetter
   {
      public string? RecipientId   { get; init; }
      public string? RecipientName { get; init; }

      /// <summary>The player's message content, verbatim as typed.</summary>
      public string? Content { get; init; }

      public int  SentOnDay   { get; init; }
      public int  DeliveryDay { get; init; }

      /// <summary>True once the courier has arrived (DeliveryDay ≤ today).</summary>
      public bool IsDelivered { get; set; }

      /// <summary>
      ///   Set to true after the NPC's first dialogue response following delivery,
      ///   preventing the letter from being re-injected into future prompts.
      /// </summary>
      public bool HasBeenRead { get; set; }
   }
}

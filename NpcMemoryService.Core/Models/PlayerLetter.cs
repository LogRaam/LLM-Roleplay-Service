// Code written by Gabriel Mailhot, 03/06/2026.
// Sprint 12d: player-to-NPC letter model.
// Sprint 12c: visible courier party (CourierPartyId, IsLost).

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

      /// <summary>
      ///   Sprint 12c — Bannerlord <c>MobileParty.StringId</c> of the visible courier
      ///   carrying this letter across the campaign map. Null when no courier was
      ///   spawned (spawn failed, or a legacy letter). Cleared once the courier is
      ///   gone (delivered or lost).
      /// </summary>
      public string? CourierPartyId { get; set; }

      /// <summary>
      ///   Sprint 12c — set true when the courier was destroyed in transit (waylaid by
      ///   bandits). A lost letter is never delivered and never injected into a prompt.
      /// </summary>
      public bool IsLost { get; set; }

      /// <summary>
      ///   Sprint 12d — set true once <see cref="LetterGenerationService" /> has been
      ///   asked to generate an NPC reply, preventing duplicate LLM calls on subsequent
      ///   daily ticks. Set regardless of whether the LLM ultimately decides to reply.
      /// </summary>
      public bool HasTriggeredReply { get; set; }
   }
}

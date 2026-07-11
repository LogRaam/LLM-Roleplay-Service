// Code written by Gabriel Mailhot, 01/07/2026.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Why one of the player's own companions asked for a private audience (the popup's "Let us speak of it"
   ///   opened a real conversation). Drives the companion's opening turn and, for a retirement, the in-chat
   ///   surface that lets the player grant their leave or persuade them to stay.
   /// </summary>
   public enum CompanionAudienceReason
   {
      /// <summary>Not an audience — an ordinary conversation.</summary>
      None,

      /// <summary>They are unhappy in the player's service and asked to air the grievance.</summary>
      Grievance,

      /// <summary>They are war-weary and wish to retire (leave, or step back from the field if landed).</summary>
      Retirement,

      /// <summary>
      ///   A faithful, content companion has come to confide genuine affection born of shared war, and wishes
      ///   to become the player's discreet SECRET LOVER. Never a demand or a bargain. Added last to preserve the
      ///   serialized ordinals of existing saves.
      /// </summary>
      SecretAffection
   }
}

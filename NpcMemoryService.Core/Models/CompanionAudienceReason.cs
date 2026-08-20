// Code written by Gabriel Mailhot, 01/07/2026.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Why one of the player's own companions asked for a private audience (the popup's "Let us speak of it"
   ///   opened a real conversation). Drives the companion's opening turn and, for a retirement, the in-chat
   ///   surface that lets the player grant their leave or persuade them to stay.
   ///   ORDINALS ARE FROZEN (persisted nowhere directly today, but treated as if they were, exactly like
   ///   <c>NotableEventType</c> / <c>GrudgeSource</c>): new reasons are always APPENDED at the end, never
   ///   inserted or reordered.
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
      ///   to become the player's discreet SECRET LOVER. Never a demand or a bargain.
      /// </summary>
      SecretAffection,

      /// <summary>
      ///   A dispatched companion mission has a result to report (news, intel, gold, or a faction's mood) that
      ///   the player has not yet heard in conversation. Player-feedback pillar (Nexus): a found-topic audience,
      ///   never a mood-only "check on me" popup.
      /// </summary>
      MissionFollowUp,

      /// <summary>
      ///   A real, unresolved grudge between this companion and another member of the player's own party: the
      ///   holder wants the player to take their side. Reads the Grudges pillar's own ledger; never a parallel
      ///   notion of conflict.
      /// </summary>
      CompanionConflict,

      /// <summary>
      ///   A measurable, currently-true condition of the party worth acting on (food monotony, low morale,
      ///   wounded troops with no surgeon, an overloaded train). Computed from live state only, so the counsel
      ///   is never stale advice.
      /// </summary>
      PracticalCounsel,

      /// <summary>
      ///   An old and significant shared memory resurfaces unprompted, paying off a detail the mod already
      ///   stored but never otherwise brings back on its own.
      /// </summary>
      Callback,

      /// <summary>
      ///   The companion brings word of the world: a rumour they could plausibly have heard (the WorldEventStore's
      ///   own awareness rules), never one already reported to the player.
      /// </summary>
      RumourReport,

      /// <summary>
      ///   A content companion has come, unbidden, to thank the player for a specific kindness (a gift, being
      ///   ransomed or freed, troops or influence spent for them) and to reaffirm the loyalty that shared service
      ///   built. Purely warm: never a request, a bargain, or a demand of any kind (a future "ask for something"
      ///   reason is deliberately separate from this one).
      /// </summary>
      Gratitude,

      /// <summary>
      ///   A loyal companion has come to WARN the player that someone is secretly plotting against them (the
      ///   scheme against the player, SchemeStore's own ledger). Urgent and protective, born of loyalty: never a
      ///   request, a bargain, or a demand, and the companion asks for nothing in return. Granting the audience
      ///   HEEDS the warning (the scheme is exposed and its real consequences apply); refusing costs nothing and
      ///   leaves the scheme live, exactly like every other found-topic.
      /// </summary>
      SchemeWarning
   }
}

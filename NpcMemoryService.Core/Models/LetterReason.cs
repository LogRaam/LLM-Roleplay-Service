// Code written by Gabriel Mailhot, 02/06/2026.
// Sprint 12b: NPC-initiated letter correspondence.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   The circumstance that prompted an NPC to write to the player. Used both as
   ///   a label (so the LLM knows the occasion) and as a categorisation for future
   ///   filtering. Only a subset of values are evaluated in Sprint 12b; the rest are
   ///   reserved for later sprints.
   /// </summary>
   public enum LetterReason
   {
      // ── Event-triggered ────────────────────────────────────────────────────

      /// <summary>Player won a tournament — admirers or rivals may write.</summary>
      TournamentVictory,

      /// <summary>Player won a notable battle — allies may congratulate.</summary>
      BattleVictory,

      /// <summary>An ongoing quest's deed was stamped — giver may check in.</summary>
      QuestUpdate,

      // ── Political / military ────────────────────────────────────────────────

      /// <summary>A parent wishes to propose a match between their child and the player.</summary>
      MarriageProposal,

      /// <summary>A lord calls for the player's military aid during an active war.</summary>
      ReinforcementRequest,

      /// <summary>A gang leader asks the player to strike a rival gang.</summary>
      GangFavor,

      /// <summary>A faction leader proposes or hints at a political alliance.</summary>
      PoliticalAlliance,

      // ── Personal ────────────────────────────────────────────────────────────

      /// <summary>
      ///   An NPC in a romantic relationship with the player misses them or wants to
      ///   deepen the connection — written when they have not met in several days.
      /// </summary>
      RomanticCorrespondence,

      /// <summary>The player's vanilla spouse or <c>SecretLover</c> writes from afar.</summary>
      SpouseCorrespondence,

      // ── Adversarial ─────────────────────────────────────────────────────────

      /// <summary>An enemy tries to bribe or pressure the player for sensitive information.</summary>
      CorruptionAttempt,

      /// <summary>
      ///   A third party threatens to expose the player's secret lover unless paid.
      ///   Requires player to have a spouse AND a <c>SecretLover</c> NPC.
      /// </summary>
      Blackmail,

      // ── Family ──────────────────────────────────────────────────────────────

      /// <summary>
      ///   A woman informs the player that she has given birth to his child —
      ///   whether the child is legitimate (player's vanilla spouse) or born
      ///   outside of marriage.
      /// </summary>
      BirthAnnouncement,

      /// <summary>
      ///   A mother asks the player for financial support to help raise their child.
      ///   Translates into a <c>ProvideGold</c> quest in the vanilla quest journal.
      /// </summary>
      ChildSupportRequest,

      // ── Sprint 12d ──────────────────────────────────────────────────────────

      /// <summary>
      ///   NPC is replying to a letter the player sent via courier. The LLM decides
      ///   whether to write at all; if so, a visible reply courier travels to the player.
      /// </summary>
      PlayerLetterReply,

      // ── Sprint 23 ───────────────────────────────────────────────────────────

      /// <summary>
      ///   The NPC sent a romantic letter but received no reply after several days.
      ///   They write again — impatient, longing, or a little wounded — asking whether
      ///   the player received their first letter and why they have not answered.
      /// </summary>
      AwaitingReply
   }
}

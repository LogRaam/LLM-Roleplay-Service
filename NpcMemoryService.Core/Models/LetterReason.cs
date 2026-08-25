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
      AwaitingReply,

      // ── Sprint 24 (jealousy) ─────────────────────────────────────────────────

      /// <summary>
      ///   A jealous party — the wronged spouse of someone the player courted, the player's
      ///   own slighted spouse, a rival suitor, or a spurned admirer — writes to confront,
      ///   warn, or threaten the player over a romantic act. Tone and severity scale with the
      ///   writer's stake and their culture's view of exclusivity vs. shared partners.
      /// </summary>
      JealousThreat,

      // ── Bastards ─────────────────────────────────────────────────────────────

      /// <summary>
      ///   The mother of the player's hidden bastard writes — but not to extort (that is
      ///   <see cref="Blackmail" />). Her tone follows her disposition: a fond woman longs for him and the
      ///   child, a wronged one writes coldly, a pragmatic one asks for the child's keep. Added last to
      ///   preserve the serialized ordinals of existing saves.
      /// </summary>
      BastardMotherNote,

      // ── Stance with teeth ────────────────────────────────────────────────────

      /// <summary>
      ///   A lord whose posture has hardened into OPEN enmity writes unbidden — a cold word of displeasure, or
      ///   a bold lord's open challenge to settle things blade to blade. (Secret hatred — a schemer, a would-be
      ///   assassin — does NOT write; it would tip its hand.) Added last to preserve old-save ordinals.
      /// </summary>
      StanceHostility,

      /// <summary>
      ///   A lord who holds the player in genuine regard writes unbidden to offer friendship, aid, or a
      ///   promise to warn them of anything moving against them. Added last to preserve old-save ordinals.
      /// </summary>
      StanceFavor,

      // ── Companion retention ──────────────────────────────────────────────────

      /// <summary>
      ///   A companion who grew unhappy in the player's service, asked repeatedly for an audience, and was
      ///   refused each time, writes a final farewell: they tried to speak with the player and could not, and
      ///   so they have left. The companion departs when this letter arrives. Added last to preserve old-save
      ///   ordinals.
      /// </summary>
      CompanionFarewell,

      // ── Divorce, Phase 2b (the player's own spouse demands) ──────────────────

      /// <summary>
      ///   The player's own spouse, grown deeply unhappy, writes to announce that they can no longer bear
      ///   the marriage and mean to press the player to end it when next they speak. The demand itself is
      ///   then resolved entirely in conversation (accept or refuse, no button). Added last to preserve
      ///   old-save ordinals.
      /// </summary>
      SpouseDivorceDemand,

      // ── Divorce escalation (the refused spouse begins ending it themselves) ──────────────────

      /// <summary>
      ///   The player's own spouse, refused too many times, writes to announce that they no longer wait
      ///   on the player's consent: they have begun ending the marriage themselves, on a deadline. Only a
      ///   genuinely repaired bond, not mere words, can still call it off. Added last to preserve old-save
      ///   ordinals.
      /// </summary>
      SpouseDivorceEscalation,

      // ── Interception fallback ─────────────────────────────────────────────────

      /// <summary>
      ///   A lord who rode out to intercept the player (a territorial challenge, or reacting to fresh news
      ///   of the player's deeds) never managed to force the meeting: the chase went stale or was outrun.
      ///   Rather than the whole thing evaporating, the lord writes the same intent by letter instead. Added
      ///   last to preserve old-save ordinals.
      /// </summary>
      InterceptionMissed,

      // ── Realm tidings (news of the realm) ─────────────────────────────────────

      /// <summary>
      ///   An NPC who holds the player in genuine regard writes unbidden with news stirring in their realm
      ///   (a looming war, hard times fallen on a town, a rumour that reached them). They RELAY a real world
      ///   event they know of, in character, never inventing a fact. Added last to preserve old-save ordinals.
      /// </summary>
      RealmTidings,

      // ── Intercepted correspondence (a leaked letter falls into the wrong hands) ────

      /// <summary>
      ///   An antagonist has come into possession of a private letter the player sent to someone else, intercepted
      ///   on the road after its courier was waylaid by bandits, and writes to the player to exploit what they
      ///   learned. Added last to preserve old-save ordinals.
      /// </summary>
      LeakedCorrespondence,

      // ── Lord offers (a proposition the player can accept or decline) ──────────────

      /// <summary>
      ///   A lord who is NOT the player's own liege writes to poach them: leave your lord and enter my service,
      ///   and I will make it worth your while. The letter names a concrete enticement (a purse of gold, a castle
      ///   of their own, a company of soldiers). A light offer: accepting warms the bond and opens the door, it
      ///   does not itself change the player's allegiance. Added last to preserve old-save ordinals.
      /// </summary>
      ServiceOffer,

      /// <summary>
      ///   A lord writes offering to buy or ransom a specific prisoner the PLAYER currently holds (their own kin,
      ///   a clansman, an ally, or an enemy they want in hand), naming their price. Accepting settles the trade:
      ///   the prisoner passes to the lord and the player is paid. Added last to preserve old-save ordinals.
      /// </summary>
      PrisonerOffer
   }
}

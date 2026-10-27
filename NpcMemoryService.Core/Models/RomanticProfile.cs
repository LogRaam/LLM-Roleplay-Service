// Code written by Gabriel Mailhot, 18/06/2026.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Romantic and sexual profile attached to an <see cref="NpcProfile" />.
   ///   Layered design (each layer optional, surfaced according to
   ///   <see cref="AdultContentLevel" />):
   ///   - Layer 1 — <see cref="ArchetypeName" /> + <see cref="RelationalSketch" />
   ///   Derived from the NPC's Bannerlord traits. Describes how this
   ///   person courts, what they value in attachment, fidelity stance,
   ///   emotional rhythm. Safe for all adult levels &gt;= Mature.
   ///   - Layer 2 — <see cref="Orientation" /> + <see cref="IsFemale" />
   ///   Determined once at profile creation. Gates whether the player
   ///   is even a viable romantic target.
   ///   - Layer 3 — <see cref="Preferences" />
   ///   Relational dynamics (dominant, possessive, monogamous, etc.).
   ///   Always safe to surface at Mature level and above.
   ///   - Layer 4 — <see cref="IntimateSketch" /> + <see cref="Kinks" />
   ///   <see cref="IntimateSketch" /> surfaces at Explicit and above.
   ///   <see cref="Kinks" /> surface only at Hardcore.
   ///   The mutable state — <see cref="AttractionToPlayer" /> and
   ///   <see cref="Status" /> — evolves through conversations.
   /// </summary>
   public sealed class RomanticProfile
   {
      // ── Layer 1: derived from traits (set at creation) ───────────────

      public string ArchetypeName { get; init; } = "";

      // ── Mutable state ─────────────────────────────────────────────────

      /// <summary>
      ///   Attraction toward the player. Separate from
      ///   <see cref="NpcProfile.ReputationWithPlayer" />: an NPC may
      ///   respect the player without desiring them, or the reverse.
      ///   Clamped to [-100, 100].
      /// </summary>
      public int AttractionToPlayer { get; set; }

      /// <summary>
      ///   Intimate sketch surfaced at <see cref="AdultContentLevel.Explicit" />
      ///   and above. Describes the texture of physical closeness, the
      ///   dynamics they seek, their patterns of vulnerability.
      /// </summary>
      public string IntimateSketch { get; init; } = "";

      /// <summary>
      ///   True when the player and this NPC have explicitly formed a consort bond via
      ///   the <c>take_as_consort</c> action. A consort is a committed partner whose
      ///   bond is real but not recognised by Calradian law — no clan merger, no
      ///   inheritance implications. Distinct from the native <c>Hero.Spouse</c> link.
      ///   Once set, <see cref="Status" /> is <see cref="RomanticStatus.Committed" />.
      /// </summary>
      public bool IsConsort { get; set; }

      /// <summary>
      ///   True when a faithful COMPANION has become the player's SECRET LOVER via the
      ///   <c>take_as_secret_lover</c> action: an intimate bond born of shared war, kept
      ///   deliberately hidden to avoid stirring jealousy. Distinct from <see cref="IsConsort" />
      ///   (open and committed) and from <see cref="RomanticStatus.SecretLover" /> as reached by a
      ///   MARRIED NPC's affair: this is the player's own discreet lover. Additive, defaults false on
      ///   older saves. When set, <see cref="Status" /> is <see cref="RomanticStatus.SecretLover" />.
      /// </summary>
      public bool IsSecretLover { get; set; }

      /// <summary>
      ///   The in-game day the secret bond (<see cref="IsSecretLover" />) began, or null when there is none. The
      ///   consumer uses it to hold the secret genuinely secret for a while before whispers of its EXISTENCE (never
      ///   the player's identity) may begin to circulate as gossip (mystery-lover pillar). Additive; null on older
      ///   saves and cleared when the bond ends.
      /// </summary>
      public int? SecretLoverSinceDay { get; set; }

      /// <summary>
      ///   True when this partner and the player hold RECIPROCAL OPEN TERMS: an open couple, each free to
      ///   love others, and neither wronged by the other's loves. Set via the <c>open_relationship</c> action,
      ///   which either the player or the partner herself may propose (she may ask leave to love another, or
      ///   urge the player to take another). Because the arrangement is mutual, she is NOT jealous of the
      ///   player's other partners: it suppresses her competition and event-triggered jealousy alike. Layers
      ///   on top of an existing spouse or consort bond, it does not replace it. Additive and save-safe:
      ///   absent on older saves, where it defaults to false (no migration). Distinct from
      ///   <see cref="IsConsort" /> (a committed bond) and <see cref="IsSecretLover" /> (a hidden one).
      /// </summary>
      public bool IsOpenArrangement { get; set; }

      /// <summary>
      ///   True when this partner has said she accepts the player having OTHER partners, while remaining
      ///   exclusive to the player herself. An asymmetric, CLOSED arrangement, and deliberately not
      ///   <see cref="IsOpenArrangement" />, which is the reciprocal one.
      ///   <para>
      ///     yozakura12 (Nexus, 22/09/2026) needed the distinction and did not have it: "accepting the player
      ///     having multiple wives is not the same as agreeing to a reciprocal open relationship... The player
      ///     may have multiple wives/consorts, but each wife remains exclusive to the player." Merging the two
      ///     is not a rounding error, because open terms SUPPRESS her jealousy outright and for good, where
      ///     this only changes what it costs her (<see cref="Logic" /> side: JealousyTrackPolicy).
      ///   </para>
      ///   <para>
      ///     It is her WORD, so it can be withdrawn: set false again and she returns to her own culture's
      ///     price. It covers the household she was told about and nothing else, so a bond kept from her is
      ///     still a betrayal at full price. Additive and save-safe: absent on older saves, where it defaults
      ///     to false, which is exactly today's behaviour.
      ///   </para>
      /// </summary>
      public bool AcceptsPlayersOtherPartners { get; set; }

      /// <summary>
      ///   The StringId of the NPC hero this OPEN partner has taken as HER OWN lover: an affair she is free
      ///   to have under the reciprocal open arrangement (<see cref="IsOpenArrangement" />), formed
      ///   autonomously over time when she exercises that same freedom (especially when the player neglects
      ///   her). Distinct from <see cref="IsSecretLover" />, which is about the PLAYER being a companion's
      ///   secret lover: this points the other way, at a third party SHE loves. Null until she takes one.
      ///   Set by the open-partner autonomy; a future increment surfaces it to the player and can parent her
      ///   child by him TO HIM, not the player. Additive and save-safe: absent on older saves, where it
      ///   defaults to null (no migration).
      /// </summary>
      public string? TakenLoverId { get; set; }

      /// <summary>
      ///   True when <see cref="TakenLoverId" /> is a HIDDEN affair rather than an openly held lover: a wife of a
      ///   harem / patriarchal (submissive-wife) culture who keeps her lover secret from her husband, or ANY
      ///   infidelity taken OUTSIDE a reciprocal open arrangement. False for an OPEN lover held under such an
      ///   arrangement, whom the partner does not hide. This distinguishes how a later increment (3b) surfaces
      ///   the lover: an open lover is already known to the player, whereas a secret affair must be DISCOVERED.
      ///   Additive and save-safe: absent on older saves, where it defaults to false (an open lover, no migration).
      /// </summary>
      public bool LoverIsSecret { get; set; }

      /// <summary>
      ///   How many TRYSTS this partner has had with her <see cref="TakenLoverId" /> lover: private meetings that
      ///   happen when the player is away, each nudging their bond a little. Rises by one per tryst. Feeds two
      ///   later things: the discovery chance of a SECRET affair (which climbs with this count) and the
      ///   pregnancy-by-the-lover hook (Inc 3c). Additive and save-safe: absent on older saves, where it
      ///   defaults to 0 (no migration).
      /// </summary>
      public int LoverTrystCount { get; set; }

      /// <summary>
      ///   True once the player has become AWARE of this partner's <see cref="TakenLoverId" /> lover: gently for
      ///   an OPEN lover (word simply reaches him, she was free), or as a SCANDAL when a SECRET affair
      ///   (<see cref="LoverIsSecret" />) is discovered. Gates the one-time surfacing so it never repeats.
      ///   Additive and save-safe: absent on older saves, where it defaults to false (no migration).
      /// </summary>
      public bool LoverKnownToPlayer { get; set; }

      /// <summary>
      ///   True once a SECRET affair (<see cref="LoverIsSecret" />) has been DISCOVERED by the player: a hidden
      ///   affair leaks over time, the likelier the more trysts it has seen. Distinct from
      ///   <see cref="LoverKnownToPlayer" />, which an OPEN lover also sets WITHOUT any scandal: this marks
      ///   specifically the betrayal beat, so a later increment (3b-b) can gate a confrontation on it. Never set
      ///   for an open lover. Additive and save-safe: absent on older saves, where it defaults to false (no migration).
      /// </summary>
      public bool AffairDiscovered { get; set; }

      public bool IsFemale { get; init; }

      // ── Layer 4: kinks (set at creation, Hardcore only) ──────────────

      public List<Kink> Kinks { get; init; } = new();

      // ── Layer 4b: the gestures they love and dislike (Hardcore only; Gabriel, 26/10/2026) ──

      /// <summary>
      ///   The gestures this character particularly loves (<see cref="IntimateAct" />). Null until rolled: profiles made
      ///   before 27/10/2026 have none, and are given theirs the next time they are read (rolled once, then kept).
      /// </summary>
      public List<IntimateAct>? LovedActs { get; set; }

      /// <summary>
      ///   The gestures this character dislikes, often none. They refuse them, frankly or in an unease the player can read;
      ///   never with feigned pleasure. Null until rolled, like <see cref="LovedActs" />.
      /// </summary>
      public List<IntimateAct>? DislikedActs { get; set; }

      /// <summary>
      ///   The gestures this character is less fond of (Gabriel, 27/10/2026: "une chose qu'elle aime moins"). They go along
      ///   with them without eagerness, never feigning delight, and it costs the partner nothing. Null until rolled:
      ///   profiles rolled before this have none and are given theirs the next time they are read.
      /// </summary>
      public List<IntimateAct>? LessFondActs { get; set; }

      /// <summary>
      ///   The gestures, among <see cref="LovedActs" />, <see cref="LessFondActs" /> and <see cref="DislikedActs" />, whose taste the player has learned
      ///   by living it with them (a gesture they love found, one they dislike met with a refusal or an unease). Null or
      ///   empty when none. Additive, absent in older saves.
      /// </summary>
      public List<IntimateAct>? KnownActs { get; set; }

      // ── Layer 2: orientation (rolled at creation; overridable by authored char data) ──

      // Settable, not init-only: a player may deliberately change an NPC's orientation via
      // character_overrides.json (or the cr.orientation console), e.g. to enable a same-sex romance.
      public SexualOrientation Orientation { get; set; } = SexualOrientation.Heterosexual;

      // ── Layer 3: preferences (set at creation) ───────────────────────

      public List<RomanticPreference> Preferences { get; init; } = new();

      /// <summary>
      ///   Always-safe sketch of how this NPC approaches romance — courting
      ///   patterns, fidelity stance, what they value. No explicit content.
      /// </summary>
      public string RelationalSketch { get; init; } = "";

      public RomanticStatus Status { get; set; } = RomanticStatus.None;

      /// <summary>
      ///   The in-game day <see cref="Status" /> last CHANGED, stamped by <see cref="MoveTo" /> and
      ///   <see cref="NoteChangeFrom" />. The encyclopedia's Heart section dates the bond with it ("your secret lover
      ///   since spring 1091", Gabriel, 20/10/2026). An event that leaves the status as it was never restamps it, or the
      ///   date would drift to the last conversation. Additive: null on older saves, and the page then gives no date.
      /// </summary>
      public int? StatusSinceDay { get; set; }

      /// <summary>
      ///   The in-game day the bond FIRST became intimate (<see cref="RomanticStatus.Intimate" /> or
      ///   <see cref="RomanticStatus.SecretLover" />). Set once and kept through every later change, so a bond that
      ///   became a marriage, or was broken, still remembers when it began. Additive: null on older saves.
      /// </summary>
      public int? LoversSinceDay { get; set; }

      /// <summary>Moves the bond to <paramref name="status" /> on <paramref name="day" />, dating it only when it changes.</summary>
      public void MoveTo(RomanticStatus status, int day)
      {
         RomanticStatus before = Status;
         Status = status;
         NoteChangeFrom(before, day);
      }

      /// <summary>
      ///   Dates a change already made to <see cref="Status" /> (it was <paramref name="before" />) on
      ///   <paramref name="day" />. Nothing is stamped when the status did not move.
      /// </summary>
      public void NoteChangeFrom(RomanticStatus before, int day)
      {
         if (Status == before) return;

         StatusSinceDay = day;
         if (LoversSinceDay == null && (Status == RomanticStatus.Intimate || Status == RomanticStatus.SecretLover)) LoversSinceDay = day;
      }

      /// <summary>
      ///   Carries everything the player and this character have LIVED onto a freshly rebuilt profile: a rebuild (an
      ///   orientation fix, a missing profile built) remakes only what is derived from traits. Every settable property
      ///   but <see cref="Orientation" /> is carried, since fixing the orientation is why a rebuild fires
      ///   (RomanticProfileCarryTests holds the list complete; until 20/10/2026 the secret lover's day and her own
      ///   lover were silently dropped).
      /// </summary>
      public void CarryRelationshipFrom(RomanticProfile? previous)
      {
         if (previous == null) return;

         Status = previous.Status;
         StatusSinceDay = previous.StatusSinceDay;
         LoversSinceDay = previous.LoversSinceDay;
         AttractionToPlayer = previous.AttractionToPlayer;
         IsConsort = previous.IsConsort;
         IsSecretLover = previous.IsSecretLover;
         SecretLoverSinceDay = previous.SecretLoverSinceDay;
         IsOpenArrangement = previous.IsOpenArrangement;
         AcceptsPlayersOtherPartners = previous.AcceptsPlayersOtherPartners;
         TakenLoverId = previous.TakenLoverId;
         LoverIsSecret = previous.LoverIsSecret;
         LoverTrystCount = previous.LoverTrystCount;
         LoverKnownToPlayer = previous.LoverKnownToPlayer;
         AffairDiscovered = previous.AffairDiscovered;
         // Their tastes are not derived from traits: a rebuild keeps the ones already rolled.
         if (previous.LovedActs != null) LovedActs = previous.LovedActs;
         if (previous.DislikedActs != null) DislikedActs = previous.DislikedActs;
         if (previous.LessFondActs != null) LessFondActs = previous.LessFondActs;
         KnownActs = previous.KnownActs;
      }

      // ── Compatibility ─────────────────────────────────────────────────────

      /// <summary>
      ///   Returns true when the player is within this NPC's attraction.
      ///   Single authoritative location for the orientation × player-gender rule —
      ///   all callers (PromptBuilder, HeroProfileMapper diagnostic) delegate here.
      /// </summary>
      public bool IsCompatibleWith(bool playerIsFemale)
         => Orientation switch {
            SexualOrientation.Heterosexual => playerIsFemale != IsFemale,
            SexualOrientation.BiCurious => true,
            SexualOrientation.Bisexual => true,
            SexualOrientation.Homosexual => playerIsFemale == IsFemale,
            SexualOrientation.Pansexual => true,
            SexualOrientation.Asexual => false,
            _ => false
         };
   }
}
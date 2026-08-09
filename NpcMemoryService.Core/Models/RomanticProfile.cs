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

      public bool IsFemale { get; init; }

      // ── Layer 4: kinks (set at creation, Hardcore only) ──────────────

      public List<Kink> Kinks { get; init; } = new();

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
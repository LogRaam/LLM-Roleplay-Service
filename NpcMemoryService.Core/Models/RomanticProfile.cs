// Code written by Gabriel Mailhot, 24/05/2026.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Romantic and sexual profile attached to an <see cref="NpcProfile"/>.
    ///
    ///   Layered design (each layer optional, surfaced according to
    ///   <see cref="AdultContentLevel"/>):
    ///
    ///   - Layer 1 — <see cref="ArchetypeName"/> + <see cref="RelationalSketch"/>
    ///       Derived from the NPC's Bannerlord traits. Describes how this
    ///       person courts, what they value in attachment, fidelity stance,
    ///       emotional rhythm. Safe for all adult levels &gt;= Mature.
    ///
    ///   - Layer 2 — <see cref="Orientation"/> + <see cref="IsFemale"/>
    ///       Determined once at profile creation. Gates whether the player
    ///       is even a viable romantic target.
    ///
    ///   - Layer 3 — <see cref="Preferences"/>
    ///       Relational dynamics (dominant, possessive, monogamous, etc.).
    ///       Always safe to surface at Mature level and above.
    ///
    ///   - Layer 4 — <see cref="IntimateSketch"/> + <see cref="Kinks"/>
    ///       <see cref="IntimateSketch"/> surfaces at Explicit and above.
    ///       <see cref="Kinks"/> surface only at Hardcore.
    ///
    ///   The mutable state — <see cref="AttractionToPlayer"/> and
    ///   <see cref="Status"/> — evolves through conversations.
    /// </summary>
    public sealed class RomanticProfile
    {
        // ── Layer 1: derived from traits (set at creation) ───────────────

        public string ArchetypeName { get; init; } = "";

        /// <summary>
        ///   Always-safe sketch of how this NPC approaches romance — courting
        ///   patterns, fidelity stance, what they value. No explicit content.
        /// </summary>
        public string RelationalSketch { get; init; } = "";

        /// <summary>
        ///   Intimate sketch surfaced at <see cref="AdultContentLevel.Explicit"/>
        ///   and above. Describes the texture of physical closeness, the
        ///   dynamics they seek, their patterns of vulnerability.
        /// </summary>
        public string IntimateSketch { get; init; } = "";

        // ── Layer 2: orientation (set at creation) ───────────────────────

        public SexualOrientation Orientation { get; init; } = SexualOrientation.Heterosexual;
        public bool IsFemale { get; init; }

        // ── Layer 3: preferences (set at creation) ───────────────────────

        public List<RomanticPreference> Preferences { get; init; } = new List<RomanticPreference>();

        // ── Layer 4: kinks (set at creation, Hardcore only) ──────────────

        public List<Kink> Kinks { get; init; } = new List<Kink>();

        // ── Mutable state ─────────────────────────────────────────────────

        /// <summary>
        ///   Attraction toward the player. Separate from
        ///   <see cref="NpcProfile.ReputationWithPlayer"/>: an NPC may
        ///   respect the player without desiring them, or the reverse.
        ///   Clamped to [-100, 100].
        /// </summary>
        public int AttractionToPlayer { get; set; }

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
                SexualOrientation.BiCurious    => true,
                SexualOrientation.Bisexual     => true,
                SexualOrientation.Homosexual   => playerIsFemale == IsFemale,
                SexualOrientation.Pansexual    => true,
                SexualOrientation.Asexual      => false,
                _                              => false
            };
    }
}

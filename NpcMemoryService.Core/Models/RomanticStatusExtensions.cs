// Code written by Gabriel Mailhot, 17/07/2026.
// Romance audit M-R1: RomanticStatus is an ENUM ORDERED for narrative progression, not for magnitude, yet
// several gates compared it ordinally (Status >= Courting, Status < Intimate). Because Estranged and Broken sit
// AFTER the positive tiers in that order, a terminal-negative romance slipped through every "at least courting /
// intimate" gate: a Broken NPC read as consort-eligible, as romantically bound, and still drew romantic letters.
// These predicates name the intent (an ACTIVE positive bond of at least a given tier) so no ordinal accident can
// ever again treat a dead romance as a live one.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Intent-named tests over <see cref="RomanticStatus" /> that treat <see cref="RomanticStatus.Estranged" />
    ///   and <see cref="RomanticStatus.Broken" /> as the terminal-negative states they are, never as "beyond
    ///   intimate" the way a raw ordinal comparison did.
    /// </summary>
    public static class RomanticStatusExtensions
    {
        /// <summary>
        ///   True for an ACTIVE romance at least at the courting tier: Courting, Intimate, SecretLover, or
        ///   Committed. Excludes the pre-romantic None/Curious AND the terminal Estranged/Broken.
        /// </summary>
        public static bool IsCourtingOrDeeper(this RomanticStatus status)
            => status == RomanticStatus.Courting
               || status == RomanticStatus.Intimate
               || status == RomanticStatus.SecretLover
               || status == RomanticStatus.Committed;

        /// <summary>
        ///   True for an ACTIVE romance at least at the intimate tier: Intimate, SecretLover, or Committed.
        ///   Excludes everything below Intimate AND the terminal Estranged/Broken.
        /// </summary>
        public static bool IsIntimateOrDeeper(this RomanticStatus status)
            => status == RomanticStatus.Intimate
               || status == RomanticStatus.SecretLover
               || status == RomanticStatus.Committed;
    }
}

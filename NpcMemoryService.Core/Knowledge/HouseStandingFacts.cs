// Code written by Gabriel Mailhot, 11/09/2026.
// The first knowledge pack's payload. fkasad, 2026-09-11, after finding his own governor did not know he
// governed: "I feel like companions should have a better understanding of the clan current status, territories,
// armies, treasury, wars etc..."
//
// This carries the standing of the character's OWN house, which for a companion IS the player's house. That is
// what makes the composition honest in both directions at once: the companion learns the holdings because they
// are his own house's, and the enemy lord across the table learns his, not yours.
//
// DERIVED, NEVER STORED. This is filled at conversation time from the live campaign and thrown away with the
// encounter. A persisted copy would be a second world model beside Bannerlord's, and this project already pays
// for two of those: the two regard ledgers that diverge, and romantic profiles generated once so that later
// rule changes never reach existing heroes.
//
// TREASURY IS DELIBERATELY ABSENT from this first slice. A companion who knows the player's gold to the denar
// will haggle with it ("you can afford it"), so it wants a band rather than a number, and that is a design
// ruling rather than a technical one. Named here so its absence reads as a decision.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>The standing of one house, as a member of that house would carry it in their head.</summary>
    public sealed class HouseStandingFacts
    {
        /// <summary>Bounded so a great house does not spend the prompt budget listing its map.</summary>
        public const int MaxFiefsNamed = 4;

        /// <summary>
        ///   How many are named in a Compact prompt. The same bounded allowance <c>personal_bonds</c> and the
        ///   Lean witness recall use, and for the same reason: what a character costs must not grow with how
        ///   much they have. The "and N others" tail still carries the scale, so a man with nine towns never
        ///   sounds like a man with two.
        /// </summary>
        public const int MaxFiefsNamedLean = 2;

        /// <summary>Bounded for the same reason. A realm at war with everyone is one sentence, not a gazetteer.</summary>
        public const int MaxWarsNamed = 3;

        /// <summary>How many wars are named in a Compact prompt. See <see cref="MaxFiefsNamedLean" />.</summary>
        public const int MaxWarsNamedLean = 2;

        /// <summary>The house's own name, as the character would say it.</summary>
        public string? HouseName { get; init; }

        /// <summary>True when this house is the PLAYER'S, which changes how the standing is voiced, not whether.</summary>
        public bool IsPlayerHouse { get; init; }

        /// <summary>
        ///   The towns, castles and villages the house holds. Supplied whole; the pack itself does the trimming,
        ///   so the bound lives in one place and the host is not asked to remember it.
        /// </summary>
        public IReadOnlyList<string> Fiefs { get; init; } = new List<string>();

        /// <summary>The realms the house is at war with right now, likewise supplied whole and trimmed here.</summary>
        public IReadOnlyList<string> AtWarWith { get; init; } = new List<string>();
    }
}

// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 5: what the house can actually bring to bear.
//
// The remainder of fkasad's report, and the last part of it still unanswered. His words, in full: "I feel like
// companions should have a better understanding of the clan current status, territories, armies, treasury,
// wars etc". house_standing answered territories and wars on 2026-09-11. This answers ARMIES and TREASURY.
//
// Why it is its own pack rather than four more fields on house_standing, which carries the identical bearer
// rule today: the two come apart the moment knowledge acquires a DATE, which is Gabriel's own observation
// ("un prisonnier sait ce qui s'est passé avant son emprisonnement"). A man in a cell still knows where his
// family's lands are - that does not change from one month to the next - and has no idea what is in the
// coffers this morning or who is in the field. Standing is durable; means are today's. Keeping them apart now
// costs nothing and means the prisoner rule, when it comes, is one pack changing rather than a field-by-field
// audit of a class that mixes both.
//
// BANDS, NOT NUMBERS, as with seat_standing (see SeatStandingFacts for the transcript that settled it): a lord
// knows his house is short of coin without knowing the figure, and the figure is the thing a player can check.

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>The coffers, ranked against other houses. Wealth has no ceiling, so it is said as a comparison.</summary>
    public enum TreasuryBand
    {
        Unknown,
        Empty,
        Strained,
        Sound,
        Deep
    }

    /// <summary>What the house has under arms, ranked the same way.</summary>
    public enum MusterBand
    {
        Unknown,

        /// <summary>Nobody in the field at all. A real and speakable state, not a missing reading.</summary>
        Nothing,
        Small,
        Respectable,
        Formidable
    }

    /// <summary>How far the house's voice carries at its own court. Meaningless outside a realm.</summary>
    public enum InfluenceBand
    {
        Unknown,
        Negligible,
        Modest,
        Considerable,
        Commanding
    }

    /// <summary>
    ///   What the character's own house can bring to bear TODAY: coin, men, and the weight of its word at
    ///   court. Read live and thrown away with the encounter, like every pack.
    /// </summary>
    public sealed class HouseMeansFacts
    {
        /// <summary>The coffers.</summary>
        public TreasuryBand Treasury { get; init; } = TreasuryBand.Unknown;

        /// <summary>
        ///   The house owes the crown and has not repaid it. A fact a lord would feel rather than look up, and
        ///   one the engine keeps explicitly (<c>Clan.DebtToKingdom</c>) rather than one inferred from a
        ///   shortfall.
        /// </summary>
        public bool OwesTheCrown { get; init; }

        /// <summary>What is under arms.</summary>
        public MusterBand Muster { get; init; } = MusterBand.Unknown;

        /// <summary>
        ///   At least one of the house's parties rides with an army right now. The difference between men on
        ///   the map and men on campaign, which is the difference a lord would actually speak about.
        /// </summary>
        public bool RidingWithAnArmy { get; init; }

        /// <summary>How much the house's word is worth at court.</summary>
        public InfluenceBand Influence { get; init; } = InfluenceBand.Unknown;

        /// <summary>True when anything at all was read. An all-unknown record means nobody asked the engine.</summary>
        public bool HasAnyReading
            => Treasury != TreasuryBand.Unknown
               || Muster != MusterBand.Unknown
               || Influence != InfluenceBand.Unknown
               || OwesTheCrown || RidingWithAnArmy;
    }
}

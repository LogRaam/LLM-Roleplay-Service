// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 4: what a person knows about the place they answer for.
//
// This pack has the best specification any of them has had, because the model wrote it. On 2026-09-12 the first
// live cr.memory_bench run passed all six probes, and the two most interesting inventions were inside PASSES.
// Handed nothing but two fief names, the character volunteered:
//
//   "Pravend is the larger, with decent trade flowing through it, though the roads have grown less certain
//    since the wars began."
//   "The autumn harvest came in tolerably... We lost some grain to rot in the northern fields, but the granaries
//    hold sufficient stores to see us through winter... The garrison is at strength."
//
// Not one of those was in her prompt. So the ignorance frontier is not a wall, it is a slope: it holds where a
// character has no business knowing (the player's purse, a battle at a ford - both refused cleanly), and gives
// way exactly where he obviously WOULD know and was simply never told. Structural ignorance prevents divulging;
// it does not prevent filling in.
//
// Every field below is one of those invented sentences, turned into a fact the host must actually read. The
// stakes are higher than an ordinary invention because the game HAS these numbers - WorldConditionsFeeder
// already reads food and prosperity per town - so a player can open the town screen and catch the lie.
//
// BANDS, NOT NUMBERS (Gabriel was asleep; assumption stated so it is cheap to reverse). A lord knows how his
// seat FARES, not its ledger: he has never counted the granary. A number also invites the model to do
// arithmetic with it, and the prose never needed the digits.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>How this character answers for the place: it decides what they may reasonably know of it.</summary>
    public enum SeatRole
    {
        Unknown,

        /// <summary>They govern it day to day for its owner. The closest knowledge there is.</summary>
        Governor,

        /// <summary>They hold it. They know how it fares without living in it.</summary>
        Owner,

        /// <summary>A headman, merchant or artisan of the place. Their whole world, seen from below.</summary>
        Notable
    }

    /// <summary>Town, castle or village. A castle has no market and a village has no garrison worth the name.</summary>
    public enum SettlementKind
    {
        Unknown,
        Town,
        Castle,
        Village
    }

    /// <summary>
    ///   How the place is doing, relative to others of its kind in Calradia. Relative on purpose: prosperity has
    ///   no natural ceiling to measure against, and a threshold invented here would be a threshold nobody could
    ///   defend. A lord compares his town with other towns, which is also how he would say it.
    /// </summary>
    public enum ProsperityBand
    {
        Unknown,
        Poor,
        Middling,
        Comfortable,
        Rich
    }

    /// <summary>The granary, as a share of what the place can hold. The engine supplies the ceiling itself.</summary>
    public enum FoodBand
    {
        Unknown,
        Empty,
        Thin,
        Adequate,
        Full
    }

    /// <summary>Which way it is going, which is half of what a steward actually reports.</summary>
    public enum FoodTrend
    {
        Unknown,
        Falling,
        Steady,
        Rising
    }

    /// <summary>The garrison as a share of what the place can hold, again on the engine's own limit.</summary>
    public enum GarrisonBand
    {
        Unknown,
        Skeleton,
        UnderStrength,
        Adequate,
        Strong
    }

    /// <summary>The people's temper, on the engine's own 0-100 loyalty scale.</summary>
    public enum LoyaltyBand
    {
        Unknown,
        Rebellious,
        Restless,
        Content,
        Devoted
    }

    /// <summary>Whether the roads and the streets are safe, on the engine's own 0-100 security scale.</summary>
    public enum SecurityBand
    {
        Unknown,
        Lawless,
        Uneasy,
        Settled
    }

    /// <summary>
    ///   One place, as the person who answers for it knows it. Every band is a read of a live engine value; not
    ///   one of them is stored, and an unread value stays <c>Unknown</c> so that nothing is said rather than
    ///   something being guessed.
    /// </summary>
    public sealed class SeatFacts
    {
        /// <summary>The place's name.</summary>
        public string? PlaceName { get; init; }

        /// <summary>How they answer for it.</summary>
        public SeatRole Role { get; init; } = SeatRole.Unknown;

        /// <summary>Town, castle or village.</summary>
        public SettlementKind Kind { get; init; } = SettlementKind.Unknown;

        /// <summary>True when the place belongs to the player's own house.</summary>
        public bool IsPlayerHolding { get; init; }

        /// <summary>How it fares against other places of its kind.</summary>
        public ProsperityBand Prosperity { get; init; } = ProsperityBand.Unknown;

        /// <summary>What is in the granary.</summary>
        public FoodBand FoodStores { get; init; } = FoodBand.Unknown;

        /// <summary>Which way the granary is going.</summary>
        public FoodTrend FoodDirection { get; init; } = FoodTrend.Unknown;

        /// <summary>How the garrison stands. Meaningless for a village, which keeps none.</summary>
        public GarrisonBand Garrison { get; init; } = GarrisonBand.Unknown;

        /// <summary>The people's temper.</summary>
        public LoyaltyBand Loyalty { get; init; } = LoyaltyBand.Unknown;

        /// <summary>Whether the country around it is safe.</summary>
        public SecurityBand Security { get; init; } = SecurityBand.Unknown;

        /// <summary>An army is at the walls right now. Nothing else said about the place matters as much.</summary>
        public bool IsUnderSiege { get; init; }

        /// <summary>Raiders are burning it right now.</summary>
        public bool IsBeingRaided { get; init; }

        /// <summary>It has been sacked and has not recovered. The state the escort quest's village is in.</summary>
        public bool IsLooted { get; init; }

        /// <summary>The people have risen. The engine's own reading, not one inferred from loyalty.</summary>
        public bool IsInRebellion { get; init; }

        /// <summary>People are going hungry, on the engine's own flag rather than a threshold guessed here.</summary>
        public bool IsStarving { get; init; }

        /// <summary>True when anything at all is known about this place beyond its name.</summary>
        public bool HasAnyReading
            => Prosperity != ProsperityBand.Unknown
               || FoodStores != FoodBand.Unknown
               || Garrison != GarrisonBand.Unknown
               || Loyalty != LoyaltyBand.Unknown
               || Security != SecurityBand.Unknown
               || IsUnderSiege || IsBeingRaided || IsLooted || IsInRebellion || IsStarving;
    }

    /// <summary>
    ///   The places this character answers for. A great lord may hold a dozen; <see cref="MaxSeatsDescribed" />
    ///   of them are described in full and the rest are already named by <c>house_standing</c>, so nothing is
    ///   lost and the prompt does not grow with the map.
    /// </summary>
    public sealed class SeatStandingFacts
    {
        /// <summary>
        ///   How many places get a full description. Two, because the seat a governor keeps and the seat he was
        ///   born to is the realistic span of what anyone speaks about unprompted, and because this pack is read
        ///   in every conversation with a landed character.
        /// </summary>
        public const int MaxSeatsDescribed = 2;

        /// <summary>The places, most closely held first: what they govern before what they merely own.</summary>
        public IReadOnlyList<SeatFacts> Seats { get; init; } = new List<SeatFacts>();
    }
}

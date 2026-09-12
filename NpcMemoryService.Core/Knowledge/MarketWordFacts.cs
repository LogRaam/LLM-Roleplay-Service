// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 8: what somebody who trades for a living knows about their own market.
//
// Gabriel, 2026-09-12: "on va aussi modéliser le commerce pour les marchands." The audit had already named it
// as the one category nobody had modelled at all, and a grep confirms it: EncounterContext returns nothing for
// trade, price, market, goods or caravan - the only match is CompanionAskingPrice, which is a hiring fee.
//
// So a town merchant, whose entire life is what things cost and where they come from, had no more idea of it
// than a passing soldier. Ask him what is dear this season and he invents - the same slope seat_standing was
// built for, in the one profession where it is the whole conversation.
//
// WHAT THIS DELIBERATELY DOES NOT CARRY, because a merchant notable already gets it from seat_standing: how
// the town prospers, whether the roads are safe, the granary, the temper of the people. Those reach him
// already. Repeating them here would be a second source for one fact, which is how two sources come to
// disagree - the rule that closed the "what the player is carrying" candidate without a line being written.
//
// PRICES ARE BANDS, like everything else, and here the reason is sharper than usual: a denar figure is exactly
// what a player can check by walking into the same market, and it is also the number the model would most
// enjoy doing arithmetic with.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What the market says about one good, against what that good is normally worth. The engine supplies
    ///   both halves - the local price and the item's own base value - so no threshold here is invented.
    /// </summary>
    public enum PriceBand
    {
        Unknown,

        /// <summary>Going for less than it is worth. Somebody is selling and few are buying.</summary>
        Cheap,

        /// <summary>About what it is worth anywhere.</summary>
        Ordinary,

        /// <summary>Dear. Either it is scarce here or somebody wants it badly.</summary>
        Dear
    }

    /// <summary>One good and what it is doing in this market.</summary>
    public sealed class MarketGood
    {
        /// <summary>The good, as a person would name it: "grain", "iron", "hides".</summary>
        public string? Name { get; init; }

        /// <summary>What it is doing against its usual worth.</summary>
        public PriceBand Price { get; init; } = PriceBand.Unknown;
    }

    /// <summary>
    ///   What a trader knows of their own market: what the place makes, and what is dear or cheap in it right
    ///   now. Read live and thrown away with the encounter, like every pack.
    /// </summary>
    public sealed class MarketWordFacts
    {
        /// <summary>How many goods are named. A trader talks about what stands out, not about a ledger.</summary>
        public const int MaxGoodsNamed = 3;

        /// <summary>The market they answer for.</summary>
        public string? PlaceName { get; init; }

        /// <summary>
        ///   What this place MAKES - a village's own production, a town's workshops. The most durable trade
        ///   fact there is, and the one a merchant would state without thinking.
        /// </summary>
        public IReadOnlyList<string> Produces { get; init; } = new List<string>();

        /// <summary>What the market is doing, worth naming first.</summary>
        public IReadOnlyList<MarketGood> Goods { get; init; } = new List<MarketGood>();

        /// <summary>True when anything at all was read.</summary>
        public bool HasAnyReading => Produces.Count > 0 || Goods.Count > 0;
    }
}

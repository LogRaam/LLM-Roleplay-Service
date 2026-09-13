// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 12: the ventures somebody owns that earn while they are elsewhere.
//
// The last of the gaps the role audit named. Gabriel raised caravans unprompted - "les caravanes, etc." - and
// checking found Hero.OwnedCaravans and Hero.OwnedWorkshops returning nothing to the context at all.
//
// WHY IT IS NOT market_word, which was the obvious place to put it. A trader's market is where he stands; his
// caravans are where he is NOT. And the carry rules genuinely differ: a lord who owns three caravans is not a
// merchant and must not be handed the price of grain, while a merchant with no caravan has a market and no
// ventures. Two rules that refuse different people are two packs.
//
// NUMBERS ARE ALLOWED HERE, and it is worth saying why when every other pack bands them. A man knows how many
// caravans he owns the way he knows how many children he has: it is two or three, he has not counted, he
// simply knows. The rule was never "never be precise" - it was "never invent what you have not counted", and
// house_standing has named fiefs exactly since the day it shipped.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>One workshop somebody owns, as its owner would name it.</summary>
    public sealed class OwnedWorkshop
    {
        /// <summary>What it makes - a smithy, a tannery, a brewery.</summary>
        public string? Trade { get; init; }

        /// <summary>The town it stands in.</summary>
        public string? PlaceName { get; init; }
    }

    /// <summary>
    ///   What this character owns that earns for them elsewhere: caravans on the road, workshops in towns.
    /// </summary>
    public sealed class OwnVenturesFacts
    {
        /// <summary>How many workshops are named before the rest become "and two others".</summary>
        public const int MaxWorkshopsNamed = 3;

        /// <summary>How many caravans they have on the road. Small, exact, and a thing anybody would know.</summary>
        public int Caravans { get; init; }

        /// <summary>The workshops they hold.</summary>
        public IReadOnlyList<OwnedWorkshop> Workshops { get; init; } = new List<OwnedWorkshop>();

        /// <summary>True when they own anything at all.</summary>
        public bool HasAnyReading => Caravans > 0 || Workshops.Count > 0;
    }
}

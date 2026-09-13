// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 11: what the people riding with you know about the company they are in.
//
// THE PATTERN THIS CLOSES, found by the role audit Gabriel asked for. EncounterContext carries three OFFER
// gates - NpcCanBeAppointedGovernor, NpcCanBeAssignedPartyRole, NpcCanBeDispatchedOnMission - and exactly ONE
// of them had a matching "post actually held", added the day fkasad reported that his governor did not know he
// governed.
//
//     The context knew whether a post could be OFFERED. It mostly did not know whether one was HELD.
//
// So the mod appoints a companion QUARTERMASTER through its own verb and never tells him, while the engine has
// kept EffectiveQuartermaster, EffectiveScout, EffectiveSurgeon and EffectiveEngineer all along. That is
// fkasad's bug in a second place, and one the mod created for itself.
//
// WHY THE POST ALONE WOULD HAVE BEEN HALF A FIX, and this is the granary lesson applied before it could bite
// again: telling a man he is quartermaster and nothing else does not stop him inventing the stores, it gives
// him a REASON to. So the post goes in station (a rank held, never news) and what the post KNOWS goes here.
//
// Bands, and for once one of them is close to a real number the engine keeps: GetNumDaysForFoodToLast. Even
// there it is banded, because "eleven days" is a figure a player can check and a quartermaster would say "a
// week or so" anyway.

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>The post somebody holds in the party they ride with. None for an ordinary member.</summary>
    public enum PartyPost
    {
        None,

        /// <summary>Answers for the provisions and the pay. Knows the stores better than anyone.</summary>
        Quartermaster,

        /// <summary>Answers for the road ahead and what is on it.</summary>
        Scout,

        /// <summary>Answers for the wounded.</summary>
        Surgeon,

        /// <summary>Answers for the engines and the walls.</summary>
        Engineer
    }

    /// <summary>How long the food will last, on the engine's own reckoning of days.</summary>
    public enum ProvisionBand
    {
        Unknown,

        /// <summary>Nothing left. People are going hungry today.</summary>
        Starving,

        /// <summary>Days, not weeks. Somebody should be worried.</summary>
        Short,

        /// <summary>Enough to march on without thinking about it.</summary>
        Sufficient,

        /// <summary>Well found. The waggons are heavy.</summary>
        Plentiful
    }

    /// <summary>The temper of the camp, on the engine's own 0-100 morale.</summary>
    public enum CampMood
    {
        Unknown,
        Mutinous,
        Sullen,
        Steady,
        HighHearted
    }

    /// <summary>How much of the company is fit to fight.</summary>
    public enum WoundedBand
    {
        Unknown,

        /// <summary>Nobody worth mentioning.</summary>
        Few,

        /// <summary>A real number of them, and it shows.</summary>
        Many,

        /// <summary>More hurt than whole. This company should not be fighting.</summary>
        Most
    }

    /// <summary>
    ///   What somebody riding in the player's company knows about it. Everyone in a hungry camp knows the camp
    ///   is hungry; the quartermaster knows how many days are left.
    /// </summary>
    public sealed class PartyStandingFacts
    {
        /// <summary>How long the food holds.</summary>
        public ProvisionBand Provisions { get; init; } = ProvisionBand.Unknown;

        /// <summary>The temper of the camp.</summary>
        public CampMood Mood { get; init; } = CampMood.Unknown;

        /// <summary>How many are hurt.</summary>
        public WoundedBand Wounded { get; init; } = WoundedBand.Unknown;

        /// <summary>True when anything at all was read.</summary>
        public bool HasAnyReading
            => Provisions != ProvisionBand.Unknown
               || Mood != CampMood.Unknown
               || Wounded != WoundedBand.Unknown;
    }
}

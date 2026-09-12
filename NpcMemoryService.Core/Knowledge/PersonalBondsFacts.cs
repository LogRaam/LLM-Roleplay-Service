// Code written by Gabriel Mailhot, 11/09/2026.
// Knowledge composition, increment 2: the people in a character's life who are not the player.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>How a character stands with one named person. Deliberately coarse: a bond is a feeling with a
    /// name on it, not a relationship model.</summary>
    public enum BondKind
    {
        /// <summary>Blood or marriage.</summary>
        Kin,

        /// <summary>Someone they are fond of and would speak well of.</summary>
        Friend,

        /// <summary>Someone they measure themselves against, without hatred.</summary>
        Rival,

        /// <summary>Someone they have a real grievance with.</summary>
        Enemy,

        /// <summary>Someone they love, or have loved.</summary>
        Beloved,

        /// <summary>A love that ended, which is a different thing to carry than one that did not.</summary>
        LostLove
    }

    /// <summary>One person who matters to this character, and how much of it they would say.</summary>
    public sealed class PersonalBond
    {
        /// <summary>The other person, by the name this character would use for them.</summary>
        public string? PersonName { get; init; }

        /// <summary>What they are to each other.</summary>
        public BondKind Kind { get; init; }

        /// <summary>
        ///   One short clause of WHY, in plain words: "who left him for a Vlandian", "who took his brother at
        ///   Pravend". A bond with no reason behind it gives a character a name to drop and nothing to feel.
        /// </summary>
        public string? Note { get; init; }

        /// <summary>
        ///   How freely they would speak of it. A feud is usually <see cref="Discretion.Open" />; a love affair
        ///   is <see cref="Discretion.Guarded" /> at best and often <see cref="Discretion.Secret" />, which the
        ///   pack never renders at all.
        /// </summary>
        public Discretion Discretion { get; init; } = Discretion.Guarded;
    }

    /// <summary>
    ///   The handful of people who matter to this character, other than the player.
    ///   <para>
    ///     BOUNDED ON PURPOSE. A character who can discuss everyone's affairs becomes a gossip engine, and CR
    ///     already has a rumour path that would then be doubled. These are the few people he would actually
    ///     bring up, not a directory of everyone he has met.
    ///   </para>
    /// </summary>
    public sealed class PersonalBondsFacts
    {
        /// <summary>At most this many are named, however many the simulation could supply.</summary>
        public const int MaxNamed = 3;

        /// <summary>Supplied whole; the pack trims and filters, so the bound and the discretion live in one place.</summary>
        public IReadOnlyList<PersonalBond> Bonds { get; init; } = new List<PersonalBond>();
    }
}

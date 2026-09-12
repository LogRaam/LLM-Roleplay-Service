// Code written by Gabriel Mailhot, 11/09/2026.
// Increment 1 of the knowledge-composition pillar (ROADMAP: "WHAT A CHARACTER KNOWS, declared once and
// auditable"). What a person knows is decided by TWO axes, not one, and the design entry argues why naming only
// the first would bake in the wrong model:
//
//   WHAT THEY ARE decides which KINDS of knowledge exist for them at all. A merchant has trade knowledge; a
//   landless wanderer has neither a treasury nor a garrison to know about.
//
//   WHAT THEY ARE TO THE PLAYER decides WHOSE facts they may hold. "Lord" says nothing about whether he knows
//   your finances: an enemy lord knows his own house's and not yours, your companion knows yours because yours
//   IS his. Composing on role alone would hand every stranger the player's ledger while fixing one companion.

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   Who this character is, and what they are to the player: everything the composition rule is allowed to
    ///   weigh when deciding which knowledge they carry. Plain facts, no engine types, so the rule stays pure
    ///   and a test can state a person without a campaign running.
    /// </summary>
    public sealed class KnowledgeBearer
    {
        /// <summary>A lord or lady: someone with a house, a standing, and business at court.</summary>
        public bool IsLord { get; init; }

        /// <summary>One of the player's own companions. Their house IS the player's house.</summary>
        public bool IsPlayerCompanion { get; init; }

        /// <summary>Of the player's clan by blood or oath, which is the other way the same thing can be true.</summary>
        public bool IsOfPlayerClan { get; init; }

        /// <summary>
        ///   A settlement notable: a headman, a merchant, an artisan, a gang leader. They know their town and
        ///   its trade; they do not sit at anybody's council.
        /// </summary>
        public bool IsNotable { get; init; }

        /// <summary>They belong to some house at all. A landless wanderer has no standing to know about.</summary>
        public bool HasHouse { get; init; }

        /// <summary>
        ///   They answer for a place: they hold it, they govern it for whoever does, or they are a notable of
        ///   the place they live in. Separate from
        ///   <see cref="IsLord" /> because the two come apart in both directions - a landless lord holds
        ///   nothing, and a companion who governs a town is no lord at all but knows that town better than
        ///   anyone. fkasad's governor was exactly the second case.
        /// </summary>
        public bool HoldsASeat { get; init; }

        /// <summary>
        ///   No news reaches them. A prisoner in a cell is the case the game already models as cut off.
        ///   <para>
        ///     Gabriel ruled on 2026-09-11 that knowledge is CURRENT, and gave the reason from the world
        ///     rather than from the code: "considerant qu'il y a un systeme de courrier, je pencherais pour
        ///     que le Seigneur soit au courant de ce qui se passe". This is the same reasoning run backwards,
        ///     and he ruled it in on 2026-09-12: a man who receives no couriers hears nothing.
        ///   </para>
        ///   <para>
        ///     What it does NOT do is make knowledge STALE. A captive does not carry a months-old copy of his
        ///     house's ledger - he does not carry it at all. That keeps staleness out of the design entirely,
        ///     which was the whole point of the current-knowledge ruling, and it means no pack anywhere needs
        ///     a freshness axis.
        ///   </para>
        /// </summary>
        public bool IsCutOff { get; init; }

        /// <summary>
        ///   Trade is how they eat: a merchant, an artisan, a caravan master. Deliberately narrower than
        ///   <see cref="IsNotable" />, which also covers headmen and preachers, and narrower than "has money":
        ///   a lord knows his lands make wine and has no idea what wine fetched this week, and knowing that is
        ///   exactly what separates a merchant from a man who buys things.
        /// </summary>
        public bool TradesForALiving { get; init; }

        /// <summary>
        ///   True when this character's own house is the PLAYER'S house, which is what makes a companion's
        ///   knowledge of the player's holdings ordinary rather than a leak.
        /// </summary>
        public bool OwnHouseIsPlayerHouse => IsPlayerCompanion || IsOfPlayerClan;
    }
}

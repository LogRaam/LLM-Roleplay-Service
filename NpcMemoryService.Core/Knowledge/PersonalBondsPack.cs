// Code written by Gabriel Mailhot, 11/09/2026.
// Knowledge composition, increment 2. The category Gabriel asked for, as ONE pack rather than the two he
// proposed ("mes histoires de coeur" and "mes rancunes"): from the character's own side, love, loss, kinship,
// rivalry and enmity are the same kind of knowing - how I stand with named people who are not you. Two packs
// would render twice and pay the budget twice for one composition rule.
//
// What genuinely differs between a feud and an affair is not whether he KNOWS it. It is whether he would SAY
// it, which is why increment 2 introduced Discretion rather than a second pack.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>The people in this character's life other than the player, and what they would say of them.</summary>
    public sealed class PersonalBondsPack : KnowledgePack
    {
        public override string Name => "personal_bonds";

        public override string Covers =>
            "The few people this character carries with them who are not the player: kin, friends, rivals, "
            + "enemies, and the ones they have loved. Everyone has these; what varies is whom, and how much of "
            + "it they would say to the person in front of them.";

        /// <summary>
        ///   Everyone has a life. The interesting rule for this pack is not WHO carries it - that is all of
        ///   them - but what survives <see cref="DiscretionPolicy" />, and saying so plainly is better than
        ///   inventing a condition to make the rule look substantial.
        /// </summary>
        /// <summary>Everyone has their own people, and nobody knows them better than they do.</summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
            => bearer == null ? KnowledgeDepth.None : KnowledgeDepth.Deep;

        public override bool IsSupplied(EncounterContext context)
            => context?.PersonalBonds?.Bonds != null && context.PersonalBonds.Bonds.Count > 0;

        /// <summary>
        ///   A life can be legitimately empty here. The live sweep's first run reported four lords as "carried
        ///   but not supplied" simply because none of them had a living feud with anyone or a lover, and calling
        ///   that a missing fact would train everyone to ignore the report. Contrast house_standing, where an
        ///   empty pack means somebody failed to read the engine.
        /// </summary>
        public override bool RequiresContent => false;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            PersonalBondsFacts? facts = context?.PersonalBonds;
            if (facts?.Bonds == null) return;

            // Supplied by the host, because who is listening is a fact of the encounter and not of the pack.
            // Null is read by DiscretionPolicy as a stranger in a crowded room, which refuses everything but the
            // open: guessing wrong in this one place costs a secret.
            ListeningAudience? audience = context!.Audience;

            List<PersonalBond> tellable = facts.Bonds
                                               .Where(b => b != null && !string.IsNullOrWhiteSpace(b.PersonName))
                                               .Where(b => DiscretionPolicy.WouldSay(b.Discretion, audience))
                                               .Take(lean ? PersonalBondsFacts.MaxNamedLean : PersonalBondsFacts.MaxNamed)
                                               .ToList();

            if (tellable.Count == 0) return;

            // Compact gets a bounded allowance and a bare label, the same shape the Lean witness recall was
            // given for the same reason: cost must not grow with how full a character's life is. Measured
            // 2026-09-12, when the budget guard was first handed a character who actually knows things.
            sb.AppendLine(lean
                              ? "People you carry:"
                              : "PEOPLE YOU CARRY WITH YOU (not the player, and yours to raise or not):");

            foreach (PersonalBond bond in tellable)
                sb.AppendLine("- " + Sentence(bond));

            if (lean) return;

            sb.AppendLine("These are your own affairs, and old ground to you. Bring one up when it bears on what "
                          + "is being said, the way anyone mentions their own people; never recite them, and "
                          + "never speak of them as though you had only just remembered they existed.");
        }

        #region private

        private static string Sentence(PersonalBond bond)
        {
            string name = bond.PersonName!.Trim();
            string note = string.IsNullOrWhiteSpace(bond.Note) ? "" : ", " + bond.Note!.Trim();

            switch (bond.Kind)
            {
                case BondKind.Kin: return $"{name} is your own blood{note}.";
                case BondKind.Friend: return $"{name} is a friend{note}.";
                case BondKind.Rival: return $"{name} is a rival of yours{note}.";
                case BondKind.Enemy: return $"You hold a grievance against {name}{note}.";
                case BondKind.Beloved: return $"You love {name}{note}.";
                case BondKind.LostLove: return $"You loved {name} once{note}.";
                default: return $"{name} matters to you{note}.";
            }
        }

        #endregion
    }
}

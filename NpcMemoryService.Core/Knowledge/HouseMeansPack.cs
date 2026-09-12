// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 5. See HouseMeansFacts for fkasad's report and for why means live apart
// from standing.

#region

using System.Collections.Generic;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What the character's own house can bring to bear: coin, men under arms, and weight at court. In
    ///   bands, never in figures.
    /// </summary>
    public sealed class HouseMeansPack : KnowledgePack
    {
        public override string Name => "house_means";

        public override string Covers =>
            "What the character's own house can bring to bear today: the state of its coffers, whether it owes "
            + "the crown, what it has under arms and whether those men are on campaign, and how far its word "
            + "carries at court. The half of fkasad's report - 'armies, treasury' - that house_standing did not "
            + "answer.";

        /// <summary>
        ///   The same rule as <c>house_standing</c> today, and deliberately written out rather than shared: a
        ///   member of a house knows what that house is worth, and for a companion the house IS the player's,
        ///   which is what makes fkasad's ask ordinary rather than a leak. The two rules are expected to
        ///   diverge when knowledge acquires a date, because a captive keeps his house's lands and loses its
        ///   ledger.
        /// </summary>
        public override bool CarriedBy(KnowledgeBearer bearer)
            => bearer != null && bearer.HasHouse && (bearer.IsLord || bearer.OwnHouseIsPlayerHouse);

        /// <summary>A house always has coffers and always has a muster, so an empty reading is a fault.</summary>
        public override bool IsSupplied(EncounterContext context)
            => context?.HouseMeans != null && context.HouseMeans.HasAnyReading;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            HouseMeansFacts? means = context?.HouseMeans;
            if (means == null || !means.HasAnyReading) return;

            var clauses = new List<string>();

            AddIf(clauses, Treasury(means.Treasury));
            if (means.OwesTheCrown) clauses.Add("it owes the crown a debt it has not repaid");
            AddIf(clauses, Muster(means.Muster, means.RidingWithAnArmy));

            // Court standing is the first thing Lean drops: it changes how a lord SPEAKS of the realm rather
            // than what he can do, and Compact has no room for register.
            if (!lean) AddIf(clauses, Influence(means.Influence));

            if (clauses.Count == 0) return;

            string preamble = lean
                ? "Your house:"
                : "Your own house's means, as you would know them without asking anyone:";

            sb.AppendLine($"{preamble} {Sentence(clauses)}");

            if (lean) return;

            sb.AppendLine("Speak of your house's coin and its strength the way anyone speaks of their own "
                          + "household's: you know how it stands, not what it tallies. Never name a sum of "
                          + "money, a count of men, or a figure of influence - you have not counted them, and "
                          + "a number you invent is one the player can check. If pressed, say you would have "
                          + "to ask whoever keeps the books.");
        }

        #region private

        private static void AddIf(List<string> clauses, string? clause)
        {
            if (!string.IsNullOrEmpty(clause)) clauses.Add(clause!);
        }

        private static string? Treasury(TreasuryBand band)
        {
            switch (band)
            {
                case TreasuryBand.Empty: return "its coffers are all but empty";
                case TreasuryBand.Strained: return "its coffers are strained";
                case TreasuryBand.Sound: return "its coffers are sound";
                case TreasuryBand.Deep: return "its coffers are deep, among the richest houses in Calradia";
                default: return null;
            }
        }

        private static string? Muster(MusterBand band, bool ridingWithAnArmy)
        {
            string men;

            switch (band)
            {
                case MusterBand.Nothing:
                    // Said plainly rather than left out: a lord with nobody in the field is a lord who cannot
                    // promise you soldiers, and that is exactly the promise the mod must not let him make.
                    return "it has no one in the field at all";
                case MusterBand.Small:
                    men = "it keeps only a small force in the field";
                    break;
                case MusterBand.Respectable:
                    men = "it keeps a respectable force in the field";
                    break;
                case MusterBand.Formidable:
                    men = "it fields one of the stronger musters in Calradia";
                    break;
                default: return null;
            }

            return ridingWithAnArmy ? $"{men}, and those men ride with an army as you speak" : men;
        }

        private static string? Influence(InfluenceBand band)
        {
            switch (band)
            {
                case InfluenceBand.Negligible: return "and at court its word counts for very little";
                case InfluenceBand.Modest: return "and at court its word carries modestly";
                case InfluenceBand.Considerable: return "and at court its word carries real weight";
                case InfluenceBand.Commanding: return "and at court few voices carry further";
                default: return null;
            }
        }

        /// <summary>The clauses as one sentence. Influence already carries its own "and", so it is not doubled.</summary>
        private static string Sentence(List<string> clauses)
        {
            if (clauses.Count == 1) return $"{clauses[0]}.";

            string last = clauses[clauses.Count - 1];
            string head = string.Join(", ", clauses.GetRange(0, clauses.Count - 1));

            return last.StartsWith("and ") ? $"{head}, {last}." : $"{head} and {last}.";
        }

        #endregion
    }
}

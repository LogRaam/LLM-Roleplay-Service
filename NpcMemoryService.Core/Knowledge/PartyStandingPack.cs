// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 11. See PartyStandingFacts for the offered-versus-held pattern this closes.

#region

using System.Collections.Generic;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What somebody riding in the player's company knows about it: the provisions, the temper of the camp,
    ///   and how many are hurt. Deeper for whoever answers for that particular thing.
    /// </summary>
    public sealed class PartyStandingPack : KnowledgePack
    {
        public override string Name => "party_standing";

        public override string Covers =>
            "How the player's own company is faring, to the people riding in it: how long the food holds, the "
            + "temper of the camp, and how many are hurt. Everyone who marches with it knows whether it is "
            + "hungry or sullen; whoever holds the post that answers for a thing knows that thing more closely.";

        /// <summary>The player's own company, and it decides what its people may promise him. Also the most
        ///   leak-sensitive content here, so it is never worth trading away for atmosphere.</summary>
        public override int DropPriority => 15;

        /// <summary>
        ///   Only the people actually in the company. A lord met on the road has no idea how the player's
        ///   waggons are stocked, and telling him would be the leak this pillar spends its time preventing.
        ///   <para>
        ///     Depth is the post: the quartermaster and the surgeon answer for provisions and wounded and know
        ///     them closely; everyone else knows what anybody in a camp knows.
        ///   </para>
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
        {
            if (bearer is not {RidesWithThePlayer: true}) return KnowledgeDepth.None;

            return bearer.PartyPost == PartyPost.None ? KnowledgeDepth.Ordinary : KnowledgeDepth.Deep;
        }

        /// <summary>
        ///   A company always has a temper and always has stores, so an empty reading on somebody riding in it
        ///   means nobody read the party - the fkasad class of fault, one more time.
        /// </summary>
        public override bool IsSupplied(EncounterContext context)
            => context?.PartyStanding != null && context.PartyStanding.HasAnyReading;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            PartyStandingFacts? party = context?.PartyStanding;
            if (party == null || !party.HasAnyReading) return;

            // Checked HERE as well as by the factory, which is not usual and is deliberate for this pack
            // alone: what it renders is the state of the PLAYER'S OWN CAMP, the most leak-sensitive content in
            // the pillar. Every other pack relies on the composer having filtered by CarriedBy; this one makes
            // the guarantee local, so it survives a caller who forgets - a test asked for exactly that.
            if (!CarriedBy(context!.Bearer)) return;

            var clauses = new List<string>();

            AddIf(clauses, Provisions(party.Provisions));
            AddIf(clauses, Mood(party.Mood));
            AddIf(clauses, Wounded(party.Wounded));

            if (clauses.Count == 0) return;

            sb.AppendLine($"You ride with the player's own company, and you know how it stands: {Sentence(clauses)}");

            if (lean) return;

            // The post-holder's line. Without it, telling a man he is quartermaster gives him a reason to
            // invent the stores rather than a reason not to - the granary lesson, applied before it bit again.
            if (context!.Bearer?.PartyPost is not (null or PartyPost.None))
                sb.AppendLine("It is your post to answer for this, so speak of it as a man who checks it daily "
                              + "rather than as somebody repeating camp talk. Still no figures: you know how "
                              + "many days the food will run, not the tally of every sack.");
        }

        #region private

        private static void AddIf(List<string> clauses, string? clause)
        {
            if (!string.IsNullOrEmpty(clause)) clauses.Add(clause!);
        }

        /// <summary>
        ///   Being well found earns no clause, like clear weather and an unhurt body: this is read in every
        ///   conversation with every companion, and most of the time there is nothing wrong.
        /// </summary>
        private static string? Provisions(ProvisionBand band)
        {
            switch (band)
            {
                case ProvisionBand.Starving: return "there is nothing left to eat and people are going hungry";
                case ProvisionBand.Short: return "the provisions will not last the week";
                case ProvisionBand.Plentiful: return "the waggons are well found";
                default: return null;
            }
        }

        private static string? Mood(CampMood mood)
        {
            switch (mood)
            {
                case CampMood.Mutinous: return "the men are close to mutiny";
                case CampMood.Sullen: return "the camp is sullen";
                case CampMood.HighHearted: return "the men are in high heart";
                default: return null;
            }
        }

        private static string? Wounded(WoundedBand band)
        {
            switch (band)
            {
                case WoundedBand.Many: return "a good many of the men are carrying wounds";
                case WoundedBand.Most: return "more of the company is hurt than whole";
                default: return null;
            }
        }

        private static string Sentence(List<string> clauses)
        {
            string joined = clauses.Count == 1
                ? clauses[0]
                : string.Join(", ", clauses.GetRange(0, clauses.Count - 1)) + " and " + clauses[clauses.Count - 1];

            return char.ToUpperInvariant(joined[0]) + joined.Substring(1) + ".";
        }

        #endregion
    }
}

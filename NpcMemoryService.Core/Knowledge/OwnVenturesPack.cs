// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 12. See OwnVenturesFacts for why this is not part of market_word.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   The caravans and workshops somebody owns. Nobody knows their own ventures at second hand, so this is
    ///   Deep or nothing.
    /// </summary>
    public sealed class OwnVenturesPack : KnowledgePack
    {
        public override string Name => "own_ventures";

        public override string Covers =>
            "The caravans on the road and the workshops in towns that this character owns. Distinct from "
            + "market_word, which is the market they stand in: these are the things that earn while they are "
            + "somewhere else, and a lord may own them without knowing the price of grain.";

        /// <summary>What earns for him elsewhere. Colour in almost every conversation.</summary>
        public override int DropPriority => 60;

        /// <summary>
        ///   Whoever owns one. The host decides that, because only the host can see whether the ventures were
        ///   actually read - the one-read rule that HoldsASeat and the market both learned.
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer? bearer)
            => bearer is {OwnsVentures: true} ? KnowledgeDepth.Deep : KnowledgeDepth.None;

        public override bool IsSupplied(EncounterContext context)
            => context?.OwnVentures != null && context.OwnVentures.HasAnyReading;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            OwnVenturesFacts? mine = context?.OwnVentures;
            if (mine == null || !mine.HasAnyReading) return;

            var clauses = new List<string>();

            if (mine.Caravans == 1) clauses.Add("a caravan of your own on the roads");
            else if (mine.Caravans > 1) clauses.Add($"{mine.Caravans} caravans of your own on the roads");

            string shops = Workshops(mine.Workshops);
            if (shops.Length > 0) clauses.Add(shops);

            if (clauses.Count == 0) return;

            sb.AppendLine($"You own {Join(clauses)}. That is your own business and not news to you: you never "
                          + "hear of it as though somebody had just told you.");

            if (lean) return;

            // The invention this pack has to close is not the existence of the ventures but their FORTUNES,
            // which is the granary lesson wearing a merchant's coat: a man who knows he owns three caravans
            // and is asked how they are doing will otherwise make up a profit.
            sb.AppendLine("What they are EARNING, where each one stands today, and whether any has been taken "
                          + "on the road are things you would have to hear from your own people - so do not "
                          + "state them. Speak of the ventures themselves plainly, and of their fortunes only "
                          + "as a man who is waiting on word.");
        }

        #region private

        private static string Workshops(IReadOnlyList<OwnedWorkshop> all)
        {
            List<string> named = (all ?? new List<OwnedWorkshop>())
                                 .Where(w => w != null && !string.IsNullOrWhiteSpace(w.Trade))
                                 .Take(OwnVenturesFacts.MaxWorkshopsNamed)
                                 .Select(Describe)
                                 .ToList();

            if (named.Count == 0) return "";

            int rest = all!.Count - named.Count;

            return rest <= 0
                ? Join(named)
                : $"{Join(named)} and {rest} other{(rest == 1 ? "" : "s")}";
        }

        private static string Describe(OwnedWorkshop shop)
        {
            string trade = shop.Trade!.Trim();
            string place = (shop.PlaceName ?? "").Trim();

            return place.Length == 0 ? $"a {trade}" : $"a {trade} in {place}";
        }

        private static string Join(List<string> parts)
            => parts.Count == 1
                ? parts[0]
                : string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[parts.Count - 1];

        #endregion
    }
}

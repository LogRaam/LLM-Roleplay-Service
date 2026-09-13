// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 8. See MarketWordFacts for the gap and for what is deliberately left to
// seat_standing.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What a trader knows of their own market: what the place makes, and what is dear or cheap in it.
    /// </summary>
    public sealed class MarketWordPack : KnowledgePack
    {
        public override string Name => "market_word";

        public override string Covers =>
            "What the place a trader lives in produces, and which goods are dear or cheap in its market right "
            + "now. Carried by the people whose living it is - merchants, artisans, caravan folk - and not by "
            + "a lord, who knows his fiefs make wine without knowing what wine fetched this week. Prosperity, "
            + "safety and the granary are NOT here: seat_standing already tells a notable those.";

        /// <summary>A trader's whole living. He carries few other packs, so this rarely competes.</summary>
        public override int DropPriority => 35;

        /// <summary>
        ///   Whose living it is. A real composition rule that can be false, unlike the packs everyone carries:
        ///   a lord knows his lands make wine and has no idea what wine fetched this week, and knowing prices
        ///   is exactly the thing that separates a merchant from a man who buys things.
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer? bearer)
            => bearer is {TradesForALiving: true} ? KnowledgeDepth.Deep : KnowledgeDepth.None;

        /// <summary>
        ///   A market always has something to say - it makes something, or something in it is dear. An empty
        ///   reading on somebody whose living this is means nobody asked the engine.
        /// </summary>
        public override bool IsSupplied(EncounterContext context)
            => context?.MarketWord != null && context.MarketWord.HasAnyReading;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            MarketWordFacts? market = context?.MarketWord;
            if (market == null || !market.HasAnyReading) return;

            string place = (market.PlaceName ?? "").Trim();
            string here = place.Length == 0 ? "here" : place;

            List<string> produces = Named(market.Produces);
            if (produces.Count > 0)
                sb.AppendLine($"{here} makes {Join(produces)}. You know that the way anyone knows their own "
                              + "trade: it is not news to you.");

            AppendPrices(sb, market, here);

            // Compact stops at the facts. A trader told that iron is dear does not need telling to care.
            if (lean) return;

            sb.AppendLine("Prices and where goods come from are your daily business, so speak of them plainly "
                          + "when they come up. But name no figure in denars: you know what is dear and what is "
                          + "going cheap, not a price list, and a number you invent is one the player can check "
                          + "by walking into the same market.");
        }

        #region private

        private static void AppendPrices(StringBuilder sb, MarketWordFacts market, string here)
        {
            List<string> dear = Named(market.Goods.Where(g => g?.Price == PriceBand.Dear).Select(g => g!.Name));
            List<string> cheap = Named(market.Goods.Where(g => g?.Price == PriceBand.Cheap).Select(g => g!.Name));

            // Ordinary prices earn no words. A market where nothing stands out is a market with no news in it,
            // and this pack is read in every conversation with every merchant in the game.
            if (dear.Count > 0) sb.AppendLine($"In {here}, {Join(dear)} {(dear.Count == 1 ? "is" : "are")} dear.");

            if (cheap.Count > 0)
                sb.AppendLine($"{Capitalise(Join(cheap))} {(cheap.Count == 1 ? "is" : "are")} going cheap.");
        }

        private static List<string> Named(IEnumerable<string?>? all)
            => (all ?? new List<string?>())
               .Where(n => !string.IsNullOrWhiteSpace(n))
               .Select(n => n!.Trim())
               .Distinct()
               .Take(MarketWordFacts.MaxGoodsNamed)
               .ToList();

        private static string Join(List<string> named)
            => named.Count == 1
                ? named[0]
                : string.Join(", ", named.Take(named.Count - 1)) + " and " + named[named.Count - 1];

        private static string Capitalise(string text)
            => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

        #endregion
    }
}

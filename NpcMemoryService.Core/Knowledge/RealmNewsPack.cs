// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 10. See RealmNewsFacts for the audit finding: the news already reached the
// prompt, and its REACH was the same four lines for a village headman and a marshal of the realm.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What word has brought this character about the wider world, in the amount their station actually
    ///   commands. The pack the depth axis was invented for.
    /// </summary>
    public sealed class RealmNewsPack : KnowledgePack
    {
        public override string Name => "realm_news";

        public override string Covers =>
            "The great happening of the realm, which everyone hears, and the lesser news that word has brought "
            + "this particular character - as much of it as somebody in their position would actually have. A "
            + "lord keeps couriers, a court, and men whose business is knowing things; a villager has the "
            + "market. Same world, different reach.";

        /// <summary>
        ///   News moves. Rendered below the encounter marker with the rest of what cannot be cached, rather
        ///   than up in the identity block where it would cost every later section its prefix.
        /// </summary>
        public override PackVolatility Volatility => PackVolatility.PerEncounter;

        /// <summary>
        ///   Everybody hears something; how much depends on what they are. A lord's word reaches furthest, a
        ///   notable hears his own region well, and an ordinary soul hears what comes through the market.
        ///   <para>
        ///     This is the composition rule the pack exists for, and the one the host used to answer with a
        ///     constant four.
        ///   </para>
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
        {
            if (bearer == null) return KnowledgeDepth.None;
            if (bearer.IsLord || bearer.OwnHouseIsPlayerHouse) return KnowledgeDepth.Knowing;
            if (bearer.IsNotable) return KnowledgeDepth.Ordinary;

            return KnowledgeDepth.Slight;
        }

        /// <summary>
        ///   Quiet times are real. A character who has heard nothing this season is not a character the host
        ///   failed to fill, so this must not be reported as a fault - the personal_bonds distinction.
        /// </summary>
        public override bool RequiresContent => false;

        public override bool IsSupplied(EncounterContext context)
            => context?.RealmNews != null && context.RealmNews.HasAnyReading;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            RealmNewsFacts? news = context?.RealmNews;
            if (news == null) return;

            KnowledgeDepth depth = DepthFor(context!.Bearer);
            if (depth == KnowledgeDepth.None) return;

            AppendHeadline(sb, news.Headline, lean);
            AppendTidings(sb, Reaching(news.Tidings, depth), lean);
        }

        #region private

        /// <summary>
        ///   How many tidings reach somebody of this standing, and from how far. Deliberately a rule about
        ///   PEOPLE rather than a constant: it replaces a hard-coded four that applied to a headman and a
        ///   marshal alike.
        /// </summary>
        private static List<Tiding> Reaching(IReadOnlyList<Tiding> all, KnowledgeDepth depth)
        {
            int room = depth switch {
                KnowledgeDepth.Slight => 1,
                KnowledgeDepth.Ordinary => 3,
                _ => 5
            };

            // A distant, unverified rumour has travelled a long way to arrive, and it arrives where people
            // keep couriers. Somebody who hears what the market hears does not get it at all - which is a
            // FEWER-FACTS cut, never a vaguer one, the rule the depth axis is held to.
            bool distantReaches = depth >= KnowledgeDepth.Knowing;

            return (all ?? new List<Tiding>())
                   .Where(t => t != null && !string.IsNullOrWhiteSpace(t.Text))
                   .Where(t => distantReaches || t.Confidence != TidingConfidence.Distant)
                   .Take(room)
                   .ToList();
        }

        /// <summary>
        ///   The great news, which reaches everyone. Kept word for word from the block it replaces, because
        ///   this prose has been tuned in play and the migration was meant to move it, not rewrite it.
        /// </summary>
        private static void AppendHeadline(StringBuilder sb, string? headline, bool lean)
        {
            if (string.IsNullOrWhiteSpace(headline)) return;

            sb.AppendLine("THE TALK OF ALL THE REALM:");
            sb.AppendLine(headline);

            if (!lean)
            {
                sb.AppendLine("This is the great news of the moment — you may bring it up as the weighty matter it is,");
                sb.AppendLine("as anyone of standing would, with your own reading of what it means.");
            }

            sb.AppendLine();
        }

        private static void AppendTidings(StringBuilder sb, List<Tiding> tidings, bool lean)
        {
            if (tidings.Count == 0) return;

            sb.AppendLine("WHAT YOU'VE HEARD (news that has reached you):");

            if (!lean)
            {
                sb.AppendLine("These are things word has brought to you: battles, sieges, marriages, deaths,");
                sb.AppendLine("wars. Mention them only if they naturally fit the conversation, as hearsay ('I");
                sb.AppendLine("heard', 'word came that'), never as a list. This is what you KNOW, not a line to");
                sb.AppendLine("read aloud: put it in your own words, as you would telling it to a neighbour, and");
                sb.AppendLine("NEVER repeat its wording word for word. Do not claim to have witnessed any of them");
                sb.AppendLine("firsthand unless your own memories say you were there. An item marked '(heard");
                sb.AppendLine("secondhand)' you hold loosely; one marked '(a distant, unverified rumour)' you");
                sb.AppendLine("repeat with real doubt, you may have the details wrong, and you say so.");
            }

            foreach (Tiding tiding in tidings) sb.AppendLine($"- {Tag(tiding.Confidence)}{tiding.Text!.TrimEnd('.', ' ')}.");

            sb.AppendLine();
        }

        private static string Tag(TidingConfidence confidence)
        {
            switch (confidence)
            {
                case TidingConfidence.Distant: return "(a distant, unverified rumour) ";
                case TidingConfidence.Secondhand: return "(heard secondhand) ";
                default: return "";
            }
        }

        #endregion
    }
}

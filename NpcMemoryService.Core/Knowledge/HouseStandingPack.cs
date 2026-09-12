// Code written by Gabriel Mailhot, 11/09/2026.
// The first knowledge pack. Chosen because it is the remainder of a real report rather than a demonstration:
// fkasad asked for companions to understand "the clan current status, territories, armies, treasury, wars".

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What a member of a house knows about that house's standing: what it holds and who it is fighting.
    /// </summary>
    public sealed class HouseStandingPack : KnowledgePack
    {
        public override string Name => "house_standing";

        public override string Covers =>
            "The lands the character's own house holds, and the realms it is at war with. For a companion or a "
            + "clan member the house IS the player's, which is why they know the player's holdings without that "
            + "being a leak; for anyone else it is their own house, and the player's affairs are none of it.";

        /// <summary>
        ///   Anyone with a house of their own knows its standing. A landless wanderer has none to know about,
        ///   and a settlement notable answers for a town rather than sitting at a house's council.
        ///   <para>
        ///     Note what this rule does NOT say: it never mentions the player. Whether the character ends up
        ///     knowing the PLAYER'S holdings falls out of whose house it is, not out of a separate permission,
        ///     which is what keeps one rule from needing an exception for every relation.
        ///   </para>
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
            => bearer != null && bearer.HasHouse && (bearer.IsLord || bearer.OwnHouseIsPlayerHouse)
                ? KnowledgeDepth.Knowing
                : KnowledgeDepth.None;

        public override bool IsSupplied(EncounterContext context)
            => context?.HouseStanding != null
               && !string.IsNullOrWhiteSpace(context.HouseStanding.HouseName);

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            HouseStandingFacts? facts = context?.HouseStanding;
            if (facts == null || string.IsNullOrWhiteSpace(facts.HouseName)) return;

            string house = facts.HouseName!.Trim();
            string whose = facts.IsPlayerHouse
                ? $"You are of the {house}, which is the player's own house."
                : $"You are of the {house}.";

            sb.AppendLine(whose);

            int fiefCap = lean ? HouseStandingFacts.MaxFiefsNamedLean : HouseStandingFacts.MaxFiefsNamed;
            List<string> fiefs = Named(facts.Fiefs, fiefCap);
            if (fiefs.Count > 0)
            {
                sb.Append($"Your house holds {Join(fiefs, facts.Fiefs.Count, fiefCap)}.");

                // Coaching, and it used to sit INSIDE this fact, so Compact paid 124 characters for it on every
                // landed conversation. Measured 2026-09-12 when the budget guard was finally given a character
                // who knows things: the five packs cost 947 characters in Compact and put the prompt 329 over
                // DuskSymphony's 16,000 ceiling. Lean carries facts; it does not carry advice about them.
                if (lean) sb.AppendLine();
                else
                    sb.AppendLine(" You know this the way anyone knows where their own family's lands are: it is "
                                  + "not news to you, and you never hear it as news.");
            }

            // A captive keeps his family's lands and loses the news. Where a house holds land does not change
            // from one month to the next, so a man in a cell still knows it; who it is fighting THIS WEEK is
            // exactly what a courier would have had to bring him. Gabriel's cut-off ruling, 2026-09-12, and
            // the same durable-versus-today's line that put house_means in its own pack.
            bool cutOff = context?.Bearer?.IsCutOff == true;

            int warCap = lean ? HouseStandingFacts.MaxWarsNamedLean : HouseStandingFacts.MaxWarsNamed;
            List<string> wars = cutOff ? new List<string>() : Named(facts.AtWarWith, warCap);
            if (wars.Count > 0)
                sb.AppendLine($"Your house is at war with {Join(wars, facts.AtWarWith.Count, warCap)}.");

            // Lean stops here. The Full prompt adds the one line that turns a list of facts into conduct, which
            // is the difference between a character who CAN answer and one who behaves as though he has lived
            // there. The 2026-09-11 Compact cut (22,500 -> 15,150 chars) is exactly what this restraint protects.
            if (lean) return;

            sb.AppendLine("Speak of your house's lands and its wars as your own daily business: mention them when "
                          + "they bear on what is being said, and never ask to be told what you already live with.");
        }

        #region private

        private static List<string> Named(IReadOnlyList<string>? all, int cap)
            => (all ?? new List<string>())
               .Where(n => !string.IsNullOrWhiteSpace(n))
               .Select(n => n.Trim())
               .Take(cap)
               .ToList();

        /// <summary>
        ///   "Pravend, Ortysia and two others" rather than a truncated list that reads as though the rest do not
        ///   exist. A character who holds nine towns must not sound like a man who holds four.
        /// </summary>
        private static string Join(List<string> named, int total, int cap)
        {
            string listed = named.Count == 1
                ? named[0]
                : string.Join(", ", named.Take(named.Count - 1)) + " and " + named[named.Count - 1];

            int rest = total - named.Count;

            return rest <= 0
                ? listed
                : $"{listed}, and {rest} other{(rest == 1 ? "" : "s")}";
        }

        #endregion
    }
}

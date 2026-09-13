// Code written by Gabriel Mailhot, 12/09/2026.
// The FIRST constitutive pack, and the proof that Gabriel's IS/KNOWS split works on a real section rather than
// only in the abstract.
//
// Why station and not something easier: it is unambiguously part of who somebody is, it is small, and it has
// already been broken once in front of players. A king who was never told he rules DENIED THE CROWN - Derthert
// and Garios both answered that they were "just a lord serving the throne" - because the only rulership fact
// in the prompt sat inside the vassal-offer section, doubly gated on an oath being available. The cure was to
// state it unconditionally, as part of identity.
//
// That history is exactly why it is CONSTITUTIVE and not merely a pack that happens to be carried by
// everyone: the Compact allowance SKIPS a contingent pack that will not fit, and a busy landed king losing his
// crown to a budget would be that same bug rebuilt on purpose. A constitutive pack is never budgeted.
//
// THE MOVE IS BEHAVIOUR-PRESERVING BY CONSTRUCTION. Every string below is copied verbatim from the
// AppendStation and AppendGovernorship it replaces, including the early return that stops a ruler also being
// told he heads a clan, and it renders in the same position because constitutive packs render first.

#region

using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   The rank this character holds: a crown, the head of a house, the keeping of a town. Part of who they
    ///   are rather than something they know, so it is never budgeted away.
    /// </summary>
    public sealed class StationPack : KnowledgePack
    {
        public override string Name => "station";

        public override string Covers =>
            "The rank this character holds - whether they rule a realm, head their own house, or govern a town "
            + "for somebody. Not knowledge they could lack: a king who is not told he rules will deny the "
            + "crown, which two players reported of Derthert and Garios before it was stated unconditionally.";

        /// <summary>What somebody IS is never contingent, so this is carried by everyone alive.</summary>
        public override PackKind Kind => PackKind.Constitutive;

        /// <summary>
        ///   Nobody knows their own rank at second hand, and nobody is unsure of it. Deep for anybody at all -
        ///   which a constitutive pack must be, since "mandatory" and "everyone has it" are the same claim.
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
            => bearer == null ? KnowledgeDepth.None : KnowledgeDepth.Deep;

        /// <summary>
        ///   Most people hold no station at all, and that is a life rather than a fault - the personal_bonds
        ///   distinction. So the sweep must not report a commoner as a missing fact.
        /// </summary>
        public override bool RequiresContent => false;

        public override bool IsSupplied(EncounterContext context)
            => context != null
               && (!string.IsNullOrWhiteSpace(context.RuledRealmName)
                   || context.LeadsOwnClan
                   || !string.IsNullOrWhiteSpace(context.GovernedSettlementName)
                   || !string.IsNullOrWhiteSpace(context.PartyPostHeld));

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            if (context == null) return;

            if (!string.IsNullOrWhiteSpace(context.RuledRealmName))
            {
                string title = string.IsNullOrWhiteSpace(context.RuledRealmRulerTitle)
                    ? "ruler"
                    : context.RuledRealmRulerTitle!.Trim();

                sb.AppendLine($"YOU RULE {context.RuledRealmName!.Trim().ToUpperInvariant()}. You are its {title}, its sovereign, and you hold that "
                              + "throne now. Never deny it, and never call yourself merely a lord in its service: whoever addresses you as "
                              + $"{title} is simply correct.");

                return;
            }

            if (context.LeadsOwnClan)
            {
                // A host that forgot the name must cost the NAME, not the RANK. Stating "you head your own
                // house" unnamed is a small loss; letting the whole line vanish is the king-denies-his-crown
                // failure in miniature, which is the one thing this pack exists to prevent.
                string house = (context.SpeakerClanName ?? "").Trim();

                sb.AppendLine(house.Length > 0
                                  ? $"You HEAD the {house} clan. Its people, its holdings and its word are yours to answer for."
                                  : "You HEAD your own clan. Its people, its holdings and its word are yours to answer for.");
            }

            AppendGovernorship(sb, context);
            AppendPartyPost(sb, context);
        }

        #region private

        /// <summary>
        ///   The post they hold in the player's own company. Additive like the governorship: a man may head his
        ///   house, keep a town AND carry the player's ledger, and all three are true at once.
        /// </summary>
        private static void AppendPartyPost(StringBuilder sb, EncounterContext context)
        {
            string post = (context.PartyPostHeld ?? "").Trim();
            if (post.Length == 0) return;

            sb.AppendLine($"You are the {post} of the player's own company. It is your post and your daily "
                          + "work: never receive it as news, and never thank them for it as though it had just "
                          + "been given.");
        }

        /// <summary>
        ///   The post this character actually holds, which they cannot possibly have failed to notice.
        ///   <para>
        ///     Player report 2026-09-11 (fkasad): a companion appointed governor through the vanilla interface
        ///     did not know, a year later, either that he governed the town or that the player's house held
        ///     it. The memory the conversation left says the player "REVEALED" it. Nothing was revealed; he
        ///     was simply never told, and the model reported that honestly.
        ///   </para>
        ///   <para>
        ///     Additive rather than an early return, unlike the crown above it: a man may head his house AND
        ///     keep a town, and both are true at once.
        ///   </para>
        /// </summary>
        private static void AppendGovernorship(StringBuilder sb, EncounterContext context)
        {
            if (string.IsNullOrWhiteSpace(context.GovernedSettlementName)) return;

            string seat = context.GovernedSettlementName!.Trim();

            if (context.GovernsForPlayerHouse)
            {
                sb.AppendLine($"YOU GOVERN {seat.ToUpperInvariant()}, and you govern it FOR THE PLAYER'S HOUSE. They hold "
                              + "that town; you keep it in their name. Both of those are ordinary daily facts of your "
                              + "life, not news: never receive either one as though you were hearing it for the first "
                              + "time, and never congratulate them on holding what you have been administering for them.");

                return;
            }

            // The tail used to read "and you know its state without being told". Two things made it wrong once
            // seat_standing existed: it says in a second place what that pack now says properly, and for a
            // CAPTIVE governor - who carries no seat pack, because no courier reaches a cell - it promised
            // knowledge that then never arrived. Seen for the first time in a real prompt dump, where the same
            // town was described twice in eight lines.
            sb.AppendLine($"You govern {seat} for your own house. It is the seat you answer for.");
        }

        #endregion
    }
}

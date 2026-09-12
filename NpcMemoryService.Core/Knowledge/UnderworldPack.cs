// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 9. See UnderworldFacts for the gap and for why this pack is the first real
// use of the depth axis.

#region

using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What the streets know: how the player stands with the law here, and - for those who would know it -
    ///   who holds the criminal trade of this place.
    /// </summary>
    public sealed class UnderworldPack : KnowledgePack
    {
        public override string Name => "underworld";

        public override string Covers =>
            "How badly the player is wanted in this character's realm, which anyone there would know, and who "
            + "holds the criminal trade of this place, which only the underworld would. The voice of a gang "
            + "leader already existed in the behaviour guidelines; this is what he actually knows.";

        /// <summary>
        ///   Everyone knows a wanted face; only the trade knows who runs the trade. The first pack whose two
        ///   depths carry genuinely different FACTS rather than the same fact at two lengths.
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
        {
            if (bearer == null) return KnowledgeDepth.None;

            return bearer.IsUnderworld ? KnowledgeDepth.Deep : KnowledgeDepth.Ordinary;
        }

        /// <summary>
        ///   Everyone carries this pack, and plenty of people have no law to answer to: a wanderer of no realm
        ///   has no crime rating anywhere, and a quiet town has no gang. Both are lives rather than faults, so
        ///   the sweep must not report them - the personal_bonds distinction, and the reason this is the one
        ///   universal pack that does not require content.
        /// </summary>
        public override bool RequiresContent => false;

        /// <summary>
        ///   A clean-handed player in a peaceful town is a successful read, not a missing one, so this asks
        ///   whether the host looked rather than whether it found anything - the own_body distinction.
        /// </summary>
        public override bool IsSupplied(EncounterContext context)
            => context?.Underworld != null && context.Underworld.PlayerNotoriety != Notoriety.Unknown;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            UnderworldFacts? streets = context?.Underworld;
            if (streets == null) return;

            bool deep = DepthFor(context!.Bearer) == KnowledgeDepth.Deep;

            AppendNotoriety(sb, streets.PlayerNotoriety, deep);

            if (deep) AppendStreets(sb, streets);

            // Compact stops at the facts. The conduct below is what turns them into a scene, and it is the
            // half a small model has no room for.
            if (lean) return;

            if (deep && streets.PlayerNotoriety >= Notoriety.Moderate)
                sb.AppendLine("You are no one to be scandalised by any of it. A name the garrison wants is a "
                              + "name worth something to you: it makes them useful, and it makes them "
                              + "dangerous to be seen with. Weigh both, in your own voice, and never lecture "
                              + "them about the law.");
        }

        #region private

        /// <summary>
        ///   A clean record earns NO words. Everyone in the game carries this pack, and most players are not
        ///   wanted anywhere, so the common case has to cost nothing - the same restraint that keeps
        ///   here_and_now from announcing that it is not raining.
        /// </summary>
        private static void AppendNotoriety(StringBuilder sb, Notoriety notoriety, bool deep)
        {
            switch (notoriety)
            {
                case Notoriety.Mild:
                    sb.AppendLine("You have heard the player's name in connection with some petty trouble in "
                                  + "these lands. Nothing anyone would act on.");

                    break;
                case Notoriety.Moderate:
                    sb.AppendLine("The player is a known offender in these lands. Respectable people are "
                                  + "careful to be seen with them.");

                    break;
                case Notoriety.Severe:
                    sb.AppendLine(deep
                                      ? "The player is badly wanted in these lands - the sort the garrison "
                                        + "would take on sight. That is a fact about them you have known "
                                        + "since before this conversation began."
                                      : "The player is badly wanted in these lands, and the garrison would "
                                        + "take them on sight if they knew. You know it; whether you act on "
                                        + "it is yours to decide.");

                    break;
            }
        }

        private static void AppendStreets(StringBuilder sb, UnderworldFacts streets)
        {
            string place = (streets.PlaceName ?? "").Trim();
            string here = place.Length == 0 ? "this place" : place;

            if (streets.YouRunTheseStreets)
            {
                sb.AppendLine($"The criminal trade of {here} is YOURS. Nothing is taken, sold or hidden here "
                              + "without your leave, and you never speak of that as though it were news or a "
                              + "boast: it is simply how things are.");

                return;
            }

            string boss = (streets.StreetsRunBy ?? "").Trim();

            if (boss.Length > 0)
                sb.AppendLine($"The criminal trade of {here} is held by {boss}. You know that the way anyone "
                              + "in your line knows whose ground they are standing on.");
        }

        #endregion
    }
}

// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 6. See OwnBodyFacts for what was found and what was deliberately left out.

#region

using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   Whether the character is hurt, and whether they carry a lasting maiming. Costs nothing when they are
    ///   well, which is most of the time.
    /// </summary>
    public sealed class OwnBodyPack : KnowledgePack
    {
        public override string Name => "own_body";

        public override string Covers =>
            "Whether this character is wounded and how badly, and whether the game has permanently maimed them. "
            + "Everyone knows the state of their own body without being told, which is exactly why nobody "
            + "noticed the characters had never been told. Age is NOT here: PromptBuilder already states their "
            + "years and their life stage.";

        /// <summary>A body decides what may be PROMISED. A man barely on his feet who agrees to ride out
        ///   has broken the anti-empty-promise rule, which no amount of colour makes up for.</summary>
        public override int DropPriority => 10;

        /// <summary>
        ///   Everyone, and always at the greatest depth there is: a beggar knows he is bleeding as surely as a
        ///   king does, and nobody knows a body better than the person standing in it.
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
            => bearer == null ? KnowledgeDepth.None : KnowledgeDepth.Deep;

        /// <summary>
        ///   Supplied means the host READ the body, not that the body is interesting. A hero who is perfectly
        ///   well is a successful read - so this asks for a reading, and <see cref="Render" /> is what stays
        ///   quiet when there is nothing to say.
        /// </summary>
        public override bool IsSupplied(EncounterContext context)
            => context?.OwnBody != null && context.OwnBody.Hurt != HurtBand.Unknown;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            OwnBodyFacts? body = context?.OwnBody;
            if (body == null || !body.HasAnythingToSay) return;

            string hurt = Hurt(body.Hurt);

            if (hurt.Length > 0) sb.AppendLine(hurt);

            if (body.IsMaimed)
                sb.AppendLine("You carry an old injury that never healed and never will. It is part of you now, "
                              + "not news and not a complaint.");

            // Compact stops at the fact: a character who has been told he is bleeding rarely needs telling to
            // act like it, and the Compact prompt has no room to spend on manner.
            if (lean) return;

            sb.AppendLine("Let it show in how you stand, what you are willing to take on, and how long you care "
                          + "to talk. Do not make a performance of it, and do not agree to anything your body "
                          + "plainly could not do today.");
        }

        #region private

        /// <summary>
        ///   Being whole earns NO words. This pack is carried by every character in the game and most of them
        ///   are fine, so the common case has to cost nothing - the same restraint that keeps here_and_now from
        ///   announcing that it is not raining.
        /// </summary>
        private static string Hurt(HurtBand band)
        {
            switch (band)
            {
                case HurtBand.Scratched:
                    return "You are knocked about - bruises, a shallow cut - but nothing that would stop you.";
                case HurtBand.Wounded:
                    return "You are wounded, and badly enough that anyone looking at you can tell.";
                case HurtBand.Grievous:
                    return "You are grievously hurt and barely on your feet. Standing here at all is costing you.";
                default:
                    return "";
            }
        }

        #endregion
    }
}

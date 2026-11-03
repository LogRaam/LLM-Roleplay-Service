// Code written by Gabriel Mailhot, 03/11/2026.
// Knowledge composition: the tales of a character's own people. See FolkloreFacts for why this is knowledge and not
// memory.
//
// THE HONESTY LINES ARE THE FEATURE. A character asked "tell me the one about X" for a tale it knows only by name
// would invent the story, and two characters of one people would then tell two different tales under one title.
// So only the favourite carries its parts, the others are named as outlines the character may not fill in, and every
// tale is said to be a story, never something that happened. The restraint wording comes from kurushimi00's report
// (Nexus, 01/11/2026): characters recited their memories "almost in checklist fashion".

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>The tales a character's own people tells, known as tales, and the one they could tell in full.</summary>
    public sealed class FolklorePack : KnowledgePack
    {
        public override string Name => "folklore";

        public override string Covers =>
            "The tales the character's own people tells around the fire, known AS tales and never as history: each "
            + "by its title and outline, and the one they love best by its parts, so they can tell it when asked "
            + "without inventing it.";

        /// <summary>
        ///   Colour, not consequence: a tale changes how a character speaks and nothing they can do, so it yields to
        ///   every other pack when a Compact prompt runs short.
        /// </summary>
        public override int DropPriority => 90;

        /// <summary>
        ///   Anyone raised among a people knows its tales, and a cell does not take them away: what was learned at
        ///   the hearth is carried, unlike news.
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer? bearer)
            => bearer == null ? KnowledgeDepth.None : KnowledgeDepth.Ordinary;

        /// <summary>A people with no tales on file (another mod's culture, a minor one) is a life, not a fault.</summary>
        public override bool RequiresContent => false;

        public override bool IsSupplied(EncounterContext context) => context?.Folklore?.Tales.Count > 0;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            IReadOnlyList<FolkTaleFacts>? tales = context?.Folklore?.Tales;
            if (tales == null || tales.Count == 0) return;

            FolkTaleFacts favourite = tales[0];

            if (lean)
            {
                sb.AppendLine($"A tale of your people you love: \"{favourite.Title}\", {favourite.Gist.TrimEnd('.', ' ')}. A story, not history:");
                sb.AppendLine("if asked, tell it briefly from this outline, adding no event of your own.");

                return;
            }

            sb.AppendLine("TALES YOUR PEOPLE TELL (stories, not history, and you know them as such):");
            foreach (FolkTaleFacts tale in tales)
                sb.AppendLine($"- \"{tale.Title}\": {tale.Gist}");

            List<string> beats = favourite.Beats.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
            if (beats.Count > 0)
            {
                sb.AppendLine($"The one you love best, \"{favourite.Title}\", you can tell from memory. Its parts, in order:");
                for (int i = 0; i < beats.Count; i++) sb.AppendLine($"{i + 1}. {beats[i].Trim()}");
                sb.AppendLine("Asked for a tale, you may tell this one in your own voice, keeping these events and this ending.");
            }

            sb.AppendLine("The others you know only by their outline above: if asked to tell one, say you know it only in");
            sb.AppendLine("part, and never invent the rest of its story. Allude to a tale rarely, when the moment truly calls");
            sb.AppendLine("for it, never at every meeting, and never speak of one as something that happened.");
        }
    }
}

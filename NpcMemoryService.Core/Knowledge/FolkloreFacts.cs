// Code written by Gabriel Mailhot, 03/11/2026.
// Knowledge composition: the tales a people tells around the fire. Gabriel, 03/11/2026: "des contes qui existeraient
// dans la culture des factions de Calradia... Les NPC sauraient que c'est des contes, mais pourraient parfois en faire
// reference, ou meme raconter le conte si on leur demande."
//
// Knowledge, not memory: a memory is what a character lived with the player, and a tale filed there would be told as
// something that happened. The host picks the tales (by the character's own culture) and which one they love best;
// only that favourite carries its parts, so it is the only one they can tell in full.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>One tale as a character knows it.</summary>
    public sealed class FolkTaleFacts
    {
        /// <summary>The name the tale goes by.</summary>
        public string Title { get; init; } = "";

        /// <summary>What it is about, in one line: enough for an allusion, not for a telling.</summary>
        public string Gist { get; init; } = "";

        /// <summary>
        ///   The tale's parts in order, for the one the character can tell in full; empty for a tale known only by its
        ///   outline.
        /// </summary>
        public IReadOnlyList<string> Beats { get; init; } = new List<string>();
    }

    /// <summary>The tales of a character's own people that they carry.</summary>
    public sealed class FolkloreFacts
    {
        /// <summary>
        ///   The tales they know, the one they love best FIRST (it alone carries its parts). Empty when their people
        ///   has no tales on file, which is a life and not a fault.
        /// </summary>
        public IReadOnlyList<FolkTaleFacts> Tales { get; init; } = new List<FolkTaleFacts>();
    }
}

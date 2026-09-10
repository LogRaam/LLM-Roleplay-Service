// Code written by Gabriel Mailhot, 09/09/2026.
// Player report (fkasad, Nexus, 2026-09-09), playing with The Old Realms, whose lords and holdings carry Germanic
// names: "CR starts generating memory in german... Elector Count Theoderic Gausser has 3 memories (cr.memory), 2
// first in english, the last one in german. Most probably because german names are piling up in the NPC memory.
// Still, the letter is ok, in english. The same sort of thing could happen with Nords and Aserais. And ultimately
// create dialogs using different languages."
//
//   "Ich bot Amfildor eine Summe von dreitausendsechshundertsiebenundachtzig Denaren an, um die sichere Rueckkehr
//    meines Untertans Darius der Speer zu gewaehrleisten."
//
// His reading was right, and the sentence that did it was one line long. Each of the three summarizers, when the
// player has set no explicit Reply Language, said:
//
//      "Write it in the same language as the letter below."
//      "Write it in the same language as the transcript below."
//
// That asks the model to detect a language across the WHOLE text, names and all. Under a total conversion the
// names are most of the distinctive vocabulary in the passage, so a perfectly English letter about Elector Count
// Theoderic Gausser and Darius der Speer reads, statistically, as German, and the memory comes back in German.
//
// The chat prompt never had this problem, and comparing the two is the whole lesson. PromptBuilder's
// AppendLanguageMirror anchors on ONE unambiguous thing, "detect the language of the PLAYER'S LAST MESSAGE",
// which is why fkasad's conversations and letters stayed in English while their memories drifted. Three copies of
// a language rule had quietly diverged from the one that worked.
//
// So the rule lives here once, and says out loud the thing the model was getting wrong: NAMES ARE NOT EVIDENCE.
// It matters beyond one mod. Vanilla Calradia is already full of Aserai, Sturgian, Khuzait and Battanian names,
// and fkasad is right that the same drift is reachable there; a total conversion just makes it certain.

using System.Text;

namespace NpcMemoryService.Core.Services
{
    /// <summary>
    ///   The ONE rule deciding what language a private memory is written in, asked by every summarizer instead of
    ///   each carrying its own sentence. They already diverged once from the chat prompt's version, which is the
    ///   whole reason this is a shared decision rather than a string repeated three times.
    /// </summary>
    public static class MemoryLanguagePolicy
    {
        /// <summary>
        ///   The language instruction for a summary prompt.
        ///   <para>
        ///     With an explicit <paramref name="replyLanguage" /> (the player's own setting) there is nothing to
        ///     detect and nothing to get wrong: it wins over whatever the source is written in.
        ///   </para>
        ///   <para>
        ///     With none, the language is taken from the source, but ONLY from its whole sentences. Naming that
        ///     restriction is the fix: a model left to weigh the whole passage counts proper nouns as evidence,
        ///     and under a total conversion the proper nouns outvote the prose.
        ///   </para>
        ///   <paramref name="sourceLabel" /> is what the passage is, in the words the surrounding prompt already
        ///   uses for it: "letter", "transcript", "scene".
        /// </summary>
        public static string Directive(string? replyLanguage, string sourceLabel)
        {
            string source = string.IsNullOrWhiteSpace(sourceLabel) ? "text" : sourceLabel.Trim();

            if (!string.IsNullOrWhiteSpace(replyLanguage))
                return $"Write it in {replyLanguage!.Trim()}, regardless of the language of the {source} below.";

            var sb = new StringBuilder();
            sb.AppendLine($"Write it in the same language as the PROSE of the {source} below, and judge that from");
            sb.AppendLine("whole sentences only. NAMES ARE NOT EVIDENCE OF LANGUAGE: people, places, titles, houses,");
            sb.AppendLine("realms and factions in this world may be named in any tongue at all, and a passage written");
            sb.AppendLine("in English about a Kurfuerst, a jarl or an emir is still written in English. If the");
            sb.AppendLine("sentences around those names are English, write English.");

            return sb.ToString().TrimEnd();
        }
    }
}

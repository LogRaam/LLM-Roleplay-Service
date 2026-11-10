// Code written by Gabriel Mailhot, 10/11/2026.
// The words a character has already worn out in THIS conversation, so the next reply finds new ones (Gabriel, 10/11/2026,
// reading AI Influence 7.0.0: "under the history each speaker sees which of their own turns of phrase and openings are
// already used up"; "vas-y avec les 3 dans l'ordre"). kurushimi00 (Nexus, 01/11/2026) heard characters recite like a
// checklist, and a small local model falls into the same groove within a few turns. The general rule ("VARY YOUR
// OPENINGS", in the dialogue style) was there and was not enough: a model obeys a list of its own words better than a
// principle. So the character is shown, per turn, how it opened its recent replies and the runs of words it has already
// said in two replies or more.
//
// WHERE it is shown is a matter of money. The providers players use (Grok, GLM, DeepSeek) cache the longest common
// prefix of a request automatically, past the explicit breakpoint: the 09/11/2026 replay log shows 16,768 of 16,925
// input tokens read from cache at turn 18, tail and history included. A note that changes every turn, put anywhere in
// the system prompt, would cut that cache before the history and the constant rules that close the prompt. So the note
// rides on the player's NEW line, on the wire only, in brackets that say it is not the player's words (as the prior
// exchange and witness statements already do); the session keeps the plain line, so next turn the cache runs up to the
// line before. It also sits where it is read last, next to the reply it is about.
//
// Language-blind on purpose: players write in Turkish, French, Chinese. Words are runs of letters or digits, phrases are
// runs of at least PhraseWords words found in two replies; there is no stopword list to be English about. A script written
// without spaces simply yields no phrase, which is the safe failure. Names are spared: a character who says the player's
// name twice is not repeating himself.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>What a character has already worn out in this conversation.</summary>
   public sealed class WornPhrases
   {
      /// <summary>How their recent replies opened, most recent first.</summary>
      public IReadOnlyList<string> Openings { get; init; } = Array.Empty<string>();

      /// <summary>Runs of words said in two replies or more, most recent first.</summary>
      public IReadOnlyList<string> Phrases { get; init; } = Array.Empty<string>();

      /// <summary>Nothing to show.</summary>
      public bool IsEmpty => Openings.Count == 0 && Phrases.Count == 0;
   }

   /// <summary>Finds the openings and phrases a character has worn out. Pure.</summary>
   public static class WornPhrasePolicy
   {
      /// <summary>Earlier replies needed before anything is shown: one reply has no habit yet.</summary>
      public const int MinEarlierLines = 2;

      /// <summary>How many of the latest replies are read.</summary>
      public const int LinesRead = 8;

      /// <summary>The most openings shown.</summary>
      public const int MaxOpenings = 5;

      /// <summary>The most phrases shown.</summary>
      public const int MaxPhrases = 5;

      /// <summary>Words in an opening.</summary>
      public const int OpeningWords = 4;

      /// <summary>The shortest run of words that counts as a phrase.</summary>
      public const int PhraseWords = 4;

      /// <summary>The worn openings and phrases of <paramref name="ownLines" /> (oldest first), sparing any word of <paramref name="names" />.</summary>
      public static WornPhrases Of(IReadOnlyList<string>? ownLines, IEnumerable<string?>? names)
      {
         if (ownLines == null || ownLines.Count(l => !string.IsNullOrWhiteSpace(l)) < MinEarlierLines) return new WornPhrases();

         var spared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
         foreach (string? name in names ?? Array.Empty<string?>())
         foreach (string w in Words(name))
            spared.Add(w);

         List<List<string>> lines = ownLines.Where(l => !string.IsNullOrWhiteSpace(l)).Reverse().Take(LinesRead).Select(Words).Where(w => w.Count > 0).ToList();

         return new WornPhrases {Openings = OpeningsOf(lines), Phrases = PhrasesOf(lines, spared)};
      }

      /// <summary>The note for the player's new line, or null when nothing is worn.</summary>
      public static string? Render(WornPhrases? worn)
      {
         if (worn == null || worn.IsEmpty) return null;

         var sb = new StringBuilder();
         sb.Append("[A note to you, not words the player said. WORDS YOU HAVE ALREADY USED earlier in this conversation; find new ones for this reply.");
         if (worn.Openings.Count > 0) sb.Append(" You opened with: " + string.Join(" / ", worn.Openings.Select(o => $"\"{o}...\"")) + ".");
         if (worn.Phrases.Count > 0) sb.Append(" You have already said, more than once: " + string.Join(" / ", worn.Phrases.Select(p => $"\"{p}\"")) + ".");
         sb.Append(" Your habit of speech may come back; these exact words may not. Answer in the language you were already using.]");

         return sb.ToString();
      }

      /// <summary>
      ///   <paramref name="messages" /> with <paramref name="note" /> added under the last line when it is the player's;
      ///   otherwise, or with no note, the same list. A copy: the session's own line stays as the player wrote it.
      /// </summary>
      public static IReadOnlyList<LlmMessage> OnTheLastLine(IReadOnlyList<LlmMessage> messages, string? note)
      {
         if (string.IsNullOrWhiteSpace(note) || messages == null || messages.Count == 0 || messages[messages.Count - 1].Role != MessageRole.User) return messages!;

         var sent = messages.ToList();
         LlmMessage last = sent[sent.Count - 1];
         sent[sent.Count - 1] = new LlmMessage(MessageRole.User, last.Content + Environment.NewLine + Environment.NewLine + note);

         return sent;
      }

      private static List<string> OpeningsOf(List<List<string>> lines)
      {
         var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
         var openings = new List<string>();
         foreach (List<string> words in lines)
         {
            if (words.Count < 2) continue;
            string opening = string.Join(" ", words.Take(OpeningWords));
            if (seen.Add(opening)) openings.Add(opening);
            if (openings.Count == MaxOpenings) break;
         }

         return openings;
      }

      private static List<string> PhrasesOf(List<List<string>> lines, HashSet<string> spared)
      {
         // In how many replies each run of PhraseWords words appears.
         var replies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
         foreach (List<string> words in lines)
         foreach (string gram in new HashSet<string>(Grams(words), StringComparer.OrdinalIgnoreCase))
            replies[gram] = replies.TryGetValue(gram, out int n) ? n + 1 : 1;

         var phrases = new List<string>();
         var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
         foreach (List<string> words in lines)
         {
            // The longest runs of this reply made only of worn grams: "a b c d" and "b c d e" read as "a b c d e".
            int i = 0;
            while (i + PhraseWords <= words.Count)
            {
               if (!IsWorn(words, i, replies, spared))
               {
                  i++;
                  continue;
               }

               int end = i + PhraseWords;
               while (end < words.Count && IsWorn(words, end - PhraseWords + 1, replies, spared)) end++;
               string phrase = string.Join(" ", words.Skip(i).Take(end - i));
               if (seen.Add(phrase)) phrases.Add(phrase);
               if (phrases.Count == MaxPhrases) return phrases;
               i = end;
            }
         }

         return phrases;
      }

      private static bool IsWorn(List<string> words, int at, Dictionary<string, int> replies, HashSet<string> spared)
      {
         if (at + PhraseWords > words.Count) return false;
         for (int k = at; k < at + PhraseWords; k++)
            if (spared.Contains(words[k])) return false;

         return replies.TryGetValue(Gram(words, at), out int n) && n >= 2;
      }

      private static IEnumerable<string> Grams(List<string> words)
      {
         for (int i = 0; i + PhraseWords <= words.Count; i++)
            yield return Gram(words, i);
      }

      private static string Gram(List<string> words, int at) => string.Join(" ", words.Skip(at).Take(PhraseWords));

      private static List<string> Words(string? text)
      {
         var words = new List<string>();
         if (string.IsNullOrEmpty(text)) return words;

         var current = new StringBuilder();
         foreach (char c in text!)
         {
            if (char.IsLetterOrDigit(c) || (c == '\'' && current.Length > 0))
            {
               current.Append(c);
               continue;
            }

            Flush(current, words);
         }

         Flush(current, words);

         return words;
      }

      private static void Flush(StringBuilder current, List<string> words)
      {
         string word = current.ToString().TrimEnd('\'');
         if (word.Length > 0) words.Add(word);
         current.Clear();
      }
   }
}

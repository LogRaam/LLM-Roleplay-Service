// Code written by Gabriel Mailhot, 13/09/2026.
//
// WHY THIS EXISTS. Gabriel's play-test, 13:31:32. Nabb held the floor, and the reply that came back was not a
// reply - it was a SCRIPT, with everybody's lines in it, including the player's:
//
//   *Ira's fingers linger on Nabb's sleeve...*
//   [Nabb]: *Nabb holds his ground as Ira steps back...*
//   [Arwa]: *Arwa steps forward...* "A proposal, Lady Ira. ... I brought Nabb because he is the finest
//           blade I have, and because ... he has a gift for making himself indispensable. ... I offer you
//           his service. His counsel. His blade. And whatever else may grow from that."
//   [Nabb]: *Nabb's lips twitch...*
//
// The player never said a word of that. And what it says out loud, to the very person it was meant to stay
// hidden from, is the BRIEFING PLAN. The whole companion-briefing pillar rests on the target not learning
// what was said in private; here the model handed it over in the player's own voice. It then wrote an
// [EVENT] recording the alliance the player had supposedly just proposed.
//
// WHERE IT COMES FROM, and this is why prompting alone will not hold. Third-party voices reach a model as
// "[Name]: content" (ChatSession.AddWitnessStatement, and the same shape is used for a witness stimulus).
// That format is a script format. Shown enough of it, a model continues the script - and once it is writing
// the scene rather than a character, the player is just another part to write. The prompt is advice; this is
// the law.
//
// WHAT IT DOES NOT DO. It does not police an unlabelled sentence about the player ("Arwa said nothing"), and
// it must not try: an NPC narrating the room, the player included, is this mod's ordinary third-person style,
// and Gabriel has never objected to it. Only a LABELLED turn is cut, because only a labelled turn is certain.

#region

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace NpcMemoryService.Core.Parsing
{
   /// <summary>What a reply had to have removed from it, if anything.</summary>
   public sealed class SpeakerScriptVerdict
   {
      /// <summary>The text with every foreign speaker's turn removed. Unchanged when nothing was cut.</summary>
      public string Text { get; init; } = string.Empty;

      /// <summary>The names whose turns were cut, in the order they appeared.</summary>
      public IReadOnlyList<string> CutSpeakers { get; init; } = Array.Empty<string>();

      /// <summary>True when one of the cut turns was the PLAYER's - the serious case.</summary>
      public bool SpokeForThePlayer { get; init; }

      /// <summary>True when anything at all was removed.</summary>
      public bool CutAnything => CutSpeakers.Count > 0;
   }

   /// <summary>
   ///   Cuts turns a reply wrote for OTHER people out of it. Pure and engine-free: it is given the text, who
   ///   is actually speaking, and what the player is called, and returns the text that speaker is entitled to.
   /// </summary>
   public static class SpeakerScriptGuard
   {
      // A speaker label at the START of a line: "[Arwa]:" or a bare "Arwa:".
      //
      // The BRACKETED form is the one the conversation history teaches, so it is always a label. The BARE form
      // is only a label when it names the speaker or the player, and that check happens below rather than in
      // this pattern - because "One thing:" and "A warning:" open a title-cased word before a colon exactly
      // the way a name does, and cutting real prose is a far worse failure than missing a stray label.
      private static readonly Regex Label = new(
         @"(?m)^[ \t]*(?:\[(?<b>[^\]\r\n]{1,40})\]|(?<p>[A-Z][\p{L}'\-]{1,20}(?:[ ][A-Z][\p{L}'\-]{1,20}){0,3}))[ \t]*:[ \t]*",
         RegexOptions.Compiled);

      /// <summary>
      ///   Removes every labelled turn that does not belong to <paramref name="speakerName" />.
      ///   <para>
      ///     Text before the first label is KEPT: a reply normally opens unlabelled, and that opening is the
      ///     speaker's own prose (their narration of the room included). Only once a label appears is the
      ///     reply admitting it is writing parts, and from there each part is judged by whose name it carries.
      ///   </para>
      ///   <para>
      ///     A label carrying the speaker's OWN name is not a foreign turn: its content is kept and the
      ///     redundant label dropped, so a model that merely announces itself is tidied rather than silenced.
      ///   </para>
      /// </summary>
      public static SpeakerScriptVerdict Cut(string? text, string? speakerName, string? playerName)
      {
         if (string.IsNullOrWhiteSpace(text))
            return new SpeakerScriptVerdict {Text = text ?? string.Empty};

         MatchCollection labels = Label.Matches(text!);
         if (labels.Count == 0) return new SpeakerScriptVerdict {Text = text!};

         var kept = new StringBuilder();
         var cut = new List<string>();
         var spokeForPlayer = false;

         // Everything before the first label belongs to the speaker: an ordinary reply opens this way.
         kept.Append(text!.Substring(0, labels[0].Index));

         for (var i = 0; i < labels.Count; i++)
         {
            Match m = labels[i];
            bool bracketed = m.Groups["b"].Success;
            string who = (bracketed ? m.Groups["b"].Value : m.Groups["p"].Value).Trim();

            int bodyStart = m.Index + m.Length;
            int bodyEnd = i + 1 < labels.Count ? labels[i + 1].Index : text!.Length;
            string body = text!.Substring(bodyStart, bodyEnd - bodyStart);

            // A bare "Word:" is only a speaker label when it actually names somebody in this scene. Otherwise
            // it is prose ("A proposal:", "One condition:") and is put back exactly as it was written.
            if (!bracketed && !IsSamePerson(who, speakerName) && !IsSamePerson(who, playerName))
            {
               kept.Append(text!.Substring(m.Index, m.Length)).Append(body);

               continue;
            }

            if (IsSamePerson(who, speakerName))
            {
               kept.Append(body); // their own turn: keep the words, drop the redundant label

               continue;
            }

            cut.Add(who);
            if (IsSamePerson(who, playerName)) spokeForPlayer = true;
         }

         return new SpeakerScriptVerdict {
            Text = kept.ToString().Trim(),
            CutSpeakers = cut,
            SpokeForThePlayer = spokeForPlayer
         };
      }

      /// <summary>
      ///   Whether a label names the same person as <paramref name="known" />. Matches on the whole name or on
      ///   its FIRST word, because a model labels a turn "[Nabb]" for "Nabb the Bloody Handed" and "[Arwa]" for
      ///   a player whose full name carries a clan. Deliberately loose in that one direction: over-matching the
      ///   speaker's own label keeps their words, which is the safe way to be wrong.
      /// </summary>
      public static bool IsSamePerson(string? label, string? known)
      {
         if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(known)) return false;

         string a = label!.Trim();
         string b = known!.Trim();

         if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;

         return string.Equals(FirstWord(a), FirstWord(b), StringComparison.OrdinalIgnoreCase);
      }

      private static string FirstWord(string s)
      {
         int space = s.IndexOf(' ');

         return space > 0 ? s.Substring(0, space) : s;
      }
   }
}

// Code written by Gabriel Mailhot, 30/09/2026.
// THE MOOD OF THE DAY, increments 3 and 4 (ROADMAP). The one short block the character is given about his day, in the
// per-turn tail below the encounter marker (it changes per conversation and, since increment 4, per turn: it must never
// sit in the cacheable prefix), and the ladder the player moves him along.
//
// Ruling 1: mood COLOURS delivery and never decides an outcome; the line says so in as many words. Ruling 4: the player
// moves it, one band per turn and two across a conversation, and the line says where he began and where he is now.
// Ruling 5: wit is rare and English (understatement, at one's own expense, said straight); only when the draw permits.
// The character reports how the exchange left him on a "mood:" line inside the [STANCE] block he already knows.

#region

using System;
using System.Text;

#endregion

namespace NpcMemoryService.Core.Mood
{
   /// <summary>The order the player can move a mood along, from bleakest to brightest. Pure.</summary>
   public static class MoodLadder
   {
      /// <summary>The most one turn can move a mood, whatever the character reports.</summary>
      public const int MaxStepsPerTurn = 1;

      /// <summary>The most a whole conversation can move it: a bleak man can be warmed, not transformed.</summary>
      public const int MaxStepsPerConversation = 2;

      private static readonly DayMood[] Rungs = {DayMood.Grim, DayMood.ShortTempered, DayMood.Guarded, DayMood.Even, DayMood.Expansive, DayMood.HighSpirits};

      /// <summary>The conversation's running total after one more turn's judgement, both clamped.</summary>
      public static int AddTurn(int stepsSoFar, int thisTurn)
      {
         int turn = Math.Max(-MaxStepsPerTurn, Math.Min(MaxStepsPerTurn, thisTurn));

         return Math.Max(-MaxStepsPerConversation, Math.Min(MaxStepsPerConversation, stepsSoFar + turn));
      }

      /// <summary>
      ///   Where a man who began the conversation in <paramref name="start" /> stands after <paramref name="steps" />
      ///   (clamped to the conversation's limit). Restless sits level with an ordinary day; Weary one below it, level
      ///   with guarded (lifted he is at ordinary ease, lowered he runs short of patience).
      /// </summary>
      public static DayMood Shift(DayMood start, int steps)
      {
         int clamped = Math.Max(-MaxStepsPerConversation, Math.Min(MaxStepsPerConversation, steps));
         if (clamped == 0) return start;

         int at = start switch {
            DayMood.Restless => Array.IndexOf(Rungs, DayMood.Even),
            DayMood.Weary => Array.IndexOf(Rungs, DayMood.Guarded),
            _ => Array.IndexOf(Rungs, start)
         };
         int to = Math.Max(0, Math.Min(Rungs.Length - 1, at + clamped));

         return Rungs[to];
      }
   }

   /// <summary>Composes the character's mood block for the prompt's per-turn tail. Pure.</summary>
   public static class MoodLine
   {
      /// <summary>
      ///   The block, or empty when no day was drawn. <paramref name="cause" /> is why the day began as it did
      ///   (<see cref="MoodCause.Of" />): a fact is said as true, a trifle as a small private matter.
      /// </summary>
      public static string Compose(DayMood? start, DayMood? now, bool witPermitted, DayCause? cause = null)
      {
         if (start == null) return string.Empty;

         DayMood current = now ?? start.Value;
         var sb = new StringBuilder();
         sb.AppendLine("YOUR DAY:");

         string? feeling = Describe(current);
         if (current != start.Value)
            sb.AppendLine($"You began this conversation {Brief(start.Value)}; how it has gone has changed that. "
                          + (feeling ?? "You are now at an ordinary ease."));
         else if (feeling != null)
            sb.AppendLine(feeling);

         if (cause != null && !string.IsNullOrWhiteSpace(cause.Text) && start.Value != DayMood.Even)
            sb.AppendLine(cause.IsReal
                             ? $"What set your day this way: {cause.Text}. That is true; speak of it if asked or if it fits, or keep it to yourself."
                             : $"What set your day this way: {cause.Text}, nothing more. A small private matter: name no one over it and make "
                               + "it no bigger than it is; speak of it if asked or if it fits, or keep it to yourself.");

         // fkasad (19/10/2026): "a way to lift the spirits of a depressed NPC ... accurate and logical arguments the model
         // can weigh against what caused the npc moral drop." The lift already existed (the [STANCE] mood line, one band a
         // turn); nothing told him that a true answer to his trouble is what moves it.
         if (cause != null && cause.IsReal && !string.IsNullOrWhiteSpace(cause.Text) && IsLow(start.Value))
            sb.AppendLine("If the one you speak with answers that trouble with something true and sound (a reason it is less dark "
                          + "than it looks, a way through it), weigh it as you would: it may lift you, a step at a time. Flattery and "
                          + "empty cheer do not.");

         if (feeling != null || current != start.Value)
            sb.AppendLine("This is how you FEEL, not what you decide: it colours how you speak, never what you will agree to, and "
                          + "never why this conversation is happening (come to thank, you still thank; come to ask, you still ask). "
                          + "Let your own temperament shape how it shows.");

         if (witPermitted && current == DayMood.HighSpirits)
            sb.AppendLine("At most once in this conversation, and only if the moment gives it something to land on, you may "
                          + "allow yourself one dry remark: understatement, never exaggeration; at your own expense or nobody's, "
                          + "never at anyone beneath you; said straight, without signalling the joke; never a pun or a modern turn of phrase.");

         sb.AppendLine("If this exchange lifted or lowered your mood, say so in your [STANCE] block with the line "
                       + "\"mood: lifted\" or \"mood: lowered\"; leave it out when it did neither.");
         sb.AppendLine();

         return sb.ToString();
      }

      /// <summary>
      ///   The line for a letter written today (increment 5, Ruling 6: "a letter is written on a day too"), or null on an
      ///   ordinary day or none. A letter cannot be moved by the player as it is written, so it carries the day alone.
      /// </summary>
      public static string? ForLetter(DayMood? mood)
      {
         if (mood == null || mood == DayMood.Even) return null;

         return $"You write this on a day when you are {Brief(mood.Value)}. Let it show in how you write, never in what you decide.";
      }

      // The days a trouble weighs on: below an ordinary day on the ladder, and weary.
      private static bool IsLow(DayMood mood)
         => mood == DayMood.Grim || mood == DayMood.ShortTempered || mood == DayMood.Guarded || mood == DayMood.Weary;

      private static string? Describe(DayMood mood) => mood switch {
         DayMood.Grim => "It is a grim day for you. Your answers run shorter than the question deserves, and you do not pretend otherwise.",
         DayMood.ShortTempered => "Your patience is thin today. Courtesy is there, but it costs you, and it shows.",
         DayMood.Guarded => "You are giving nothing away today. Not hostile: closed. You answer what is asked and little more.",
         DayMood.Restless => "You are restless today, energy with nowhere to put it: you get ahead of things and change the subject.",
         DayMood.Expansive => "You are in an open-handed humour today, and say a little more than you strictly meant to.",
         DayMood.HighSpirits => "You are in genuinely good spirits today.",
         DayMood.Weary => "You are weary today: worn down, not unkind. Your answers come slower and shorter, and you have little appetite for anything that asks effort.",
         _ => null
      };

      private static string Brief(DayMood mood) => mood switch {
         DayMood.Grim => "grim",
         DayMood.ShortTempered => "short of patience",
         DayMood.Guarded => "guarded",
         DayMood.Restless => "restless",
         DayMood.Expansive => "in an open-handed humour",
         DayMood.HighSpirits => "in good spirits",
         DayMood.Weary => "weary",
         _ => "at an ordinary ease"
      };
   }
}

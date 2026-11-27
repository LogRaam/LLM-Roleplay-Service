// Code written by Gabriel Mailhot, 26/11/2026.
// The floor judge of group conversations. Gabriel (26/11/2026): one thread per character; after every line a fast, cheap
// model says, for each listener, whether they react and how much they want to speak; "le juge ne fait pas parler" (it never
// writes dialogue: the player's prose model does), and "il ne faut pas sacrifier la qualite". The host's referee
// (TurnArbiter in Calradia Remembers) combines this verdict with what the game knows and picks one speaker.
//
// Built for the prompt cache, as Gabriel asked ("accumuler l'historique [...] s'annuler en cout dans le CACHE"): the rules
// and the listener's sheet are the stable system prompt; the scene so far is written in order, one line per line spoken, and
// only grows at its end, so each call re-reads the previous one from cache and pays in full only for the new line.
// The verdict is two plain lines, not JSON: a small model keeps a two-line form better than a structure.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>How much a listener wants to speak. Coarse on purpose: a small model's fine scores are noise.</summary>
   public enum JudgedIntent
   {
      /// <summary>Nothing to say.</summary>
      None,

      /// <summary>Would say something if the floor fell to them.</summary>
      Low,

      /// <summary>Wants to speak: has something to add or answer.</summary>
      High,

      /// <summary>Must speak now: provoked, threatened, or the matter is theirs.</summary>
      Urgent
   }

   /// <summary>The listener the judge speaks for, as the host describes them.</summary>
   public sealed class FloorJudgeListener
   {
      /// <summary>Their name as the scene writes it.</summary>
      public string Name { get; init; } = "";

      /// <summary>Who they are in a line or two: station, temperament, what they care about.</summary>
      public string Persona { get; init; } = "";

      /// <summary>One line per other person present: who they are to this listener ("Ira: a rival captain you distrust").</summary>
      public IReadOnlyList<string> Ties { get; init; } = new List<string>();
   }

   /// <summary>One line of the scene so far.</summary>
   public sealed class FloorJudgeLine
   {
      /// <summary>Who spoke it.</summary>
      public string Speaker { get; init; } = "";

      /// <summary>Who it was said to: a name, "everyone", or empty.</summary>
      public string To { get; init; } = "";

      /// <summary>What was said or done.</summary>
      public string Text { get; init; } = "";
   }

   /// <summary>The judge's verdict for one listener.</summary>
   public sealed class FloorJudgeVerdict
   {
      /// <summary>How much they want to speak.</summary>
      public JudgedIntent Intent { get; init; }

      /// <summary>A short non-verbal (or brief sound) reaction to the last line, or null.</summary>
      public string? Reaction { get; init; }

      /// <summary>False when the reply could not be read: the host treats the listener as having nothing to say.</summary>
      public bool Parsed { get; init; }
   }

   /// <summary>Builds the judge's prompt and reads its verdict. Pure.</summary>
   public static class FloorJudge
   {
      /// <summary>The longest reaction kept; a longer one is cut at a word, since a reaction is a gesture, not a speech.</summary>
      public const int MaxReactionChars = 160;

      /// <summary>
      ///   The stable part: the rules and the listener's sheet. Identical on every call for this listener in this scene, so it
      ///   is the cached prefix.
      /// </summary>
      public static string StablePrompt(FloorJudgeListener listener)
      {
         var sb = new StringBuilder();
         sb.AppendLine("You judge ONE listener in a group conversation set in a medieval world. You never write dialogue.");
         sb.AppendLine("After the last line spoken, say how much this listener wants to take the floor, and how they react without words.");
         sb.AppendLine();
         sb.AppendLine("intent, one of:");
         sb.AppendLine("- none: nothing to say; the line does not concern them, or they would keep quiet.");
         sb.AppendLine("- low: they might add something if the floor fell to them.");
         sb.AppendLine("- high: they want to speak: they were asked, they disagree, they have something to add that matters to them.");
         sb.AppendLine("- urgent: they must speak now: insulted, mocked, accused, threatened, or the matter is theirs above all.");
         sb.AppendLine("reaction: a brief visible reaction in asterisks (a glance, a frown, a laugh), or none. Never words spoken aloud.");
         sb.AppendLine();
         sb.AppendLine("Judge as THIS person would, by their temperament and their ties to the others: a proud lord bristles at a slight,");
         sb.AppendLine("a timid squire keeps quiet before his betters, a rival seizes an opening. Being addressed is not a reason to want");
         sb.AppendLine("to speak more than they would; being spoken ABOUT may be.");
         sb.AppendLine();
         sb.AppendLine("Answer with exactly two lines and nothing else:");
         sb.AppendLine("intent: <none|low|high|urgent>");
         sb.AppendLine("reaction: <*a brief gesture*|none>");
         sb.AppendLine();
         sb.AppendLine($"THE LISTENER: {listener?.Name}");
         if (!string.IsNullOrWhiteSpace(listener?.Persona)) sb.AppendLine(listener!.Persona.Trim());
         foreach (string tie in listener?.Ties ?? new List<string>())
            if (!string.IsNullOrWhiteSpace(tie)) sb.AppendLine("- " + tie.Trim());

         return sb.ToString();
      }

      /// <summary>
      ///   The scene so far and the question, in order. Each new line is appended at the end, so the text for one call is the
      ///   start of the text for the next: the provider's cache reads the earlier part.
      /// </summary>
      public static string TurnPrompt(string listenerName, IReadOnlyList<FloorJudgeLine> scene)
      {
         var sb = new StringBuilder();
         sb.AppendLine("THE SCENE SO FAR:");
         foreach (FloorJudgeLine line in scene ?? new List<FloorJudgeLine>())
         {
            string to = string.IsNullOrWhiteSpace(line.To) ? "" : $" (to {line.To.Trim()})";
            sb.AppendLine($"{line.Speaker}{to}: {line.Text}");
         }

         sb.AppendLine();
         sb.AppendLine($"Judge {listenerName} after the last line.");

         return sb.ToString();
      }

      /// <summary>Reads the judge's two lines. Lenient on case, spacing and stray text; unreadable means nothing to say.</summary>
      public static FloorJudgeVerdict Parse(string? reply)
      {
         if (string.IsNullOrWhiteSpace(reply)) return new FloorJudgeVerdict {Intent = JudgedIntent.None, Parsed = false};

         JudgedIntent? intent = null;
         string? reaction = null;
         foreach (string raw in reply!.Split('\n'))
         {
            string line = raw.Trim().TrimStart('-', '*', ' ').Trim();
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;

            string key = line.Substring(0, colon).Trim().ToLowerInvariant();
            string value = line.Substring(colon + 1).Trim();
            if (key == "intent" && intent == null) intent = ReadIntent(value);
            else if (key == "reaction" && reaction == null) reaction = ReadReaction(value);
         }

         return new FloorJudgeVerdict {Intent = intent ?? JudgedIntent.None, Reaction = reaction, Parsed = intent != null};
      }

      private static JudgedIntent? ReadIntent(string value)
      {
         string word = new string(value.TrimStart('<', '"', '\'', ' ').TakeWhile(char.IsLetter).ToArray()).ToLowerInvariant();
         switch (word)
         {
            case "none": return JudgedIntent.None;
            case "low": return JudgedIntent.Low;
            case "high": return JudgedIntent.High;
            case "urgent": return JudgedIntent.Urgent;
            default: return null;
         }
      }

      private static string? ReadReaction(string value)
      {
         string v = value.Trim().Trim('"', '<', '>').Trim();
         if (v.Length == 0 || v.Equals("none", StringComparison.OrdinalIgnoreCase)) return null;
         if (v.Length <= MaxReactionChars) return v;

         int cut = v.LastIndexOf(' ', MaxReactionChars);

         return v.Substring(0, cut > 0 ? cut : MaxReactionChars).TrimEnd() + "*";
      }
   }
}

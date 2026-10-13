// Code written by Gabriel Mailhot, 13/10/2026.
// Fitting one request inside a local model's context window, decided by CR instead of the server.
//
// fkasad (Nexus, 13/10/2026), on a local model in LM Studio: after a few turns "the AI derails, forgets stuff we just
// said ... a long discussion ending with a 'Give something' is not working. The context lost the rules needed for a
// 'give'." CR sends the whole prompt and the conversation fresh on every reply; when that is bigger than the server's
// window, the server cuts it by its own overflow policy, and LM Studio's "truncate middle" cuts the middle of CR's
// prompt, where the rules for actions sit. Gabriel (13/10/2026): CR trims it itself, never the action rules.
//
// The order, cheapest loss first: the oldest raw lines of the conversation (the running recap and the latest lines
// stay), then the compact prompt with less history again. The prompt is never cut piecewise: it is whole or compact.

#region

using System;
using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>One message of the conversation, as the fit sees it.</summary>
   public sealed class ContextFitMessage
   {
      /// <summary>The player's (or a witness's) turn, as opposed to the character's own.</summary>
      public bool IsUser { get; init; }

      /// <summary>Its length in characters.</summary>
      public int Chars { get; init; }
   }

   /// <summary>What one request is made of.</summary>
   public sealed class ContextFitFacts
   {
      /// <summary>The model's context window in tokens, as the player declared it.</summary>
      public int WindowTokens { get; init; }

      /// <summary>Characters per token for this endpoint (measured on the last reply, else the default).</summary>
      public double CharsPerToken { get; init; } = ContextFitPolicy.DefaultCharsPerToken;

      /// <summary>The system prompt's length in characters.</summary>
      public int PromptChars { get; init; }

      /// <summary>The reply budget (max tokens), which the server counts against the same window.</summary>
      public int ReplyTokens { get; init; }

      /// <summary>The conversation, oldest first; the last one is the player's new line.</summary>
      public IReadOnlyList<ContextFitMessage> Messages { get; init; } = Array.Empty<ContextFitMessage>();

      /// <summary>Leading messages never dropped (the running recap of the older conversation).</summary>
      public int PinnedLeading { get; init; }

      /// <summary>The fewest messages kept after the pinned ones (the latest lines, the new one included).</summary>
      public int KeepAtLeast { get; init; } = 1;
   }

   /// <summary>The fit of one request.</summary>
   public sealed class ContextFit
   {
      /// <summary>How many messages to leave out, right after the pinned ones (the oldest raw lines).</summary>
      public int Drop { get; init; }

      /// <summary>The request's estimated size in tokens once those are left out, reply budget included.</summary>
      public int EstimatedTokens { get; init; }

      /// <summary>The share of the window CR fills (the rest is margin for the estimate and the chat template).</summary>
      public int UsableTokens { get; init; }

      /// <summary>Whether it fits.</summary>
      public bool Fits => EstimatedTokens <= UsableTokens;
   }

   /// <summary>What CR did to one request to fit it, for the host's log and its one notice to the player.</summary>
   public sealed class ContextFitReport
   {
      /// <summary>Whose turn it was (the character's id), or null.</summary>
      public string? Subject { get; init; }

      /// <summary>The declared window.</summary>
      public int WindowTokens { get; init; }

      /// <summary>The request's estimated size as sent, reply budget included.</summary>
      public int EstimatedTokens { get; init; }

      /// <summary>The share of the window CR planned to fill.</summary>
      public int UsableTokens { get; init; }

      /// <summary>How many old lines were left out.</summary>
      public int DroppedMessages { get; init; }

      /// <summary>Whether the compact prompt replaced the full one.</summary>
      public bool SwitchedToCompact { get; init; }

      /// <summary>Whether it fits even so; false means the window is too small for the compact prompt itself.</summary>
      public bool Fits { get; init; }

      /// <summary>The characters-per-token ratio the estimate used.</summary>
      public double CharsPerToken { get; init; }
   }

   /// <summary>Whether a request fits a declared context window, and which old lines to leave out so it does. Pure.</summary>
   public static class ContextFitPolicy
   {
      /// <summary>
      ///   Before any reply has been measured. English prose runs about 4 characters a token; CR's prompt is denser
      ///   (names, markup, headings), so the estimate leans to more tokens rather than fewer: an underestimate is the
      ///   server truncating after all.
      /// </summary>
      public const double DefaultCharsPerToken = 3.5;

      /// <summary>What a chat template adds around each message (role markers), in tokens.</summary>
      public const int PerMessageTokens = 8;

      /// <summary>The share of the window CR plans to fill; the rest absorbs the estimate's error.</summary>
      public const double UsableShare = 0.9;

      /// <summary>With the full prompt, at least this many latest lines stay: fewer, and compact is the better trade.</summary>
      public const int KeepWithFullPrompt = 6;

      /// <summary>
      ///   Leaves out the oldest raw lines, after the pinned recap, until the request fits or only
      ///   <see cref="ContextFitFacts.KeepAtLeast" /> remain. When anything is left out, the conversation then starts on
      ///   a player's line (a local chat template may refuse a conversation that opens on the character's).
      /// </summary>
      public static ContextFit Fit(ContextFitFacts f)
      {
         if (f == null) return new ContextFit();

         int usable = (int) Math.Floor(Math.Max(0, f.WindowTokens) * UsableShare);
         double cpt = f.CharsPerToken > 0 ? f.CharsPerToken : DefaultCharsPerToken;
         IReadOnlyList<ContextFitMessage> m = f.Messages ?? Array.Empty<ContextFitMessage>();
         int pinned = Math.Min(Math.Max(0, f.PinnedLeading), m.Count);
         int maxDrop = Math.Max(0, m.Count - pinned - Math.Max(1, f.KeepAtLeast));

         int drop = 0;
         while (drop < maxDrop && Cost(f, m, pinned, drop, cpt) > usable) drop++;

         if (drop > 0)
            while (drop < m.Count - pinned - 1 && !m[pinned + drop].IsUser)
               drop++;

         return new ContextFit {Drop = drop, EstimatedTokens = Cost(f, m, pinned, drop, cpt), UsableTokens = usable};
      }

      /// <summary>
      ///   Characters per token measured on a reply the server counted, or null when it reported nothing usable.
      ///   Clamped to what text can plausibly be, so one odd reply cannot throw the next estimate far off.
      /// </summary>
      public static double? Measured(int sentChars, int promptTokens)
      {
         if (sentChars <= 0 || promptTokens <= 0) return null;

         return Math.Min(6.0, Math.Max(2.0, (double) sentChars / promptTokens));
      }

      /// <summary>
      ///   Whether the client's one retry of a cut reply, at double the reply budget, still fits: if not, it is not
      ///   attempted (it would overflow the window it is trying to finish in).
      /// </summary>
      public static bool RetryFits(ContextFit fit, int replyTokens)
         => fit != null && fit.EstimatedTokens + Math.Max(0, replyTokens) <= fit.UsableTokens;

      #region private

      private static int Cost(ContextFitFacts f, IReadOnlyList<ContextFitMessage> m, int pinned, int drop, double cpt)
      {
         double tokens = f.PromptChars / cpt + Math.Max(0, f.ReplyTokens);
         for (int i = 0; i < m.Count; i++)
         {
            if (i >= pinned && i < pinned + drop) continue;
            tokens += (m[i]?.Chars ?? 0) / cpt + PerMessageTokens;
         }

         return (int) Math.Ceiling(tokens);
      }

      #endregion
   }
}

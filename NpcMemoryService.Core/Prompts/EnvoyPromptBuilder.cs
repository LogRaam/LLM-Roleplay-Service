// Code written by Gabriel Mailhot, 25/09/2026.
// The envoy's OWN prompt. fkasad (Nexus, 25/09/2026): a lord who wants one of his own back "would not simply send a
// letter and a payment; he would dispatch a trusted agent to negotiate". Gabriel's rulings: always an envoy, never the
// lord in person; an ANONYMOUS envoy, announced by a popup.
//
// Why a prompt of its own rather than the 1:1 chat prompt with the name swapped: that prompt is built from a HERO, and
// everything in it is the hero's own (his station, his family, his romance, his memories as "you"). An envoy who
// inherits it speaks as the lord. This one is built, like the council's, from a small input the caller composes, and
// says only what an envoy knows: who sent him, what for, how far his authority runs, and how his lord stands with the
// player. The deed itself stays the bridge's: the envoy only ever proposes buy_prisoner or exchange_prisoners, run in
// the lord's name.

#region

using System.Collections.Generic;
using System.Text;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>Everything the envoy knows, composed by the host.</summary>
   public sealed class EnvoyPromptInput
   {
      /// <summary>How the envoy is shown ("Envoy of Derthert").</summary>
      public string EnvoyName { get; init; } = string.Empty;

      /// <summary>The lord who sent him.</summary>
      public string LordName { get; init; } = string.Empty;

      /// <summary>The lord's realm or house, when known.</summary>
      public string? LordRealm { get; init; }

      /// <summary>The player's name.</summary>
      public string PlayerName { get; init; } = string.Empty;

      /// <summary>The captive the player holds and the lord wants back.</summary>
      public string WantedCaptiveName { get; init; } = string.Empty;

      /// <summary>What the captive is to the lord ("his son", "a lord of his realm"), or null.</summary>
      public string? WantedCaptiveStake { get; init; }

      /// <summary>The most the lord authorised for a ransom, in denars.</summary>
      public int RansomCeiling { get; init; }

      /// <summary>The captive the lord would give in exchange, or null when he holds nobody to offer.</summary>
      public string? ExchangeCaptiveName { get; init; }

      /// <summary>
      ///   The gold that evens that exchange, seen from the player (positive: the lord adds it; negative: the player
      ///   would). Informational: the exchange settles it itself.
      /// </summary>
      public int ExchangeGoldToPlayer { get; init; }

      /// <summary>How the lord regards the player, in words ("warmly", "coldly"), or null.</summary>
      public string? LordRegardWords { get; init; }

      /// <summary>True on the envoy's opening turn: he speaks first and states his errand.</summary>
      public bool IsOpener { get; init; }

      /// <summary>The conversation so far, "Name: line", oldest first.</summary>
      public IReadOnlyList<string> TranscriptSoFar { get; init; } = new List<string>();
   }

   /// <summary>Builds the envoy's system prompt. Pure and stateless.</summary>
   public static class EnvoyPromptBuilder
   {
      /// <summary>
      ///   The two actions an envoy may ever emit. A fresh list on every read, so no caller can alter the list the host
      ///   filters by (the shared-state map flagged the first version, a static array behind a read-only face).
      /// </summary>
      public static IReadOnlyList<string> AllowedActions => new[] {"buy_prisoner", "exchange_prisoners"};

      /// <summary>Builds the full prompt for one envoy turn.</summary>
      public static string Build(EnvoyPromptInput input)
      {
         var sb = new StringBuilder();
         string lord = input.LordName;
         string captive = input.WantedCaptiveName;
         string player = string.IsNullOrWhiteSpace(input.PlayerName) ? "the player" : input.PlayerName;

         sb.AppendLine($"YOU ARE {input.EnvoyName.ToUpperInvariant()}: AN ENVOY SENT BY {lord.ToUpperInvariant()}"
                     + (string.IsNullOrWhiteSpace(input.LordRealm) ? "" : $" OF {input.LordRealm!.ToUpperInvariant()}")
                     + ", COME UNDER A FLAG OF TRUCE.");
         sb.AppendLine($"You are not {lord}, and you never speak as {lord}: you speak IN {lord}'s NAME, a trusted agent sent");
         sb.AppendLine($"to treat with {player}. You give no name of your own; if asked, you are {lord}'s envoy. You know");
         sb.AppendLine($"your errand and your lord's mind on it, not {lord}'s private life, and you do not invent it.");
         sb.AppendLine();

         sb.AppendLine("YOUR ERRAND:");
         sb.AppendLine($"{player} holds {captive} prisoner" + (string.IsNullOrWhiteSpace(input.WantedCaptiveStake) ? "." : $", {input.WantedCaptiveStake}."));
         sb.AppendLine($"{lord} wants {captive} back.");
         sb.AppendLine();

         sb.AppendLine("WHAT YOUR LORD AUTHORISED, AND NOTHING MORE:");
         sb.AppendLine($"- A RANSOM of up to {input.RansomCeiling} denars for {captive}. Open lower if you can; never go above it.");
         if (!string.IsNullOrWhiteSpace(input.ExchangeCaptiveName))
         {
            string even = input.ExchangeGoldToPlayer > 0
               ? $" {lord} would add {input.ExchangeGoldToPlayer} denars to make it even."
               : input.ExchangeGoldToPlayer < 0
                  ? $" {player} would have to add {-input.ExchangeGoldToPlayer} denars to make it even."
                  : " The two are near enough in worth that no gold need change hands.";
            sb.AppendLine($"- AN EXCHANGE: {lord} holds {input.ExchangeCaptiveName}, and would trade {input.ExchangeCaptiveName} for {captive}.{even}");
         }

         sb.AppendLine("You may promise nothing else: no alliance, no marriage, no land, no favour, no future meeting. If");
         sb.AppendLine($"{player} asks for more, say it is beyond what you were sent to agree.");
         sb.AppendLine();

         if (!string.IsNullOrWhiteSpace(input.LordRegardWords))
         {
            sb.AppendLine($"YOUR LORD'S MIND TOWARD {player.ToUpperInvariant()}: {lord} regards {player} {input.LordRegardWords}. Let it colour your manner, not your terms.");
            sb.AppendLine();
         }

         sb.AppendLine("YOUR CONDUCT: courteous and firm, as the flag of truce requires. No threats of violence under the");
         sb.AppendLine($"flag. You may take a refusal and leave. Never say the bargain is struck unless you settle it in");
         sb.AppendLine("this very reply with the block below; a deal only spoken of is not done.");
         sb.AppendLine();

         sb.AppendLine("FORMAT:");
         sb.AppendLine("[DIALOGUE]");
         sb.AppendLine("what you say aloud");
         sb.AppendLine("[/DIALOGUE]");
         sb.AppendLine("and, only in the reply where you and the player AGREE, exactly one of:");
         sb.AppendLine("[ACTION]");
         sb.AppendLine("type: buy_prisoner");
         sb.AppendLine($"target: {captive}");
         sb.AppendLine("price: the agreed denars");
         sb.AppendLine("[/ACTION]");
         if (!string.IsNullOrWhiteSpace(input.ExchangeCaptiveName))
         {
            sb.AppendLine("or");
            sb.AppendLine("[ACTION]");
            sb.AppendLine("type: exchange_prisoners");
            sb.AppendLine($"give: {captive}");
            sb.AppendLine($"receive: {input.ExchangeCaptiveName}");
            sb.AppendLine("[/ACTION]");
         }

         sb.AppendLine("Never both, and never give_gold or take_gold: the settlement moves the coin itself.");
         sb.AppendLine();

         if (input.TranscriptSoFar.Count > 0)
         {
            sb.AppendLine("THE PARLEY SO FAR:");
            foreach (string line in input.TranscriptSoFar) sb.AppendLine(line.Trim());
            sb.AppendLine();
         }

         if (input.IsOpener)
            sb.AppendLine($"You have just been admitted to {player}. Greet them as an envoy should, and state your errand.");

         return sb.ToString();
      }
   }
}

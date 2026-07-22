// Code written by Gabriel Mailhot, 21/07/2026.
// Player report (Nexus): the Encyclopedia's discovery section, meant to hold ONLY what the PLAYER has
// learned ABOUT the NPC, was filling up with facts ABOUT THE PLAYER instead. Root cause traced to
// PromptBuilder.AppendDiscoveryInstructions teaching "description: what this player now perceives, in
// their voice" - a model easily reads "in their voice" as license to write about the player rather than
// from the player's viewpoint about the NPC. The prompt wording is now unambiguous, but this guard is the
// safety net for whatever drift remains: it recognises a [DISCOVERY] whose subject is plainly the PLAYER,
// not the NPC being talked to, so ProfileMutator can drop it instead of storing it (same pattern as
// MetaReasoningGuard: pure detection, consumer decides to reject). Deliberately no grammatical analysis;
// only the handful of clear, explicit tells listed below, so a legitimate discovery that merely MENTIONS
// the player mid-sentence never gets caught (false negatives are preferred: the player can never see a
// silently-dropped discovery, so wrongly dropping one is the worse failure).

#region

using System;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace NpcMemoryService.Core.Parsing
{
   /// <summary>
   ///   Detects a <c>[DISCOVERY]</c> entry whose subject is the PLAYER rather than the NPC who is
   ///   supposed to be the one revealed about. Three explicit signals only:
   ///   <list type="bullet">
   ///     <item>the description's opening words are literally "the player";</item>
   ///     <item>the description opens with the player's own name (<paramref name="playerName" />, when supplied);</item>
   ///     <item>the key is namespaced to "player" (e.g. <c>player_orientation</c>).</item>
   ///   </list>
   ///   No attempt is made to parse the rest of the sentence, so a discovery that names the player
   ///   somewhere in the middle ("she admires how Huan Yi handles a blade") still passes: that
   ///   discovery IS about the NPC (her admiration), the player is merely its object.
   /// </summary>
   public static class DiscoverySubjectGuard
   {
      private static readonly Regex PlayerAsSubject = new(
         @"^\s*the player\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

      /// <summary>
      ///   True when <paramref name="key" /> or <paramref name="description" /> plainly identify the
      ///   PLAYER, not the NPC, as the discovery's subject. Null/blank input never throws and is never
      ///   flagged. <paramref name="playerName" /> is optional; when supplied, a description opening
      ///   with that name is also caught.
      /// </summary>
      public static bool IsAboutPlayer(string? key, string? description, string? playerName = null)
      {
         if (KeyNamespacedToPlayer(key)) return true;
         if (string.IsNullOrWhiteSpace(description)) return false;
         if (PlayerAsSubject.IsMatch(description!)) return true;

         return OpensWithPlayerName(description!, playerName);
      }

      /// <summary>A key such as "player_orientation" or bare "player" names the PLAYER as the subject, not the NPC being discovered.</summary>
      private static bool KeyNamespacedToPlayer(string? key)
         => !string.IsNullOrWhiteSpace(key)
         && key!.Split('_').Any(segment => string.Equals(segment, "player", StringComparison.OrdinalIgnoreCase));

      /// <summary>The description's very first word is the player's own name, the clearest sign the sentence is ABOUT them, not the NPC.</summary>
      private static bool OpensWithPlayerName(string description, string? playerName)
      {
         if (string.IsNullOrWhiteSpace(playerName)) return false;

         foreach (string token in playerName!.Split((char[]) null!, StringSplitOptions.RemoveEmptyEntries))
         {
            if (token.Length < 2) continue;
            if (Regex.IsMatch(description, $@"^\s*{Regex.Escape(token)}\b", RegexOptions.IgnoreCase))
               return true;
         }

         return false;
      }
   }
}

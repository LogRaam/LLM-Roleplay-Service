// Code written by Gabriel Mailhot, 02/06/2026.
// Sprint 12b: NPC-initiated letter correspondence.

#region

using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   Builds the player-facing message that is injected into the LLM pipeline to
   ///   trigger letter generation. The "player message" is a meta-instruction that
   ///   tells the NPC to write a letter rather than hold a conversation; the NPC's
   ///   normal <c>[DIALOGUE]</c> response then becomes the letter body.
   ///
   ///   This deliberately reuses <see cref="NpcChatService" /> rather than a new
   ///   LLM path so the NPC's full personality, history, and romantic context are
   ///   automatically included in the prompt at no extra cost.
   /// </summary>
   public static class LetterPromptBuilder
   {
      // A letter carries no encounter context, so the writer is not told where they currently are.
      // Left to itself the model invents a location from the most salient memory (the last town it
      // saw named), and writes as if still there long after both parties have marched off to war.
      // This guard stops that confident, stale claim: keep the setting unstated unless it is certain.
      private const string LocationGuard =
         "You are writing from wherever you presently are, which may well be on the march, at war, or " +
         "travelling. Do NOT state or assume a specific place that you or the player are in, and do NOT " +
         "treat a town named in an old memory as where either of you is now. When in doubt, leave the " +
         "letter's setting unstated rather than name a stale location.";

      // When the host knows where the writer currently is, ground the letter there and forbid drifting to
      // a remembered town; when it does not (null/blank), fall back to the guard against naming any place.
      private static string WhereaboutsLine(string whereabouts)
         => string.IsNullOrWhiteSpace(whereabouts)
            ? LocationGuard
            : $"You are presently {whereabouts.Trim()}. If your letter mentions where you are, it is there and " +
              "nowhere else; never place yourself in a town merely because an old memory named it.";

      /// <summary>
      ///   Builds the trigger message for an NPC-initiated letter (no player reply).
      /// </summary>
      public static string BuildInitialLetterMessage(
         NpcProfile npc, LetterReason reason, string triggerContext, string playerName, string whereabouts = null)
      {
         var sb = new StringBuilder();
         sb.AppendLine("[LETTER GENERATION — INTERNAL INSTRUCTION, DO NOT INCLUDE IN YOUR RESPONSE]");
         sb.AppendLine($"You have decided to write a personal letter to {playerName}.");
         sb.AppendLine();
         sb.AppendLine($"Occasion: {DescribeReason(reason)}");
         if (!string.IsNullOrWhiteSpace(triggerContext))
            sb.AppendLine($"Context: {triggerContext}");
         sb.AppendLine();
         // Your recent history with this player is already in the system prompt's own history section
         // (AppendHistory); repeating a 3-event digest here just to write a letter is redundant.
         sb.AppendLine("Write the letter now, in your own voice. This is correspondence,");
         sb.AppendLine("not a face-to-face conversation. Keep it to 2-3 paragraphs.");
         sb.AppendLine($"Address {playerName} by name. Do not use modern expressions.");
         sb.AppendLine(WhereaboutsLine(whereabouts));
         sb.AppendLine("Write ONLY the letter body in [DIALOGUE]. Do not emit [EVENT], [ACTION], [STANCE], or any");
         sb.AppendLine("section other than the letter text. No section headers, no meta-text.");
         return sb.ToString();
      }

      /// <summary>
      ///   Asks the NPC to decide whether to send a written reply to a player-sent
      ///   courier letter. The LLM outputs either "PASS" (no reply) or a "DELAY: N"
      ///   header followed by the letter body. The delay is how many days the NPC
      ///   waits before dispatching the courier — 1 (urgent) to 7 (considered).
      /// </summary>
      public static string BuildPlayerLetterReplyDecisionMessage(
         NpcProfile npc, string playerLetterContent, string playerName, string whereabouts = null)
      {
         var sb = new StringBuilder();
         sb.AppendLine("[PLAYER LETTER RECEIVED — INTERNAL INSTRUCTION, DO NOT INCLUDE IN YOUR RESPONSE]");
         sb.AppendLine($"You have just received the following letter from {playerName} via courier:");
         sb.AppendLine();
         sb.AppendLine($"\"{playerLetterContent}\"");
         sb.AppendLine();
         // Your recent history with this player is already in the system prompt's own history section
         // (AppendHistory); repeating a 3-event digest here just to decide on a reply is redundant.
         sb.AppendLine("A courier letter is a deliberate act, and it deserves a written reply.");
         sb.AppendLine("Reply unless the letter is entirely nonsensical, in a language you cannot read,");
         sb.AppendLine("or so offensive that your character would refuse all contact. PASS should be rare.");
         sb.AppendLine();
         sb.AppendLine("If you truly will NOT reply, write \"PASS: [one sentence reason]\" in [DIALOGUE].");
         sb.AppendLine("Otherwise, start [DIALOGUE] with exactly:");
         sb.AppendLine("  DELAY: N");
         sb.AppendLine("where N is the days you would wait before sending (1=urgent, 2-3=normal, 4-7=considered).");
         sb.AppendLine("Leave one blank line, then write your reply letter in 2-3 paragraphs.");
         sb.AppendLine($"Address {playerName} by name. Period-appropriate language only.");
         sb.AppendLine(WhereaboutsLine(whereabouts));
         sb.AppendLine($"Write your reply in the SAME language as the quoted letter from {playerName} above, not the");
         sb.AppendLine("language of this internal instruction.");
         sb.AppendLine("Write ONLY the letter body after the DELAY line. Do not emit [EVENT], [ACTION], [STANCE], or");
         sb.AppendLine("any section other than the letter text. No section headers.");
         sb.AppendLine();
         sb.AppendLine("Example output:");
         sb.AppendLine("DELAY: 3");
         sb.AppendLine("(blank line, then 2-3 paragraphs of the letter)");
         sb.AppendLine("Or, to decline: PASS: the message insults my house and I will not dignify it with a reply.");
         return sb.ToString();
      }

      /// <summary>
      ///   Builds the trigger message for an NPC reply to the player's response.
      /// </summary>
      public static string BuildReplyLetterMessage(
         NpcProfile npc, string playerReply, LetterReason originalReason, string playerName, string whereabouts = null)
      {
         var sb = new StringBuilder();
         sb.AppendLine("[LETTER REPLY GENERATION — INTERNAL INSTRUCTION, DO NOT INCLUDE IN YOUR RESPONSE]");
         sb.AppendLine($"You received a letter from {playerName} in reply to your previous message.");
         sb.AppendLine();
         sb.AppendLine($"{playerName} wrote:");
         sb.AppendLine($"\"{playerReply}\"");
         sb.AppendLine();
         sb.AppendLine($"Original occasion: {DescribeReason(originalReason)}");
         sb.AppendLine();

         bool isRomanticReply = originalReason == LetterReason.RomanticCorrespondence
                                || originalReason == LetterReason.AwaitingReply
                                || originalReason == LetterReason.SpouseCorrespondence;
         if (isRomanticReply)
         {
            sb.AppendLine("You had been anxiously awaiting this reply. Let the relief and warmth show —");
            sb.AppendLine("this answer matters to you. React to what they actually said before continuing");
            sb.AppendLine("your own thoughts. Do not open with a generic pleasantry.");
            sb.AppendLine();
         }

         sb.AppendLine("Write your reply letter in 2-3 paragraphs. Stay in character.");
         sb.AppendLine($"Address {playerName} by name. Do not use modern expressions.");
         sb.AppendLine(WhereaboutsLine(whereabouts));
         sb.AppendLine($"Write the letter in the same language as the quoted letter from {playerName} above, not the");
         sb.AppendLine("language of this internal instruction.");
         sb.AppendLine("Write ONLY the letter body in [DIALOGUE]. Do not emit [EVENT], [ACTION], [STANCE], or any");
         sb.AppendLine("section other than the letter text. No section headers, no meta-text.");
         return sb.ToString();
      }

      // ── Helpers ─────────────────────────────────────────────────────────────

      private static string DescribeReason(LetterReason reason) => reason switch
      {
         LetterReason.TournamentVictory     => "The player recently won a tournament. You are writing to congratulate — or to challenge.",
         LetterReason.BattleVictory         => "The player recently won a notable battle. You are acknowledging their victory.",
         LetterReason.QuestUpdate           => "You have a task outstanding with the player and wish to check on its progress.",
         LetterReason.MarriageProposal      => "You are proposing a match between your family and the player — a matter of alliance and honour.",
         LetterReason.ReinforcementRequest  => "Your faction is at war and you need the player's military support. You are calling in favour or obligation.",
         LetterReason.GangFavor             => "You want the player to strike a rival gang on your behalf — a dangerous request that comes with a reward.",
         LetterReason.PoliticalAlliance     => "You are hinting at or proposing a formal political alignment between your interests.",
         LetterReason.RomanticCorrespondence => "You miss the player and wish to write something personal — tender, longing, or playful, as your nature dictates.",
         LetterReason.SpouseCorrespondence  => "You are the player's partner and have not seen them in days. You are writing as a spouse would — with warmth, worry, or reproach.",
         LetterReason.CorruptionAttempt     => "You want information about the player's liege or war plans, and are willing to pay — or threaten.",
         LetterReason.Blackmail             => "You know the player has a secret and you intend to use it unless they pay handsomely. The tone should be veiled menace.",
         LetterReason.BirthAnnouncement     => "You have just given birth to the player's child. Write to inform him — the tone depends on your relationship: tender if you love him, matter-of-fact if it was an arrangement, conflicted if it is a secret. Mention the child's name and sex.",
         LetterReason.ChildSupportRequest   => "Some weeks have passed since you informed the player of your child. You are now asking him to help provide financially for the child's upbringing. Be specific about what you need and why. The tone should reflect your character and your relationship.",
         LetterReason.AwaitingReply         => "You wrote a personal letter to the player some days ago and have received no reply. Write again — impatient, longing, or quietly wounded — asking if your letter arrived and why they have not answered. Let your nature dictate the tone: a bold character may demand an answer; a tender one may confess they have read the silence as rejection.",
         LetterReason.JealousThreat         => "Word has reached you of a romantic act by the player that wounds or angers you. You are writing to confront, warn, or threaten them over it. The Context names your exact stake — whose affection was at issue and why it is yours to resent. Let your nature set the register: a cold warning, a wounded reproach, or an open threat to make them regret it. Do not be servile, and do not back down; make plain what you expect of them now.",
         _                                  => "You have a matter of personal importance to communicate."
      };
   }
}

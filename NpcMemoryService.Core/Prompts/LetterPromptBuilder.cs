// Code written by Gabriel Mailhot, 02/06/2026.
// Sprint 12b: NPC-initiated letter correspondence.

#region

using System.Linq;
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
      /// <summary>
      ///   Builds the trigger message for an NPC-initiated letter (no player reply).
      /// </summary>
      public static string BuildInitialLetterMessage(
         NpcProfile npc, LetterReason reason, string triggerContext, string playerName)
      {
         var sb = new StringBuilder();
         sb.AppendLine("[LETTER GENERATION — INTERNAL INSTRUCTION, DO NOT INCLUDE IN YOUR RESPONSE]");
         sb.AppendLine($"You have decided to write a personal letter to {playerName}.");
         sb.AppendLine();
         sb.AppendLine($"Occasion: {DescribeReason(reason)}");
         if (!string.IsNullOrWhiteSpace(triggerContext))
            sb.AppendLine($"Context: {triggerContext}");
         sb.AppendLine();
         AppendRecentHistory(sb, npc);
         sb.AppendLine();
         sb.AppendLine("Write the letter now, in your own voice. This is correspondence —");
         sb.AppendLine("not a face-to-face conversation. Keep it to 2–3 paragraphs.");
         sb.AppendLine($"Address {playerName} by name. Do not use modern expressions.");
         sb.AppendLine("Write ONLY the letter body in [DIALOGUE]. No section headers, no meta-text.");
         return sb.ToString();
      }

      /// <summary>
      ///   Asks the NPC to decide whether to send a written reply to a player-sent
      ///   courier letter. The LLM outputs either "PASS" (no reply) or a "DELAY: N"
      ///   header followed by the letter body. The delay is how many days the NPC
      ///   waits before dispatching the courier — 1 (urgent) to 7 (considered).
      /// </summary>
      public static string BuildPlayerLetterReplyDecisionMessage(
         NpcProfile npc, string playerLetterContent, string playerName)
      {
         var sb = new StringBuilder();
         sb.AppendLine("[PLAYER LETTER RECEIVED — INTERNAL INSTRUCTION, DO NOT INCLUDE IN YOUR RESPONSE]");
         sb.AppendLine($"You have just received the following letter from {playerName} via courier:");
         sb.AppendLine();
         sb.AppendLine($"\"{playerLetterContent}\"");
         sb.AppendLine();
         AppendRecentHistory(sb, npc);
         sb.AppendLine();
         sb.AppendLine("Decide whether this letter warrants a written reply.");
         sb.AppendLine("Consider: does it raise something personal, important, or requiring an answer?");
         sb.AppendLine("Would your character actually write back, or simply act on it in person next time?");
         sb.AppendLine();
         sb.AppendLine("If you choose NOT to reply, write \"PASS: [one sentence reason]\" in [DIALOGUE].");
         sb.AppendLine("If you choose to reply, start [DIALOGUE] with exactly:");
         sb.AppendLine("  DELAY: N");
         sb.AppendLine("where N is the days you would wait before sending (1=urgent, 2-3=normal, 4-7=considered).");
         sb.AppendLine("Leave one blank line, then write your reply letter in 2-3 paragraphs.");
         sb.AppendLine($"Address {playerName} by name. Period-appropriate language only.");
         sb.AppendLine("Write ONLY the letter body after the DELAY line. No section headers.");
         return sb.ToString();
      }

      /// <summary>
      ///   Builds the trigger message for an NPC reply to the player's response.
      /// </summary>
      public static string BuildReplyLetterMessage(
         NpcProfile npc, string playerReply, LetterReason originalReason, string playerName)
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
         sb.AppendLine("Write your reply letter in 2–3 paragraphs. Stay in character.");
         sb.AppendLine($"Address {playerName} by name. Do not use modern expressions.");
         sb.AppendLine("Write ONLY the letter body in [DIALOGUE]. No section headers, no meta-text.");
         return sb.ToString();
      }

      // ── Helpers ─────────────────────────────────────────────────────────────

      private static void AppendRecentHistory(StringBuilder sb, NpcProfile npc)
      {
         if (npc.Events == null || npc.Events.Count == 0) return;

         var recent = npc.Events
            .OrderByDescending(e => e.gameDay)
            .Take(3)
            .ToList();

         sb.AppendLine("Your recent history with this player:");
         foreach (var ev in recent)
            sb.AppendLine($"- Day {ev.gameDay}: {ev.summary}");
      }

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
         _                                  => "You have a matter of personal importance to communicate."
      };
   }
}

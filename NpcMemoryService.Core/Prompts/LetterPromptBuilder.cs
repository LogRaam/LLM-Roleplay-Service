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
      private static string WhereaboutsLine(string? whereabouts)
         => string.IsNullOrWhiteSpace(whereabouts)
            ? LocationGuard
            : $"You are presently {whereabouts!.Trim()}. If your letter mentions where you are, it is there and " +
              "nowhere else; never place yourself in a town merely because an old memory named it.";

      // Appends where the writer is, then (when the host knows it) where the PLAYER is / whether they are
      // together, so a letter does not summon a player home who is standing in the same hall.
      private static void AppendPlace(StringBuilder sb, string? whereabouts, string? playerSituation)
      {
         sb.AppendLine(WhereaboutsLine(whereabouts));
         if (!string.IsNullOrWhiteSpace(playerSituation)) sb.AppendLine(playerSituation);
      }

      // Player report: letters never named their sender, so the player could not tell who to answer (worse when
      // the writer wears a helmet in their portrait, or is challenging them to a duel / naming a meeting place).
      // The window heading shows "A LETTER FROM X", but the letter itself read anonymously; make the writer sign
      // it, and introduce themselves plainly when they are barely known.
      private static string SignOffLine(NpcProfile npc, string playerName)
         => $"Sign the letter at the end with your own name, {npc.Name}, so {playerName} knows at a glance who "
          + "wrote it and whom to answer. If the two of you are barely acquainted, also name yourself and your "
          + "house plainly in the opening.";

      /// <summary>
      ///   Builds the trigger message for an NPC-initiated letter (no player reply).
      /// </summary>
      public static string BuildInitialLetterMessage(
         NpcProfile npc, LetterReason reason, string triggerContext, string playerName, string? whereabouts = null, string? playerSituation = null)
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
         sb.AppendLine(SignOffLine(npc, playerName));
         AppendPlace(sb, whereabouts, playerSituation);
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
         NpcProfile npc, string playerLetterContent, string playerName, string? whereabouts = null, string? playerSituation = null)
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
         sb.AppendLine(SignOffLine(npc, playerName));
         // The weaker form of AppendNothingIsSettledByLetter: this path answers a letter the PLAYER chose to
         // write, so the host cannot know what is being answered and cannot name the debt. The invariant still
         // holds for every reason, and it is the only guard on this path.
         sb.AppendLine("Coin and goods do not travel with a letter. If they promise a payment here, it has NOT");
         sb.AppendLine("been made: never write as though you had received it, and settle such things in person.");
         AppendPlace(sb, whereabouts, playerSituation);
         sb.AppendLine($"Write your reply in the SAME language as the quoted letter from {playerName} above, not the");
         sb.AppendLine("language of this internal instruction.");
         sb.AppendLine("Write ONLY the letter body after the DELAY line. Do not emit [EVENT], [ACTION], [STANCE], or");
         sb.AppendLine("any section other than the letter text, with one narrow exception below. No section headers.");
         sb.AppendLine();
         sb.AppendLine("If, and only if, the player's letter clearly asks for MORE or FEWER letters going forward,");
         sb.AppendLine("append exactly one further line, on its own, after the letter body:");
         sb.AppendLine("  [CORRESPONDENCE_PREFERENCE] scope=<sender|subject|all> direction=<less|more> strength=<mild|strong>");
         sb.AppendLine("scope=sender is about letters from you specifically; subject is about this kind of news; all");
         sb.AppendLine("is about correspondence in general. Omit the line entirely when the player asked for no such");
         sb.AppendLine("thing; never invent one. It is metadata for the courier system, never part of the letter.");
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
         NpcProfile npc, string playerReply, LetterReason originalReason, string playerName, string? whereabouts = null, string? playerSituation = null)
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

         if (RequiresInPersonSettlement(originalReason)) AppendNothingIsSettledByLetter(sb);

         sb.AppendLine("Write your reply letter in 2-3 paragraphs. Stay in character.");
         sb.AppendLine($"Address {playerName} by name. Do not use modern expressions.");
         sb.AppendLine(SignOffLine(npc, playerName));
         AppendPlace(sb, whereabouts, playerSituation);
         sb.AppendLine($"Write the letter in the same language as the quoted letter from {playerName} above, not the");
         sb.AppendLine("language of this internal instruction.");
         sb.AppendLine("Write ONLY the letter body in [DIALOGUE]. Do not emit [EVENT], [ACTION], [STANCE], or any");
         sb.AppendLine("section other than the letter text. No section headers, no meta-text.");
         return sb.ToString();
      }

      // ── Helpers ─────────────────────────────────────────────────────────────

      /// <summary>
      ///   Reasons whose whole point is the player HANDING OVER COIN, which no letter can do: a letter is
      ///   forbidden from emitting any [ACTION] (see every builder above), and the payment only becomes real
      ///   when the live chat emits pay_blackmail (blackmail) or take_gold (which stamps the ProvideGold
      ///   quest for child support). Left to itself the model answered "your silence is bought" or "the child
      ///   is provided for", and the player then watched the secret break or the quest lapse anyway: they
      ///   were told a debt was discharged that the game never discharged. The same redirect
      ///   <see cref="LetterReason.SpouseDivorceDemand" /> already uses in its own description ("when you
      ///   next speak") is the fix, so the settlement happens where the mechanics actually live.
      /// </summary>
      private static bool RequiresInPersonSettlement(LetterReason reason)
         => reason == LetterReason.Blackmail || reason == LetterReason.ChildSupportRequest;

      // Kept as a shared block so the unknown-reason path (a player-initiated letter, where the host cannot
      // tell what the player is answering) can state the same invariant in its own weaker form.
      private static void AppendNothingIsSettledByLetter(StringBuilder sb)
      {
         sb.AppendLine("NOTHING IS SETTLED BY LETTER. Whatever they pledge here, no coin has reached you and no");
         sb.AppendLine("debt is discharged: a purse cannot travel in an envelope. Never write as though you had");
         sb.AppendLine("been paid, never call the matter closed, and never say your silence is bought or the");
         sb.AppendLine("child provided for. Take their pledge for what it is, then make plain that you expect it");
         sb.AppendLine("from their own hand when you next meet face to face.");
         sb.AppendLine();
      }

      private static string DescribeReason(LetterReason reason) => reason switch
      {
         LetterReason.TournamentVictory     => "The player recently won a tournament. You are writing to congratulate — or to challenge.",
         LetterReason.BattleVictory         => "The player recently won a notable battle. You are acknowledging their victory.",
         LetterReason.QuestUpdate           => "You have a task outstanding with the player and wish to check on its progress.",
         LetterReason.MarriageProposal      => "You are writing about a marriage between the player and your house, a matter of alliance and honour. The Context tells you WHO you are in this match and in whose voice to write (the head of a house offering their kin, a parent blessing a child, or the prospective spouse themselves accepting), and what has moved you to write now. Follow it, and write as the person the Context names rather than as a generic matchmaker.",
         LetterReason.ReinforcementRequest  => "Your faction is at war and you need the player's military support. You are calling in favour or obligation.",
         LetterReason.GangFavor             => "You want the player to strike a rival gang on your behalf — a dangerous request that comes with a reward.",
         LetterReason.PoliticalAlliance     => "You are hinting at or proposing a formal political alignment between your interests.",
         LetterReason.RomanticCorrespondence => "You are writing the player a personal letter. Let your CURRENT feeling toward them (stated in the Context) set the tone: warm and tender when the bond is strong, cooler or more guarded when it is not. Do not default to longing if you do not truly feel it.",
         LetterReason.SpouseCorrespondence  => "You are the player's spouse, writing to them while you are apart. Let your CURRENT feeling toward them (stated in the Context) set the tone: a warm, contented marriage writes with warmth, affection, and news, while a strained one may carry worry or reproach. Do not default to reproach or longing when the bond is in fact strong.",
         LetterReason.CorruptionAttempt     => "You want information about the player's liege or war plans, and are willing to pay — or threaten.",
         LetterReason.Blackmail             => "You know the player has a secret and you intend to use it unless they pay handsomely. The tone should be veiled menace.",
         LetterReason.BirthAnnouncement     => "You have just given birth to the player's child. Write to inform him — the tone depends on your relationship: tender if you love him, matter-of-fact if it was an arrangement, conflicted if it is a secret. Mention the child's name and sex.",
         LetterReason.ChildSupportRequest   => "Some weeks have passed since you informed the player of your child. You are now asking him to help provide financially for the child's upbringing. Be specific about what you need and why. The tone should reflect your character and your relationship.",
         LetterReason.AwaitingReply         => "You wrote a personal letter to the player some days ago and have had no reply. Write again to ask whether it arrived. Let your CURRENT feeling toward them (stated in the Context) and your nature set the tone: a warm, secure partner writes lightly and fondly, unworried; a strained one may write impatiently or quietly wounded. Do not assume rejection when the bond is in fact strong.",
         LetterReason.JealousThreat         => "Word has reached you of a romantic act by the player that wounds or angers you. You are writing to confront, warn, or threaten them over it. The Context names your exact stake — whose affection was at issue and why it is yours to resent. Let your nature set the register: a cold warning, a wounded reproach, or an open threat to make them regret it. Do not be servile, and do not back down; make plain what you expect of them now.",
         LetterReason.SpouseDivorceDemand   => "You are the player's own spouse, and you have grown truly, deeply unhappy in this marriage. You are writing to announce, plainly and without cruelty, that you can no longer bear it, and that when you next speak you mean to press them to end it. Do not be pleading or servile; you have made up your mind. Let your nature set the register: cold resolve, quiet grief, or open reproach.",
         LetterReason.SpouseDivorceEscalation => "You are the player's own spouse. You have pressed them before to end this marriage and been refused, more than once. You have resolved to wait no longer, and are writing to say, plainly, that you begin to end it yourself now, whether or not they consent. Let your nature set the register (cold resolve, quiet grief, or open reproach), but make plain that this is no longer a request.",
         LetterReason.InterceptionMissed    => "You rode out meaning to speak with the player face to face, but the chase came to nothing before you could catch them. The Context below says exactly why you set out. Write now to say what you meant to say in person: do not apologise for writing instead of meeting, simply say it as you would have said it to their face.",
         LetterReason.RealmTidings          => "You count this player a friend, and you are writing unbidden to share news stirring in your realm that has reached you. The Context below is that news, told exactly as much as you know of it, and how surely it came to you (a faint whisper, word passed to you, or a thing confirmed). Relay ONLY that news, in your own voice and with your own concerns, as one friend passing word of the wider world to another; let your certainty match how it reached you rather than overstating a rumour. Do NOT invent or add ANY fact, number, name, place, date, or outcome that is not in the Context: if a detail is not there, you do not know it, and you say nothing of it. Bring no other news.",
         LetterReason.LeakedCorrespondence  => "You have come into possession of a private letter the player sent to someone else, intercepted on the road after its courier was waylaid. The Context below is what that letter revealed. Write to the player to exploit what you now know: make plain that you have read their words, and turn the knowledge to your advantage as your own nature dictates (a veiled threat, a demand, cold mockery, or a cynical offer). Draw ONLY on what the Context reveals; invent no fact, name, plan, or detail that is not there.",
         LetterReason.ServiceOffer          => "You are NOT the player's liege, and you are writing to poach them: leave the lord they now serve and enter YOUR service instead. The Context below names the enticement you dangle (a purse of gold, a castle of their own, or a company of soldiers) and why you want them at your side. Make your case in your own voice, name that enticement plainly, and be persuasive without grovelling. Do not pretend the matter is settled; this is an offer they may take or leave.",
         LetterReason.PrisonerOffer         => "You are writing to buy a prisoner the player currently holds. The Context below names WHO the prisoner is and why they matter to you, and the price you offer. State plainly whom you want and what you will pay, and make your case in your own voice. Do not pretend the trade is done; it is an offer the player may accept or refuse.",
         _                                  => "You have a matter of personal importance to communicate."
      };
   }
}

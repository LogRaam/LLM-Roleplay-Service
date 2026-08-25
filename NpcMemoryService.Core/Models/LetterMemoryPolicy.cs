// Code written by Gabriel Mailhot, 20/08/2026.
// Player report (Desporion): an NPC proposed marriage by letter and the terms were "agreed" over three
// letters, then denied it all when the player spoke to him in person. Root cause: LetterPopupManager.MarkReceived
// recorded only a GENERIC event ("Received a letter from X (Reason)"), never the letter's substance, so the
// live chat prompt (which reads profile.Events) had nothing to go on and the NPC contradicted his own letters.
// This policy decides WHICH letters carry substance worth a real memory (ShouldRemember) and provides a
// synchronous, guaranteed-capture first-person fallback line (BaseMemory) for the two-stage pattern mirrored
// from CaptiveSceneSummarizer/ConversationSummarizer: a base event is always recorded, then LetterMemorySummarizer
// replaces it with a richer summary when the reason is substantive enough to be worth the LLM call.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Decides whether a letter's substance is worth remembering as a first-person NPC memory, and builds the
   ///   synchronous fallback line used before (or instead of) the richer async summary.
   /// </summary>
   public static class LetterMemoryPolicy
   {
      /// <summary>
      ///   True when a letter of this <paramref name="reason" /> carries substance an NPC should be able to
      ///   reference face to face (a proposal, a request, a threat, news). False only for the two pure re-pings
      ///   that add nothing beyond what an earlier memory already covers: <see cref="LetterReason.AwaitingReply" />
      ///   (a nag about a letter already remembered) and <see cref="LetterReason.QuestUpdate" /> (the quest itself
      ///   is tracked separately, in <c>ActiveQuests</c>). An explicit switch, defaulting true, so a future
      ///   LetterReason is remembered by default rather than silently dropped.
      /// </summary>
      public static bool ShouldRemember(LetterReason reason)
      {
         switch (reason)
         {
            case LetterReason.AwaitingReply:
            case LetterReason.QuestUpdate:
               return false;
            default:
               return true;
         }
      }

      /// <summary>
      ///   A synchronous, first-person (as the NPC) memory line, guaranteed even if the richer LLM summary never
      ///   returns. When <paramref name="npcIsSender" /> is true (the NPC wrote this letter, the direction
      ///   <c>LetterPopupManager.MarkReceived</c> always uses), the line reads "I wrote to {player} ({phrase}).";
      ///   otherwise ("{player} wrote to me, and I replied ({phrase})."), for the reverse direction (the NPC
      ///   received the player's letter). <paramref name="npcName" /> is accepted for symmetry with the reason's
      ///   other consumers, but the line is written in the first person and never needs to name its own author.
      /// </summary>
      public static string BaseMemory(LetterReason reason, string npcName, string playerName, bool npcIsSender)
      {
         string player = string.IsNullOrWhiteSpace(playerName) ? "the player" : playerName.Trim();
         string phrase = PlainWords(reason);

         return npcIsSender
            ? $"I wrote to {player} ({phrase})."
            : $"{player} wrote to me, and I replied ({phrase}).";
      }

      #region private

      /// <summary>A short plain-words phrase describing the occasion, for the parenthetical in BaseMemory.</summary>
      private static string PlainWords(LetterReason reason)
      {
         switch (reason)
         {
            case LetterReason.TournamentVictory: return "to congratulate a tournament win";
            case LetterReason.BattleVictory: return "to congratulate a battle victory";
            case LetterReason.QuestUpdate: return "with word on a quest already between us";
            case LetterReason.MarriageProposal: return "to propose a marriage match";
            case LetterReason.ReinforcementRequest: return "to call for military aid";
            case LetterReason.GangFavor: return "to ask a favor against a rival gang";
            case LetterReason.PoliticalAlliance: return "to propose a political alliance";
            case LetterReason.RomanticCorrespondence: return "to speak of feelings between us";
            case LetterReason.SpouseCorrespondence: return "as my spouse, writing from afar";
            case LetterReason.CorruptionAttempt: return "to press for sensitive information";
            case LetterReason.Blackmail: return "to threaten exposure unless paid";
            case LetterReason.BirthAnnouncement: return "to announce the birth of a child";
            case LetterReason.ChildSupportRequest: return "to ask for support raising a child";
            case LetterReason.PlayerLetterReply: return "in reply to their letter";
            case LetterReason.AwaitingReply: return "to ask why no reply had come";
            case LetterReason.JealousThreat: return "over jealousy and a romantic entanglement";
            case LetterReason.BastardMotherNote: return "about the child we share";
            case LetterReason.StanceHostility: return "with a word of open hostility";
            case LetterReason.StanceFavor: return "to offer friendship and aid";
            case LetterReason.CompanionFarewell: return "to bid farewell upon leaving service";
            case LetterReason.SpouseDivorceDemand: return "to announce an intent to end our marriage";
            case LetterReason.SpouseDivorceEscalation: return "to announce the marriage was being ended";
            case LetterReason.InterceptionMissed: return "with intent I could not deliver in person";
            case LetterReason.RealmTidings: return "with news of the realm";
            case LetterReason.LeakedCorrespondence: return "having come into possession of an intercepted letter";
            case LetterReason.ServiceOffer: return "to offer them a place in my own service";
            case LetterReason.PrisonerOffer: return "to offer a price for a prisoner they held";
            // A future LetterReason with no mapping yet still gets a plain, truthful fallback rather than
            // an exception thrown deep in a letter-delivery tick.
            default: return "in correspondence";
         }
      }

      #endregion
   }
}

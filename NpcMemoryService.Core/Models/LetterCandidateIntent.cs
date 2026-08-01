// Code written by Gabriel Mailhot, 01/08/2026.
// B2 (mailbox scheduler wiring): the persisted counterpart to CalradiaRemembers.Logic.LetterCandidate. An event
// handler (a battle won, a tournament taken, a birth announced) no longer fires a letter immediately: it records
// its intent to write here, and the mod's daily mailbox scheduler decides, budget and weight in hand, whether
// today is the day this candidate actually gets released.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   A budgeted, deferred letter the daily scheduler may or may not release, unlike a guaranteed
   ///   <see cref="ScheduledLetter" />. Stored on the sending NPC's <see cref="NpcProfile.LetterCandidates" />
   ///   until the scheduler either releases it (generating the real letter and removing the intent) or it is
   ///   dropped outright (the sender died in the meantime).
   /// </summary>
   public sealed class LetterCandidateIntent
   {
      /// <summary>Why the NPC wants to write. Drives the generation prompt, exactly as an immediate letter's reason does.</summary>
      public LetterReason Reason { get; set; }

      /// <summary>The trigger context handed to the generator once the scheduler releases this candidate.</summary>
      public string? Context { get; set; }

      /// <summary>The in-game day this candidate first became eligible to compete for the mailbox.</summary>
      public int EligibleSinceDay { get; set; }

      /// <summary>
      ///   An explicit base weight this candidate should compete with, overriding
      ///   <c>CalradiaRemembers.Logic.LetterWeights.BaseWeight(Reason)</c>. Zero means "derive it from the reason
      ///   as usual"; a battle congratulation instead injects its own underdog-scaled weight here
      ///   (<c>LetterWeights.BattleVictoryWeight</c>), computed once at the moment the battle ended.
      /// </summary>
      public int BaseWeightOverride { get; set; }
   }
}

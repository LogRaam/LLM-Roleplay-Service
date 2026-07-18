// Code written by Gabriel Mailhot, 17/07/2026.
// Courier audit 2.5: a letter an NPC has committed to send at a FUTURE day but has not written yet. Today this is
// only the child-support demand, due a week after a birth. It used to be generated at the moment of birth and
// stamped with a future send day (the "today+7" hack), which meant an LLM failure at that instant lost the
// pension (and its quest) permanently, and put a future day into SentOnDay, confusing the cadence. Persisting the
// INTENT instead, and writing the content on the scheduled day with a bounded retry, fixes both.

namespace NpcMemoryService.Core.Models
{
   /// <summary>An NPC's committed intention to send a letter of <see cref="Reason"/> on <see cref="SendDay"/>.</summary>
   public sealed class ScheduledLetter
   {
      /// <summary>Why the letter will be sent. Drives the generation prompt, exactly as an immediate letter's reason does.</summary>
      public LetterReason Reason { get; set; }

      /// <summary>The in-game day the content should be generated and the letter dispatched (not before).</summary>
      public int SendDay { get; set; }

      /// <summary>The trigger context handed to the generator when the day arrives (e.g. the child and the amount asked).</summary>
      public string? Context { get; set; }

      /// <summary>
      ///   How many times generation has been attempted. Bounded by the host so a persistently-failing LLM
      ///   eventually gives up instead of retrying every day forever.
      /// </summary>
      public int Attempts { get; set; }
   }
}

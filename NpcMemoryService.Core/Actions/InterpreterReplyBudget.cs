// Code written by Gabriel Mailhot, 28/11/2026.
// The action interpreter's reply budget, one number for the game and the bench. Gabriel's chat with Monchug (28/11/2026):
// with deepseek-v4-flash-latest as the interpreter, every ordinary reply (no deed) ran past the old 900 tokens of CHECK lines,
// the reading was cut and asked again at double the budget, and the reply waited about 14 more seconds on screen. Measured
// outside the game the same day (15 readings each): v4-flash-latest cut 3 of 6 ordinary replies (1,074 and 1,252 tokens when
// allowed to finish), v4.1-flash reached 833. Gabriel: "monte-le a 1400". Tokens a reading does not use are not billed.

namespace NpcMemoryService.Core.Actions
{
   /// <summary>How many tokens one reading of the action interpreter may spend, in the game and in the bench alike.</summary>
   public static class InterpreterReplyBudget
   {
      /// <summary>The reply budget. Above the longest reading measured (1,252), so a long CHECK preamble finishes on the first call.</summary>
      public const int Tokens = 1400;
   }
}

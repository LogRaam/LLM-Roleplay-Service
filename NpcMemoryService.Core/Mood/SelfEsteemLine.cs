// Code written by Gabriel Mailhot, 30/09/2026.
// SELF-ESTEEM, increment E3 (ROADMAP, ruled 30/09/2026): how the character regards himself, told to him in the per-turn
// tail beside his day. Lordfadooboo: expectations when speaking to the player, how failure is taken, insecurity that
// wants reassurance, or a man unfazed by difficulty. Ruling 1: colour, never an outcome, said in the line itself.

namespace NpcMemoryService.Core.Mood
{
   /// <summary>Composes the character's self-regard for the prompt. Pure.</summary>
   public static class SelfEsteemLine
   {
      private static readonly string NewLine = System.Environment.NewLine; // as StringBuilder.AppendLine writes it

      /// <summary>The block, or empty for a steady man or none known.</summary>
      public static string Compose(SelfEsteemBand? band)
      {
         string? body = band switch {
            SelfEsteemBand.Broken => "You have been brought low lately, and you know it. You expect little, flinch at slights, and take "
                                    + "failure hard, yours or another's. From someone you care for, you need to hear that you still matter.",
            SelfEsteemBand.Shaken => "Your confidence has been shaken lately. You are quicker to take offence and slower to trust good news, "
                                    + "and reassurance from someone you care for means more than you let show.",
            SelfEsteemBand.Assured => "You are sure of yourself these days. Setbacks are things to be dealt with, not dwelt on.",
            SelfEsteemBand.Proud => "You think well of yourself, perhaps too well. You expect deference, lay failure at others' feet, and "
                                   + "are not easily shaken.",
            _ => null
         };

         if (body == null) return string.Empty;

         return "HOW YOU SEE YOURSELF:" + NewLine + body + NewLine
                + "This colours how you speak and take things, never what you will agree to." + NewLine + NewLine;
      }
   }
}

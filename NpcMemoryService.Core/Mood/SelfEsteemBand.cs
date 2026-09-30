// Code written by Gabriel Mailhot, 30/09/2026.
// Self-esteem (ROADMAP, SELF-ESTEEM, ruled 30/09/2026): what a character thinks of HIMSELF, in words, never a number.
// The mod computes it (CalradiaRemembers.Logic.SelfEsteemPolicy); the prompt and the perceived image speak it.

namespace NpcMemoryService.Core.Mood
{
   /// <summary>How a character regards himself lately. Steady renders nothing: most men are most days.</summary>
   public enum SelfEsteemBand
   {
      /// <summary>Brought low; expects little, flinches at slights.</summary>
      Broken,

      /// <summary>Confidence shaken; quick to take offence, slow to trust good news.</summary>
      Shaken,

      /// <summary>Himself. Renders nothing.</summary>
      Steady,

      /// <summary>Sure of himself; setbacks are dealt with, not dwelt on.</summary>
      Assured,

      /// <summary>Thinks well of himself, perhaps too well; expects deference.</summary>
      Proud
   }
}

// Code written by Gabriel Mailhot, 28/11/2026.
// Stage 4 of the group-scene plan, "generated depth" (Calradia Remembers docs/design/group-chat/integration-plan-2026-11-28.md).
// Gabriel, 28/11/2026: "go pour tout le système ... je testerai demain". In the simulator a character known only from its game
// sheet spoke direct and flat beside a hand-written one; one call that gave it a voice, a mask, a fear, a limit and a private
// secret, from that sheet, brought back the subtext.
//
// Gabriel on the secret (27/11/2026): "Il peut y avoir des secrets personnels complémentaires au jeu.. si ça ne touche pas à une
// mécanique du jeu" (his example: once a lover wanted to be with Cindy, I talked him out of it, and Cindy never knew), and "Le
// problème arrive lorsque le LLM invente quelque chose que le jeu peut contredire." So the secret is private life the game
// cannot check; the host's generator is told so and a second, cheap check drops a secret that touches a tracked fact.
//
// Generated ONCE and kept: never regenerated, or the same character would change nature between two meetings (a review of the
// plan). It FILLS what is empty and COMPLEMENTS what exists: the game's own fear wins over this one, an authored backstory's
// voice over this voice, and the prompt re-checks both every time it is built (an authored text or a rolled fear may come
// after the depth).

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Who a character is beneath their game sheet: how they speak, how they want to be seen, what they fear, what they will
   ///   never do, and one private secret. Additive on <see cref="NpcProfile.GeneratedDepth" />: null on every save that never
   ///   used it, and then nothing in the prompt changes.
   /// </summary>
   public sealed class GeneratedDepth
   {
      /// <summary>Longest voice kept, in characters.</summary>
      public const int MaxVoiceChars = 300;

      /// <summary>Longest mask, fear or limit kept, in characters.</summary>
      public const int MaxTraitChars = 200;

      /// <summary>Longest secret kept, in characters.</summary>
      public const int MaxSecretChars = 400;

      /// <summary>How they speak: rhythm, register, what they never say. Empty when an authored voice existed at generation.</summary>
      public string Voice { get; set; } = "";

      /// <summary>How they want to be seen by others.</summary>
      public string Mask { get; set; } = "";

      /// <summary>What they fear most. Empty when the game gave them a fear of its own at generation (the game's wins).</summary>
      public string Fear { get; set; } = "";

      /// <summary>One thing they will never do.</summary>
      public string Limit { get; set; } = "";

      /// <summary>One private secret the game cannot check. Empty when the check dropped it.</summary>
      public string Secret { get; set; } = "";

      /// <summary>The campaign day it was generated.</summary>
      public int GeneratedOnDay { get; set; }

      /// <summary>Whether it holds anything a prompt would show.</summary>
      public bool HasContent
         => !string.IsNullOrWhiteSpace(Voice) || !string.IsNullOrWhiteSpace(Mask) || !string.IsNullOrWhiteSpace(Fear) || !string.IsNullOrWhiteSpace(Limit)
            || !string.IsNullOrWhiteSpace(Secret);
   }
}

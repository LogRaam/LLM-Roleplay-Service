// Code written by Gabriel Mailhot, 05/09/2026.
// Mystery-lover pillar (design phase 3b): once an NPC holds the player's secret-lover identity (learned only because
// the player confessed it, or because it propagated from one who was told), HOW they carry it is not a coin flip: a
// friend keeps it in trust, an enemy is tempted to trade on it, and a stranger holds it discreetly. This pure policy
// is the single source of that ruling so the prompt the player reads (PromptBuilder.AppendHeldSecret) and any later
// mechanical use (blackmail) can never drift apart. The host gathers the regard and hostility; this decides meaning.

namespace NpcMemoryService.Core.Models
{
   /// <summary>How an NPC who knows the player's secret-lover identity is disposed to carry it.</summary>
   public enum HeldSecretDisposition
   {
      /// <summary>A friend: keeps the secret gladly, may reassure the player it is safe or tease them warmly, never threatens.</summary>
      Confidant,

      /// <summary>Neither friend nor foe: holds it discreetly, neither gossiped idly nor pressed as a weapon.</summary>
      Discreet,

      /// <summary>A foe (at war, or personal enmity): tempted to hold it over the player, hint at exposure, trade on their silence.</summary>
      Leverage
   }

   /// <summary>Decides how an NPC who holds the player's secret-lover identity is disposed to use it, from regard and hostility.</summary>
   public static class HeldSecretPolicy
   {
      /// <summary>At or above this personal regard, the NPC counts the player a real friend and keeps the secret in trust. Tuning.</summary>
      public const int ConfidantRegardFloor = 20;

      /// <summary>At or below this personal regard, quiet distrust curdles into temptation to use the secret. Tuning.</summary>
      public const int LeverageRegardCeiling = -10;

      /// <summary>
      ///   The disposition. War trumps regard (an enemy at war will weigh a lever whatever their private feeling),
      ///   then personal enmity, then genuine friendship; everything between is discreet neutrality.
      /// </summary>
      public static HeldSecretDisposition Decide(int regard, bool hostile)
      {
         if (hostile || regard <= LeverageRegardCeiling) return HeldSecretDisposition.Leverage;
         if (regard >= ConfidantRegardFloor) return HeldSecretDisposition.Confidant;

         return HeldSecretDisposition.Discreet;
      }
   }
}

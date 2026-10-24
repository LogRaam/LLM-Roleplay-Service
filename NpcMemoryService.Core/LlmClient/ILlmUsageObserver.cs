// Code written by Gabriel Mailhot, 24/10/2026.
// What a call COST, as the provider billed it. fearente (Nexus, 24/10/2026) spent $3.19 in a week where the mod's page
// promised about $1, and nothing in the mod could say where it went: the wire observer sees requests only, and the
// client's own retries overwrote the first attempt's usage. This seat sees every billed attempt, retries included.

#region

using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.LlmClient
{
   /// <summary>
   ///   Sees every HTTP attempt a client makes, with what the provider reported it cost. Null in normal play: nothing is
   ///   measured unless somebody listens (the mod's cr.cost_bench).
   /// </summary>
   public interface ILlmUsageObserver
   {
      /// <summary>One attempt, as sent and as answered. A failed attempt carries no usage (an error is not billed).</summary>
      void OnAttempt(string? model, LlmResponse response);
   }
}

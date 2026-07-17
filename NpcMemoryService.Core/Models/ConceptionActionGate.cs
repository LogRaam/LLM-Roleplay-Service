// Code written by Gabriel Mailhot, 17/07/2026.
// Romance audit M-G1: impregnation_risk is the single most consequential LLM action (it can start a real
// pregnancy) yet the bridge executed it unconditionally, so a hallucinated emission at a content level that never
// TAUGHT the action, or in a non-intimate turn, could conceive a child out of nothing. The prompt only teaches
// impregnation_risk at AdultContentLevel.Explicit or above; this pure gate lets the bridge RE-VALIDATE that same
// bar ("the prompt is advice, the bridge is law") so the action can never outrun the content level that permits
// it. Kept as a named, tested rule so the bridge guard and the teaching gate can never silently drift apart.

namespace NpcMemoryService.Core.Models
{
    /// <summary>Whether the mechanical conception action (impregnation_risk) may fire at a given content level.</summary>
    public static class ConceptionActionGate
    {
        /// <summary>
        ///   True only at <see cref="AdultContentLevel.Explicit" /> or above, exactly the bar at which the prompt
        ///   teaches impregnation_risk. At Off or Mature the action was never offered, so a bridge that honours
        ///   this can never turn a hallucinated emission into a pregnancy.
        /// </summary>
        public static bool PermitsConception(AdultContentLevel level)
            => level >= AdultContentLevel.Explicit;
    }
}

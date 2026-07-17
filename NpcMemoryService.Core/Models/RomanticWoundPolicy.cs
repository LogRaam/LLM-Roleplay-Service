// Code written by Gabriel Mailhot, 17/07/2026.
// Romance audit M-J5: when the player betrays a partner (courts another before their eyes), the jealousy system
// wounded that partner's REGARD and planted a grievance, but never touched their romantic STATUS, so a betrayed
// spouse or partner stayed "Intimate"/"Committed" as if nothing had happened, an incoherence the arc itself
// otherwise models on a Betrayal event. This pure rule maps a betrayed partner's bond to Estranged ("trust
// broken but feeling remains"), NOT the terminal Broken: a first betrayal wounds, it does not end for good, and
// Estranged is now recoverable (see the reconciliation path in ProfileMutator.AdvanceRomanticStatus).

namespace NpcMemoryService.Core.Models
{
    /// <summary>How a betrayed partner's romantic status changes when they learn of the player's wandering.</summary>
    public static class RomanticWoundPolicy
    {
        /// <summary>
        ///   The status a betrayed partner falls to after a jealousy wound: an active positive bond (courting or
        ///   deeper) becomes <see cref="RomanticStatus.Estranged" /> (recoverable), while a bond that is only
        ///   budding (None/Curious) or already damaged (Estranged/Broken) is left as it is, so the wound neither
        ///   invents a relationship to break nor pushes an already-ended one further.
        /// </summary>
        public static RomanticStatus AfterJealousyWound(RomanticStatus current)
        {
            switch (current)
            {
                case RomanticStatus.Courting:
                case RomanticStatus.Intimate:
                case RomanticStatus.SecretLover:
                case RomanticStatus.Committed:
                    return RomanticStatus.Estranged;
                default:
                    return current;
            }
        }
    }
}

// Code written by Gabriel Mailhot, 24/05/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Current state of the romantic arc between this NPC and the player.
    ///   Mutable — evolves through conversations and notable events.
    /// </summary>
    public enum RomanticStatus
    {
        None,         // No romantic interaction so far
        Curious,      // Noticed the player, intrigued but distant
        Courting,     // Active romantic interest, meaningful exchanges
        Intimate,     // Physically close (exclusive or not depending on preferences)
        SecretLover,  // Intimate with the player while married to another — kept hidden
        Committed,    // Long-term bond — marriage or its equivalent
        Estranged,    // Trust broken but feeling remains
        Broken        // Done. No path back.
    }
}

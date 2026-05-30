// Code written by Gabriel Mailhot, 24/05/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Relational dynamics that shape how an NPC approaches romance.
    ///   These are distinct from <see cref="Kink"/> (which describes intimate
    ///   specifics) — these describe the broader pattern of attachment.
    /// </summary>
    public enum RomanticPreference
    {
        None,

        // ── Power and structure ──────────────────────────────────────────
        Dominant,             // Leads in the relationship dynamic
        Submissive,           // Yields, follows the partner's lead
        Switch,               // Comfortable in either role depending on context

        // ── Commitment patterns ──────────────────────────────────────────
        MonogamousStrict,     // Exclusive, faithful, expects the same
        MonogamousFlexible,   // Prefers exclusivity but tolerates discretion
        Polyamorous,          // Comfortable with multiple committed partners
        Casual,               // Prefers brief, unattached entanglements

        // ── Emotional signature ──────────────────────────────────────────
        Possessive,           // Marks, claims, expects visible loyalty
        Independent,          // Values space, time apart, separate lives
        Devoted,              // Pours self into the partner, lives for them
        Reserved,             // Slow to attach, careful with vulnerability

        // ── Pace ──────────────────────────────────────────────────────────
        SlowBurn,             // Builds attraction over time and tested ground
        Intense,              // Fast, all-consuming, immediate
    }
}

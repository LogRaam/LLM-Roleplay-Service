// Code written by Gabriel Mailhot, 24/05/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Controls how much of an NPC's romantic profile is injected into
    ///   the LLM system prompt. Set by the consumer (mod) according to the
    ///   player's preference.
    ///
    ///   Off       → No romantic content at all, including the relational
    ///               sketch. The romantic profile is simply not surfaced.
    ///   Mature    → Relational sketch only. Romance and attraction may
    ///               appear in conversation, but no explicit content.
    ///   Explicit  → Adds the intimate sketch. Sexuality is on the table,
    ///               with the NPC's specific patterns (dominant, possessive,
    ///               etc.). Vanilla in tone.
    ///   Hardcore  → Adds individual kinks (sadism, bondage, exhibitionism,
    ///               etc.) when the NPC has them. Most NPCs do not.
    /// </summary>
    public enum AdultContentLevel
    {
        Off,
        Mature,
        Explicit,
        Hardcore
    }
}

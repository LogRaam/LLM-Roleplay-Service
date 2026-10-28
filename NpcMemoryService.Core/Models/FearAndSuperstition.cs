// Code written by Gabriel Mailhot, 27/10/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   What a character fears (Gabriel, 27/10/2026: "nous pourrions aussi ajouter certaines superstitions et phobies").
    ///   Many fear nothing worth naming. Saved as integers: APPEND ONLY, never insert or reorder (FearOrdinalTests).
    /// </summary>
    public enum Fear
    {
        None,
        TheSea,        // Deep water, ships, drowning
        Fire,          // Flames, a burning hall
        Wolves,        // Wolves and the howl at night
        TheDark,       // Night without a light
        Sickness,      // Fever, plague, a sickroom
        Heights,       // Walls, cliffs, high towers
        Snakes,        // Snakes and what crawls
        ClosedSpaces,  // Cells, tunnels, narrow rooms
    }

    /// <summary>
    ///   A superstition a character keeps (Gabriel, 27/10/2026). Beliefs, never magic: Calradia has none (world.txt).
    ///   Saved as integers: APPEND ONLY, never insert or reorder (SuperstitionOrdinalTests).
    /// </summary>
    public enum Superstition
    {
        None,
        Omens,       // Reads birds and signs before a battle or a journey
        Charms,      // Never goes without a lucky charm
        Ancestors,   // The dead watch and judge the living
        EvilEye,     // Ill-wishing and curses
        Dreams,      // Dreams are portents
        Rituals,     // A small ritual before every fight or journey
        Moon,        // The moon decides good days and bad
        Thresholds,  // Iron or salt at the door against ill luck
    }

    /// <summary>The reserved [DISCOVERY] keys that mark a fear or a superstition as learned.</summary>
    public static class NatureDiscovery
    {
        /// <summary>Marks the character's fear as learned.</summary>
        public const string FearKey = "fear";

        /// <summary>Marks the character's superstition as learned.</summary>
        public const string SuperstitionKey = "superstition";
    }
}

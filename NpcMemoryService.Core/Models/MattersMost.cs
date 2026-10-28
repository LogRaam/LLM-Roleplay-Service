// Code written by Gabriel Mailhot, 27/10/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   What matters most to a character, above everything else (Gabriel, 27/10/2026: "qu'est-ce qui compte le plus pour
    ///   un NPC. Ses enfants? Sa richesse? Son amoureux?"). Chosen by the host among what is TRUE of them (one who cherishes
    ///   their children has children) and weighed by their traits; it moves only when the world takes its ground away.
    ///
    ///   Saved as integers: APPEND ONLY, never insert or reorder (MattersMostOrdinalTests).
    /// </summary>
    public enum MattersMost
    {
        None,
        Children,     // Their children
        Spouse,       // Their spouse, who is not the player
        Lover,        // Their lover, who is not the player
        ThePlayer,    // The player, their spouse or lover
        Kin,          // Their parents, brothers and sisters
        HouseHonor,   // The honour of their house
        Wealth,       // What they own and what they can gain
        Power,        // Rank, fiefs, the ear of rulers
        Glory,        // Glory in battle
        Lands,        // Their lands and the people on them
        Freedom,      // No lord, no oath, no walls
        Vengeance,    // A score they mean to settle
    }

    /// <summary>How the player learns what matters most to a character.</summary>
    public static class MattersMostDiscovery
    {
        /// <summary>The reserved [DISCOVERY] key that marks what matters most to a character as learned.</summary>
        public const string Key = "matters_most";
    }
}

// Code written by Gabriel Mailhot, 27/10/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   A kind of gift a character may love or find cold (Gabriel, 27/10/2026: "un NPC pourrait [...] preferer certains
    ///   cadeaux [...] Une Dame pourrait aimer les fourrures"). The host reads it from the item's own category.
    ///   Saved as integers: APPEND ONLY, never insert or reorder (GiftKindOrdinalTests).
    /// </summary>
    public enum GiftKind
    {
        None,
        Furs,        // fur
        Wine,        // wine
        Ale,         // beer
        Jewelry,     // jewelry
        Velvet,      // velvet
        FineCloth,   // linen, cloth, cotton
        Horses,      // a horse, a war horse, a noble horse
        Weapons,     // swords, bows, shields
        Armor,       // garments and armour
        Delicacies,  // cheese, dates, olives, grapes, butter, meat
        Silver,      // silver
    }
}

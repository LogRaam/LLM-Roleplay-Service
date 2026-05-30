// Code written by Gabriel Mailhot, 24/05/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Specific intimate interests that color an NPC's sexuality beyond
    ///   the broader <see cref="RomanticPreference"/>. Surfaced in prompts
    ///   only when <see cref="AdultContentLevel"/> is <see cref="AdultContentLevel.Hardcore"/>.
    ///
    ///   Most NPCs have no kinks at all; some have 1–3, rarely more.
    ///   Assignment is seeded by <c>Hero.StringId</c> for determinism and
    ///   partially correlated with the NPC's traits.
    /// </summary>
    public enum Kink
    {
        None,

        // ── Power dynamics ────────────────────────────────────────────────
        Dominance,            // Takes pleasure in control
        Submission,           // Finds peace in surrender
        SwitchTendencies,     // Moves fluidly between leading and following

        // ── Sensation ─────────────────────────────────────────────────────
        Sadism,               // Inflicting deliberate pain on a willing partner
        Masochism,            // Receiving sensation as clarity, release

        // ── Restraint ─────────────────────────────────────────────────────
        BondageGiving,        // The art of binding a partner
        BondageReceiving,     // The peace of being bound

        // ── Roleplay and contexts ─────────────────────────────────────────
        Roleplay,             // Performed identities, scenarios
        PowerImbalance,       // Noble/servant, conqueror/captive dynamics

        // ── Observation ───────────────────────────────────────────────────
        Exhibitionism,        // Pleasure in being watched
        Voyeurism,            // Pleasure in observing

        // ── Affection patterns ────────────────────────────────────────────
        Possessiveness,       // Marking, claiming, leaving proof
        PublicAffection,      // Open displays of attachment
    }
}

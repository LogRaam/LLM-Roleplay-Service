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

        // ── Control & denial (Sprint 17 expansion) ────────────────────────
        OrgasmControl,        // Granting, denying, or drawing out release
        Chastity,             // Enforced denial, release earned
        FreeUse,              // The partner is available to use at will

        // ── Humiliation, objectification & praise ─────────────────────────
        Degradation,          // Verbal and postural abasement
        Objectification,      // Treating the partner as an object or possession
        PetPlay,              // Collar, leash, the partner as a creature
        Praise,               // Devotion and praise as reward and weapon

        // ── Pain & sensation (extends Sadism/Masochism) ───────────────────
        ImpactPlay,           // Spanking, flogging, the cane
        SensoryDeprivation,   // Blindfolds, hoods, removing the senses
        FearPlay,             // The charge of the partner's fear

        // ── Role & fantasy (medieval-fitting) ─────────────────────────────
        MasterSlave,          // Ownership as a persistent dynamic
        Breeding,             // Impregnation, dynastic claiming
        Training,             // Conditioning the partner over time
        CorruptionKink,       // The pleasure of corrupting the virtuous
        Prize,                // The partner as a trophy, won and displayed
    }
}

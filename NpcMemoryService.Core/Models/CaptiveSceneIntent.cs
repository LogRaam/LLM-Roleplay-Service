// Code written by Gabriel Mailhot, 06/06/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   The captor's intent in a captive encounter. Drives the opening scene cue
    ///   and the specific framing injected into the CNC prompt section.
    ///   Selected by <c>CaptiveEncounterService</c> from the captor's relation score
    ///   and kinks; overridable at runtime via the <c>cr.crc_scene</c> console command.
    /// </summary>
    public enum CaptiveSceneIntent
    {
        /// <summary>Information gathering, leverage, or utility assessment.</summary>
        Interrogation,

        /// <summary>Personal or sexual summons — beyond official custody.</summary>
        PersonalDesire,

        /// <summary>Establishing dominance, control, and subjugation.</summary>
        Domination,

        /// <summary>Physical or psychological harm — punishment, sadism, or coercion.</summary>
        Torture,

        /// <summary>
        ///   Long-term conditioning. Not a single night's encounter but one session in an
        ///   ongoing process of breaking in and training the captive toward obedience.
        /// </summary>
        Training,

        /// <summary>
        ///   The positive register — the carrot after the stick. The captor summons the
        ///   prisoner to reward cooperation with praise, comfort, pleasure, or privilege.
        /// </summary>
        Reward,

        // ── Bandit / pirate intents (non-sexual menace). Appended last to preserve save
        //    ordinals. Used for a synthesized brigand captor; see CaptiveEncounterService. ──

        /// <summary>Shake the prisoner down for denars, ransom, or some advantage.</summary>
        Extortion,

        /// <summary>Pure menace — make the prisoner feel utterly at the mercy of lawless thugs.</summary>
        Intimidation,

        /// <summary>Payback — the captors lost men to the player (or their kind) and want their due.</summary>
        Revenge
    }
}

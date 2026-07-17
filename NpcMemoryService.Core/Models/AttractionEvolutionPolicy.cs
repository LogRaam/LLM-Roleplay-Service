// Code written by Gabriel Mailhot, 17/07/2026.
// Romance audit M-R5: RomanticProfile.AttractionToPlayer is documented "evolves through conversations" and is
// shown to the LLM, yet nothing but a court duel ever moved it, so it sat frozen at 0 for almost every NPC (a
// misleading prompt line) and the jealousy SpurnedAdmirer branch, which needs attraction >= 55, was effectively
// dead. This pure rule makes conversation move it at last: warm acts (flirt, intimacy) raise it, wounds
// (betrayal, quarrel) lower it, and court duels keep adding on top. Positive gains follow the SAME plausibility
// gate as the romantic arc (an orientation-implausible pair does not grow attracted); losses apply regardless.

namespace NpcMemoryService.Core.Models
{
    /// <summary>How a notable event moves an NPC's <see cref="RomanticProfile.AttractionToPlayer" />.</summary>
    public static class AttractionEvolutionPolicy
    {
        /// <summary>A flirt is a small, tentative pull. Tuning.</summary>
        public const int FlirtGain = 4;

        /// <summary>Intimacy is a stronger deepening of desire. Tuning.</summary>
        public const int IntimacyGain = 8;

        /// <summary>A betrayal is the sharpest blow to desire. Tuning.</summary>
        public const int BetrayalLoss = -20;

        /// <summary>A quarrel cools desire, less sharply than a betrayal. Tuning.</summary>
        public const int ConflictLoss = -10;

        /// <summary>
        ///   The signed change to attraction for <paramref name="type" />. POSITIVE gains (flirt, intimacy)
        ///   require an orientation-plausible pair, exactly the gate the romantic arc uses (M-R2 parity): an
        ///   implausible pair, an LLM hallucination, grows no attraction. NEGATIVE changes (betrayal, quarrel)
        ///   apply regardless of plausibility, since a wound cools desire whatever the pairing. Every other event
        ///   leaves attraction untouched.
        /// </summary>
        public static int Delta(NotableEventType type, bool orientationCompatible)
        {
            switch (type)
            {
                case NotableEventType.Flirt:    return orientationCompatible ? FlirtGain : 0;
                case NotableEventType.Intimacy: return orientationCompatible ? IntimacyGain : 0;
                case NotableEventType.Betrayal: return BetrayalLoss;
                case NotableEventType.Conflict: return ConflictLoss;
                default:                        return 0;
            }
        }
    }
}

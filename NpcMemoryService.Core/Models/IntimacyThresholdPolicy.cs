// Code written by Gabriel Mailhot, 18/07/2026.
// Adult-prompt audit M3. The consent thresholds (>=5 / >=10 / >=20 / >=30) existed ONLY as prose in the
// prompt, and the regard they had to be compared against was printed some three thousand tokens earlier
// under CURRENT STANCE. A weaker model had to remember a number from far away and do arithmetic on it, so
// an 8B model at +12 facing a threshold of 20 simply yielded. This policy is the single source of that
// rule: the prompt asks it for a VERDICT and prints the conclusion instead of the arithmetic, and the
// enforcement points (the intimacy relation bonus, the romantic arc) ask it the same question, so the
// text the player reads and the mechanic that runs can never drift apart.

namespace NpcMemoryService.Core.Models
{
    /// <summary>Where an NPC stands relative to the regard their own disposition requires for intimacy.</summary>
    public enum IntimacyConsentVerdict
    {
        /// <summary>
        ///   No stranger-gate applies: this NPC is the player's own spouse, and their vows already bind them
        ///   to this very person. Character and mood decide, never a trust threshold meant for strangers.
        /// </summary>
        Exempt,

        /// <summary>The regard their disposition asks for has been earned.</summary>
        Met,

        /// <summary>
        ///   Below the threshold, but these two HAVE been lovers before. Mechanically identical to
        ///   <see cref="Below" />; it exists so the refusal can be written as a withdrawal rather than as a
        ///   stranger's refusal. Someone who has shared your bed does not answer an advance as though you
        ///   had never met, and playing it that way reads as the amnesia the whole mod is built against.
        /// </summary>
        BelowWithHistory,

        /// <summary>Below the threshold, with no shared history to soften it.</summary>
        Below
    }

    /// <summary>The facts a consent verdict rests on. The host gathers them; this decides what they mean.</summary>
    public sealed class IntimacyConsentFacts
    {
        /// <summary>This NPC is married to the PLAYER (not merely married).</summary>
        public bool NpcIsPlayerSpouse { get; set; }

        /// <summary>This NPC is married to somebody other than the player, so intimacy would be infidelity.</summary>
        public bool NpcIsMarriedToAnother { get; set; }

        /// <summary>The NPC is open to brief, unattached encounters.</summary>
        public bool PrefersCasual { get; set; }

        /// <summary>The NPC's desire moves fast when the spark is real.</summary>
        public bool PrefersIntense { get; set; }

        /// <summary>The NPC's PERSONAL regard for the player (the mod's own ledger, not the clan relation).</summary>
        public int RegardWithPlayer { get; set; }

        /// <summary>
        ///   These two have already been intimate. The caller decides what counts as evidence (an
        ///   Intimate-or-deeper romantic status, a recorded intimacy memory); the policy only asks whether
        ///   there is a past to be tender about.
        /// </summary>
        public bool HasSharedIntimacyBefore { get; set; }

        /// <summary>
        ///   How much the host's relationship-pacing dial lowers the bar (0 at the level the mod was
        ///   balanced around). Positive values make intimacy easier. The host owns the mapping, because the
        ///   dial is a mod-side player setting the SDK cannot see.
        /// </summary>
        public int ThresholdRelief { get; set; }
    }

    /// <summary>
    ///   Decides the regard an NPC requires before physical intimacy, and how far the player is from it.
    ///   Dispositions are checked in the SAME order the consent prose has always used, so this changes no
    ///   existing outcome: a spouse is exempt, then marriage to another (the strictest bar), then a casual
    ///   disposition (the most permissive), then an intense one, then the ordinary case.
    /// </summary>
    public static class IntimacyThresholdPolicy
    {
        /// <summary>Intimacy with a married NPC is infidelity, and asks the deepest trust of all. Tuning.</summary>
        public const int MarriedToAnotherThreshold = 30;

        /// <summary>A casual disposition asks only comfort and attraction. Tuning.</summary>
        public const int CasualThreshold = 5;

        /// <summary>An intense disposition burns fast, but still needs the pull to be real. Tuning.</summary>
        public const int IntenseThreshold = 10;

        /// <summary>The ordinary case: connection built across several meetings. Tuning.</summary>
        public const int StandardThreshold = 20;

        /// <summary>
        ///   However generous the pacing dial, the bar never falls to nothing: an NPC who actively dislikes
        ///   the player must never read as consenting. Relief loosens the climb, it does not remove the gate.
        /// </summary>
        public const int MinimumThreshold = 1;

        /// <summary>
        ///   The regard this NPC requires before physical intimacy. Meaningless for the player's own spouse
        ///   (see <see cref="IntimacyConsentVerdict.Exempt" />), for whom the caller should not print a bar
        ///   at all; the value returned in that case is simply the ordinary one.
        /// </summary>
        public static int Threshold(IntimacyConsentFacts f)
        {
            if (f == null) return StandardThreshold;

            int baseline =
                f.NpcIsMarriedToAnother ? MarriedToAnotherThreshold
                : f.PrefersCasual ? CasualThreshold
                : f.PrefersIntense ? IntenseThreshold
                : StandardThreshold;

            int relieved = baseline - f.ThresholdRelief;

            return relieved < MinimumThreshold ? MinimumThreshold : relieved;
        }

        /// <summary>
        ///   Where the player stands against that bar. <see cref="IntimacyConsentVerdict.BelowWithHistory" />
        ///   and <see cref="IntimacyConsentVerdict.Below" /> are the SAME mechanical answer (no); they differ
        ///   only in how the refusal should be voiced.
        /// </summary>
        public static IntimacyConsentVerdict Resolve(IntimacyConsentFacts f)
        {
            if (f == null) return IntimacyConsentVerdict.Below;
            if (f.NpcIsPlayerSpouse) return IntimacyConsentVerdict.Exempt;
            if (f.RegardWithPlayer >= Threshold(f)) return IntimacyConsentVerdict.Met;

            return f.HasSharedIntimacyBefore
                ? IntimacyConsentVerdict.BelowWithHistory
                : IntimacyConsentVerdict.Below;
        }

        /// <summary>
        ///   Whether intimacy may mechanically COUNT this turn (the relation bonus, the romantic arc).
        ///   Deliberately blind to shared history: a past affair explains a gentler refusal, it never buys a
        ///   mechanical exemption, or a single low-regard encounter would unlock the NPC for good.
        /// </summary>
        public static bool PermitsIntimacy(IntimacyConsentFacts f)
        {
            IntimacyConsentVerdict verdict = Resolve(f);

            return verdict == IntimacyConsentVerdict.Exempt || verdict == IntimacyConsentVerdict.Met;
        }
    }
}

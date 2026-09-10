// Code written by Gabriel Mailhot, 09/09/2026.
// Gabriel's ruling, 09/09/2026: the model must declare already KNOWING the gating, never be refuted after it has
// spoken. This is that rule applied to the axis that never had it, which is what the players were describing.
//
// yozakura12 (Nexus, 09 Sep 2026): "Sometimes, an NPC will confess their feelings to me even when my intimacy
// level with them isn't high enough. It creates a disconnect: the NPC is expressing affection, yet the system
// displays a message stating that intimacy is insufficient... Could they be programmed to avoid intimate-sounding
// words or actions when the affection level is low?" And, naming the exact shape of it: "in Grounded, an NPC might
// confess their feelings to you even when your affinity level is only around 10. They might say something like,
// 'We've been travelling together for a year, so I've come to see you as someone special', only for a message to
// pop up saying your affinity isn't high enough."
//
// This was never the model being probabilistic. Three things were true at once, and all three were ours:
//   1. Regard reached the prompt as a COLOUR and never as a boundary. "Your own personal regard for this player
//      (+10): cordial regard", followed by "let your personal regard color your warmth and candor". Nothing
//      anywhere said what this NPC would NOT say yet, so there was nothing to hold the model back.
//   2. AppendSocialAttractionInstructions fires on clan tier >= 3 or arena rank <= 10 with NO regard gate at all,
//      and tells the NPC "You may raise the subject of a deeper bond, courtship, a potential match... Do not force
//      it, but do not wait for them to bring it up either." A player with a tier-3 clan was instructing every
//      compatible NPC in Calradia to open the subject, at any regard whatsoever.
//   3. Attraction is a SECOND axis moving independently of regard (duels, notable events), so "Genuinely
//      attracted" could print beside a regard of 10.
//
// The mod had already solved this exact problem once, on the neighbouring axis. IntimacyThresholdPolicy (adult
// audit M3, 18/07/2026) says it in its own header: "a weaker model had to remember a number from far away and do
// arithmetic on it, so an 8B model at +12 facing a threshold of 20 simply yielded". The answer then was to state a
// VERDICT rather than the arithmetic, and to have the enforcement points ask the very same question. That
// treatment went to the PHYSICAL axis. The EMOTIONAL one never got it. This file is that, and it is deliberately
// built in the same shape, in the same folder, with the same host-owns-the-dial split.
//
// EVERY NUMBER HERE IS AN ANCHOR, NOT AN INVENTION. That is the point, after the 2026-09-08 regression where one
// affection bar lived as a literal 50 in three places and moving one of them opened a window in which a companion
// declared their feelings and the game then refused the bond. There are no new balance figures below: the
// declaration bar is supplied BY THE HOST so it is the same number the confession roll uses (and therefore follows
// the Relationship Pacing dial), and the two floors are lifted from the two tables the prompt already prints from.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   How far this NPC may go in what they SAY to the player about feeling, right now. Not what they may DO:
    ///   physical consent is <see cref="IntimacyThresholdPolicy" />, and the two are deliberately separate axes,
    ///   because desire and trust are separate things and a character may hold one without the other.
    /// </summary>
    public enum RomanticRegister
    {
        /// <summary>
        ///   No romantic footing at all. Warmth, friendship, loyalty, all of it available; a romantic claim of
        ///   any kind is not. This is also where the whole arc sits when adult content is off or the player falls
        ///   outside this NPC's attraction, so a campaign that switched romance off never hears a word of it.
        /// </summary>
        Platonic,

        /// <summary>
        ///   They notice the player. They may flirt, tease, let the attraction show, and answer an advance
        ///   warmly. They may NOT speak of love, of a bond, or of a shared future: those are claims about a
        ///   depth that is not there yet, and they are exactly what the player then sees the game refuse.
        /// </summary>
        Interest,

        /// <summary>
        ///   The bond is real and deep enough to be named. They may say what the player has come to mean to
        ///   them, may confess, may speak of a future. This is the same bar the confession roll uses, supplied by
        ///   the host so the two can never disagree.
        /// </summary>
        Attachment,

        /// <summary>
        ///   A bond already exists (the player's own spouse, a secret lover, an openly committed partner). They
        ///   speak as a lover, and no bar stands between them and their own feelings.
        /// </summary>
        Bound
    }

    /// <summary>The facts the register rests on. The host gathers them; this decides what they mean.</summary>
    public sealed class RomanticRegisterFacts
    {
        /// <summary>The whole romantic arc is switched on for this campaign.</summary>
        public bool AdultContentEnabled { get; set; }

        /// <summary>The player falls within this NPC's attraction (orientation, already resolved by the caller).</summary>
        public bool PlayerIsCompatible { get; set; }

        /// <summary>The mod's own personal regard ledger, never the vanilla clan relation.</summary>
        public int RegardWithPlayer { get; set; }

        /// <summary>The separate desire axis, which moves on its own through duels and notable events.</summary>
        public int AttractionToPlayer { get; set; }

        /// <summary>Where the romantic arc stands.</summary>
        public RomanticStatus Status { get; set; }

        /// <summary>This NPC is married to the PLAYER, which settles the question before any bar is consulted.</summary>
        public bool NpcIsPlayerSpouse { get; set; }

        /// <summary>
        ///   The regard at which feelings may be DECLARED, supplied by the host rather than held here, because it
        ///   is the same number the host's own confession roll measures from and it follows a mod-side dial the
        ///   SDK cannot see. Passing the resolved bar rather than a relief is deliberate: the SDK then cannot
        ///   hold a second opinion about it, which is the whole lesson of the 2026-09-08 regression.
        /// </summary>
        public int DeclaredAffectionBar { get; set; } = RomanticRegisterPolicy.DefaultDeclaredAffectionBar;
    }

    /// <summary>What this NPC may and may not say about feeling, decided once and asked by everyone who needs it.</summary>
    public static class RomanticRegisterPolicy
    {
        /// <summary>
        ///   The shipped Grounded bar for declaring feelings, and the default when a host supplies none, so an
        ///   older host sees exactly the balance it always had. ANCHOR, not a new figure: it is
        ///   CompanionSecretAffectionPolicy.MinRegard mod-side, the bar the confession roll has always used.
        /// </summary>
        public const int DefaultDeclaredAffectionBar = 50;

        /// <summary>
        ///   Where regard alone first licenses a flirtation. ANCHOR: RegardBands' first positive tier, "cordial
        ///   regard" (>= 5). Below it the encyclopedia already tells the player this NPC is merely neutral toward
        ///   them, and an NPC who is neutral does not flirt.
        /// </summary>
        public const int InterestRegardFloor = 5;

        /// <summary>
        ///   Where desire alone first licenses a flirtation, even from someone who does not yet trust the player.
        ///   ANCHOR: PromptBuilder.DescribeAttraction's own first positive band, "A growing interest" (>= 10).
        /// </summary>
        public const int InterestAttractionFloor = 10;

        /// <summary>
        ///   The register, tried in the order the arc actually runs: the whole thing off, then a bond that
        ///   already exists, then a romance that has ended, then depth, then interest.
        ///   <para>
        ///     An ENDED romance (estranged, broken) returns to <see cref="RomanticRegister.Platonic" />: no new
        ///     claim may be made on a bond that is over. That governs only what may be newly DECLARED, never what
        ///     is remembered; the romantic status prose still renders, so an NPC never answers a former lover as
        ///     though they had never met, which is the amnesia this whole mod exists against.
        ///   </para>
        /// </summary>
        public static RomanticRegister Resolve(RomanticRegisterFacts? f)
        {
            if (f == null) return RomanticRegister.Platonic;
            if (!f.AdultContentEnabled) return RomanticRegister.Platonic;

            // A spouse is bound whatever their orientation says about strangers, and whatever the bar says.
            if (f.NpcIsPlayerSpouse) return RomanticRegister.Bound;
            if (!f.PlayerIsCompatible) return RomanticRegister.Platonic;

            if (f.Status == RomanticStatus.SecretLover || f.Status == RomanticStatus.Committed)
                return RomanticRegister.Bound;

            if (f.Status == RomanticStatus.Estranged || f.Status == RomanticStatus.Broken)
                return RomanticRegister.Platonic;

            if (f.RegardWithPlayer >= Bar(f)) return RomanticRegister.Attachment;

            return f.RegardWithPlayer >= InterestRegardFloor || f.AttractionToPlayer >= InterestAttractionFloor
                ? RomanticRegister.Interest
                : RomanticRegister.Platonic;
        }

        /// <summary>
        ///   The declaration bar actually in force, never below <see cref="InterestRegardFloor" />: however
        ///   generous a host's pacing dial, declaring love must still sit above merely noticing someone, or the
        ///   two registers collapse into one and the ladder stops meaning anything.
        /// </summary>
        public static int Bar(RomanticRegisterFacts? f)
        {
            int bar = f?.DeclaredAffectionBar ?? DefaultDeclaredAffectionBar;

            return bar < InterestRegardFloor ? InterestRegardFloor : bar;
        }

        /// <summary>
        ///   Whether this NPC may raise a bond, a courtship, or a match on their OWN initiative. The gate that
        ///   was missing from the clan-standing and tournament-fame nudges, which invited exactly that with no
        ///   regard condition of any kind and are the likeliest source of yozakura12's report.
        /// </summary>
        public static bool MayInitiateCourtship(RomanticRegisterFacts? f)
        {
            RomanticRegister register = Resolve(f);

            return register == RomanticRegister.Attachment || register == RomanticRegister.Bound;
        }
    }
}

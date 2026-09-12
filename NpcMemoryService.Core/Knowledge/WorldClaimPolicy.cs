// Code written by Gabriel Mailhot, 12/09/2026.
// Invented facts, increment 1: the gate. See WorldClaim for Gabriel's two rulings.
//
// This is the only thing standing between a language model and the shared world state, so it refuses by
// default and every acceptance is a named reason.

#region

using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>Why a claim was refused, so a refusal can be counted and read rather than merely happening.</summary>
    public enum ClaimVerdict
    {
        /// <summary>It may enter the world as a rumour.</summary>
        Accepted,

        /// <summary>Nobody said it, or nobody who counts. A claim with no speaker has no author.</summary>
        NoSpeaker,

        /// <summary>The player said it. Gabriel: the player suffers the world, the NPCs define it.</summary>
        FromThePlayer,

        /// <summary>It is about the player, whose deeds already have their own road into the world.</summary>
        AboutThePlayer,

        /// <summary>The engine owns this subject, so an invention here is a lie the player can check.</summary>
        EngineOwnsIt,

        /// <summary>Not classified, and an unclassifiable claim cannot be judged.</summary>
        Unclassified,

        /// <summary>Nothing to say, or too much of it to be news.</summary>
        NotSayable,

        /// <summary>The world already carries this. Saying it twice does not make it truer.</summary>
        AlreadyKnown
    }

    /// <summary>
    ///   Decides whether a thing an NPC said may become a thing that is true. Pure, so the rule can be read and
    ///   argued with on its own, without a campaign running.
    /// </summary>
    public static class WorldClaimPolicy
    {
        /// <summary>
        ///   The longest a promoted claim may be. A rumour is a sentence; anything longer is the model writing
        ///   fiction into the save, and it has to be read back to strangers later.
        /// </summary>
        public const int MaxTextChars = 180;

        /// <summary>The shortest that can carry news at all.</summary>
        public const int MinTextChars = 12;

        /// <summary>
        ///   How far a promoted claim travels, on the awareness scale the rumour pipeline already uses.
        ///   <para>
        ///     CORRECTED 2026-09-12, and the mistake is worth keeping. This was 2, chosen on the reasoning that
        ///     "an invented rumour must never outrank a battle" - the right instinct applied to the wrong
        ///     scale. Magnitude is not a RANK, it is a REACH in points: awareness is
        ///     (magnitude + 25 if the observer's faction is touched + up to 25 for proximity) x recency, and
        ///     the threshold to have heard anything at all is 50. At 2, a claim maxed out at 52 - heard only
        ///     by somebody of the same faction standing on the doorstep on the very day. In practice, nobody,
        ///     ever. The whole feature recorded rows no character would ever reach.
        ///   </para>
        ///   <para>
        ///     Caught by a t.Note in the live self-test reporting "a third party can reach it through the
        ///     rumour road=False", which was written as an observation and turned out to be the finding.
        ///   </para>
        ///   <para>
        ///     25 puts it just below a town's real hardship (30) and far below a war declared (75), which is
        ///     where an invented local blight belongs: it reaches the district and the realm, and not a
        ///     stranger across the map.
        ///   </para>
        /// </summary>
        public const int Magnitude = 25;

        /// <summary>
        ///   A thing wondered aloud rather than stated flatly. Far enough below <see cref="Magnitude" /> to be
        ///   a real difference on this scale - at 15 it needs proximity OR the realm to be heard at all, where
        ///   a stated claim needs only one of them.
        /// </summary>
        public const int HedgedMagnitude = 15;

        /// <summary>The subjects the engine does not model, and therefore the only ones a claim may be about.</summary>
        public static IReadOnlyList<ClaimSubject> PromotableSubjects { get; } = new List<ClaimSubject> {
            ClaimSubject.Blight,
            ClaimSubject.Banditry,
            ClaimSubject.PassingOrBirth,
            ClaimSubject.Omen,
            ClaimSubject.LocalQuarrel
        };

        /// <summary>
        ///   Whether this claim may enter the world, and why not when it may not.
        /// </summary>
        /// <param name="claim">What was said.</param>
        /// <param name="speakerIsThePlayer">
        ///   Whether the speaker is the player. Passed in rather than inferred from the id, because the one
        ///   rule that must never be got wrong by a string comparison is the one that decides who writes the
        ///   world.
        /// </param>
        /// <param name="alreadyKnown">Whether the world already carries this claim.</param>
        public static ClaimVerdict Judge(WorldClaim claim, bool speakerIsThePlayer, bool alreadyKnown = false)
        {
            if (claim == null) return ClaimVerdict.NoSpeaker;
            if (speakerIsThePlayer) return ClaimVerdict.FromThePlayer;
            if (string.IsNullOrWhiteSpace(claim.SpeakerId)) return ClaimVerdict.NoSpeaker;
            if (claim.AboutThePlayer) return ClaimVerdict.AboutThePlayer;
            if (claim.Subject == ClaimSubject.EngineOwned) return ClaimVerdict.EngineOwnsIt;
            if (claim.Subject == ClaimSubject.Unknown) return ClaimVerdict.Unclassified;
            if (!PromotableSubjects.Contains(claim.Subject)) return ClaimVerdict.EngineOwnsIt;

            string text = (claim.Text ?? "").Trim();
            if (text.Length < MinTextChars || text.Length > MaxTextChars) return ClaimVerdict.NotSayable;

            if (alreadyKnown) return ClaimVerdict.AlreadyKnown;

            return ClaimVerdict.Accepted;
        }

        /// <summary>
        ///   A stable key for "the world already carries this". Subject plus place plus the opening words,
        ///   lowercased: two characters describing the same blight in the same village will phrase it
        ///   differently, and a world that records one copy per retelling drowns its own real events within
        ///   an hour of play. Deliberately coarse - a false MATCH loses a duplicate, a false MISS accumulates
        ///   for ever, and only one of those is recoverable.
        /// </summary>
        public static string DedupKey(WorldClaim claim)
        {
            if (claim == null) return "";

            string place = (claim.PlaceName ?? "-").Trim().ToLowerInvariant();
            string opening = (claim.Text ?? "").Trim().ToLowerInvariant();

            if (opening.Length > DedupPrefixChars) opening = opening.Substring(0, DedupPrefixChars);

            return $"{claim.Subject}|{place}|{opening}";
        }

        /// <summary>How much of the sentence the duplicate check looks at. Enough to identify, short enough to
        /// survive rephrasing.</summary>
        public const int DedupPrefixChars = 40;

        /// <summary>Convenience for the caller that only wants the answer.</summary>
        public static bool MayEnterTheWorld(WorldClaim claim, bool speakerIsThePlayer, bool alreadyKnown = false)
            => Judge(claim, speakerIsThePlayer, alreadyKnown) == ClaimVerdict.Accepted;

        /// <summary>
        ///   How far it travels. A thing said flatly by someone who would know spreads further than a thing
        ///   wondered aloud, and a claim with a place spreads around that place rather than everywhere.
        ///   Expressed as the magnitude the rumour pipeline already understands, capped by
        ///   <see cref="Magnitude" /> so nothing invented ever outweighs something that happened.
        /// </summary>
        public static int ReachOf(WorldClaim claim)
        {
            if (claim == null) return 0;

            return claim.StatedAsFact ? Magnitude : HedgedMagnitude;
        }
    }
}

// Code written by Gabriel Mailhot, 11/09/2026.
// Knowledge composition, increment 2. Gabriel, on adding hearts and grudges to what a character knows: "la
// mémoire d'un NPC aujourd'hui est relative au joueur car elle s'inscrit pendant un chat. Mais le NPC devrait
// aussi avoir une mémoire envers d'autres afin qu'il puisse en parler."
//
// He is right, and the check bore it out precisely: GrudgeNote is documented as the grievances an NPC nurses
// AGAINST THE PLAYER, AppreciationNote is its mirror, and every romance field is about the player or the
// player's partner. Meanwhile the simulation quietly runs NPC-to-NPC lives - in one cr.testall pass, Jinda took
// Tulag as a secret lover, Decantia bore Lucon's child, and Lucon nursed a grudge against his captor rather
// than against the player. All of it happens, all of it persists, and none of those people can mention it.
//
// But the question forced an axis the design did not have. A grudge is something a man says; a love, especially
// a secret one, is something he keeps. Until now a pack answered one question - does he KNOW it. Hearts need a
// second - would he SAY it, to YOU, here, in front of these people.
//
// That axis pays for itself immediately elsewhere: without it, a bonds pack would gut the mystery-lover pillar
// by having everyone blurt everyone's secrets. With it, discretion becomes structural rather than a line of
// prose asking the model to be tactful.

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>How freely a character would speak of something they know.</summary>
    public enum Discretion
    {
        /// <summary>Common knowledge to them. They would say it to anyone who asked, and often unprompted.</summary>
        Open,

        /// <summary>Their own business. Said to someone they have reason to trust, and not in front of a room.</summary>
        Guarded,

        /// <summary>
        ///   Kept. NOT rendered by a knowledge pack at all, ever - not even framed as a thing being withheld,
        ///   because naming a secret in the prompt is how a secret gets told.
        ///   <para>
        ///     Deliberately NOT this pillar's business to draw out. CR already owns that: the mystery-lover
        ///     pillar decides when a held secret may surface and what it costs. A pack that also tried would be
        ///     a second authority on the same fact, and this project has paid for parallel authorities before.
        ///   </para>
        /// </summary>
        Secret
    }

    /// <summary>
    ///   Who is listening, and under what conditions. What a character will say is not a property of the fact
    ///   alone: the same man tells you in private what he would never say with his captain in the room.
    /// </summary>
    public sealed class ListeningAudience
    {
        /// <summary>The mod's own personal regard, never the vanilla clan relation.</summary>
        public int Regard { get; init; }

        /// <summary>Nobody else is present. A confidence is not given to a room.</summary>
        public bool InPrivate { get; init; }

        /// <summary>
        ///   The listener already knows this particular thing. A fact the player has already learned is no
        ///   longer being disclosed by mentioning it, which is what stops a character acting cagey about
        ///   something you were both there for.
        /// </summary>
        public bool AlreadyKnownToListener { get; init; }
    }

    /// <summary>Whether a character would speak of something, given who is listening.</summary>
    public static class DiscretionPolicy
    {
        /// <summary>
        ///   Regard at or above which a guarded matter may be spoken of. Set at the same bar the mod already
        ///   uses for a confidence rather than invented: below this you are an acquaintance, not a confidant.
        /// </summary>
        public const int ConfidingRegardFloor = 30;

        /// <summary>
        ///   True when the character would say it. Unknown audience is treated as a stranger in a crowded room,
        ///   which refuses everything but the open, because guessing wrong here costs a secret.
        /// </summary>
        public static bool WouldSay(Discretion discretion, ListeningAudience audience)
        {
            if (discretion == Discretion.Open) return true;

            // Something the listener already knows cannot be disclosed to them again, whatever its discretion,
            // and pretending otherwise makes a character coy about a thing you watched happen.
            if (audience != null && audience.AlreadyKnownToListener) return discretion != Discretion.Secret;

            if (discretion == Discretion.Secret) return false;

            return audience != null && audience.InPrivate && audience.Regard >= ConfidingRegardFloor;
        }
    }
}

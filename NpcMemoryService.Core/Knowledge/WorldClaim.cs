// Code written by Gabriel Mailhot, 12/09/2026.
// Invented facts, increment 1: the shape of a claim, and what kind of thing it is about.
//
// Gabriel, 2026-09-12, watching a governor invent a blight in his own fields: "si un NPC invente un FAIT, comme
// le mildiou, et que le joueur en parle à un autre NPC, ce dernier ne connaîtra pas ce fait. Ça peut créer de
// l'incohérence. Est-ce qu'il est possible de capter des FAITS INVENTÉS et de les rendre réels dans le Monde?"
//
// HIS RULING, and it is the load-bearing one: "Les nouveaux faits proviennent QUE des NPC, pas du joueur. Le
// joueur subit le Monde alors que les NPC le définissent. C'est la nature du jeu vidéo." So a claim always has
// a speaker who is not the player, and a player's own invention is never promoted - it is a thing he said, and
// the world owes it nothing.
//
// THE OTHER RULE, which is what keeps an LLM writing to shared state from being reckless: a claim may only
// enter the world where the ENGINE HAS NOTHING TO SAY. A blight, banditry on a road, a death nobody modelled,
// an omen - Bannerlord has no field for any of them, so an invention cannot contradict a screen the player can
// open. Troop counts, coin, who holds which town, who is at war: the engine owns those, and promoting an
// invention there would manufacture exactly the checkable lie that seat_standing was built to end. It is the
// "bands, not numbers" boundary drawn one level up.

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What a claim is ABOUT, which is the whole of whether it may become real. The allowed kinds share one
    ///   property and only one: Bannerlord models none of them, so nothing the player can open contradicts
    ///   them.
    /// </summary>
    public enum ClaimSubject
    {
        /// <summary>Not classified. Refused, because a claim nobody could name is a claim nobody can judge.</summary>
        Unknown,

        /// <summary>Blight, murrain, a failed harvest, a sickness of the land. The mildew that started this.</summary>
        Blight,

        /// <summary>Brigands on a road, a caravan taken, travellers waylaid. Local trouble with no engine record.</summary>
        Banditry,

        /// <summary>A death, a birth, a marriage among people the game does not simulate.</summary>
        PassingOrBirth,

        /// <summary>A portent, an omen, a superstition. True or not, people believing it is the fact.</summary>
        Omen,

        /// <summary>A quarrel, a scandal or a falling-out at a hall, with no engine consequence.</summary>
        LocalQuarrel,

        /// <summary>
        ///   Anything the ENGINE owns: troop numbers, coin, who holds a settlement, war and peace, prosperity,
        ///   garrisons, who governs. Never promoted. Named as a value rather than left as "not in the list", so
        ///   a refusal is a decision the extractor records and the sweep can count.
        /// </summary>
        EngineOwned
    }

    /// <summary>
    ///   One factual assertion an NPC made about the world that was not in their prompt. A candidate for the
    ///   rumour pipeline, not yet a fact: <see cref="WorldClaimPolicy" /> decides.
    /// </summary>
    public sealed class WorldClaim
    {
        /// <summary>The hero who said it. Never the player - see the header ruling.</summary>
        public string? SpeakerId { get; init; }

        /// <summary>Their name, for the line a third party would later hear.</summary>
        public string? SpeakerName { get; init; }

        /// <summary>What it is about, which decides whether it may enter the world at all.</summary>
        public ClaimSubject Subject { get; init; } = ClaimSubject.Unknown;

        /// <summary>
        ///   The place it concerns, as the speaker named it. Null when the claim is about nowhere in
        ///   particular, which is allowed: not every piece of news has an address.
        /// </summary>
        public string? PlaceName { get; init; }

        /// <summary>
        ///   The claim itself, in one plain third-person sentence, as a chronicler would set it down. This is
        ///   what a later listener hears, so it must read as news and not as a transcript line.
        /// </summary>
        public string? Text { get; init; }

        /// <summary>
        ///   True when the speaker stated it as fact rather than wondering aloud. Both may travel, but a thing
        ///   said flatly by a man who would know is worth more than a tavern guess, and the difference decides
        ///   how far it goes.
        /// </summary>
        public bool StatedAsFact { get; init; }

        /// <summary>
        ///   True when the claim is about the PLAYER rather than the world. Refused: the player's deeds already
        ///   have their own road into the world through the deed and chronicle systems, and letting a
        ///   conversation mint them would put a second author on one record.
        /// </summary>
        public bool AboutThePlayer { get; init; }
    }
}

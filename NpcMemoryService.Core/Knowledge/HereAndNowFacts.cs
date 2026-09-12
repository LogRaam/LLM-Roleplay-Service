// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 3. Gabriel, when asked what other categories there might be: "la saison
// actuelle, sommes-nous le jour ou la nuit, ce sont des détails d'environnement qui comptent."
//
// The audit's first pass found he was right and that the gap was total: grepping EncounterContext returned ZERO
// for season, time of day, night, weather and terrain. A character had no idea whether it was winter or whether
// it was the middle of the night, and would cheerfully wish you a fine morning in a blizzard at midnight.
//
// The sharper half of that finding, and the reason this pillar exists at all: CouncilPromptInput ALREADY has a
// Season field and the council prompt says it. So the mod had already decided a season is worth telling a
// character - once, for one surface, and never generalised. That is exactly the "built as we go, scattered"
// this whole design was asked for, caught by the first question put to it rather than by a player report.
//
// Everything here is one engine read. Nothing is inferred, nothing is stored.

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>Calradia's four seasons, as the engine's own calendar names them.</summary>
    public enum Season
    {
        Unknown,
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>
    ///   What the sky is doing. Mirrors the engine's own weather events rather than inventing a scale, so a
    ///   future engine value has an obvious home instead of being flattened into "bad weather".
    /// </summary>
    public enum Weather
    {
        Unknown,
        Clear,
        LightRain,
        HeavyRain,
        Snow,
        Blizzard,
        Storm
    }

    /// <summary>
    ///   Where in the year and the day this conversation is happening, and what the weather is doing to the
    ///   people having it. The cheapest knowledge there is, carried by everyone, and absent until now.
    ///   <para>
    ///     Deliberately NOT here: whether either of them is mounted. That is a mission-level fact with no
    ///     reliable read on the campaign map, and guessing it would be worse than omitting it. Named so its
    ///     absence reads as a decision rather than an oversight.
    ///   </para>
    ///   <para>
    ///     Also not here: whether they are indoors. <c>SceneType</c> already reaches the prompt and says so, and
    ///     a second source for one fact is how two sources come to disagree.
    ///   </para>
    /// </summary>
    public sealed class HereAndNowFacts
    {
        /// <summary>The season, from the engine's calendar.</summary>
        public Season Season { get; init; } = Season.Unknown;

        /// <summary>The hour, 0-23, from the engine's clock.</summary>
        public int HourOfDay { get; init; }

        /// <summary>The engine's own night reading, rather than a threshold guessed from the hour.</summary>
        public bool IsNight { get; init; }

        /// <summary>What the sky is doing where they are standing.</summary>
        public Weather Weather { get; init; } = Weather.Unknown;

        /// <summary>
        ///   The settlement they are in, or the nearest one when they are out in the country.
        ///   <para>
        ///     Gabriel, 2026-09-12: "Il faudrait que le NPC ait conscience de où il se trouve sur la carte
        ///     Monde." He is right and the gap is precise: <c>CurrentLocationNote</c> and <c>GeographyNote</c>
        ///     both ground the settlement a character STANDS IN, and both are documented as null when the
        ///     conversation is not in one. So the moment the party is on the open map - which is most of the
        ///     game - nobody has any idea where they are.
        ///   </para>
        /// </summary>
        public string? NearestPlace { get; init; }

        /// <summary>
        ///   True when they are INSIDE that place rather than out in the country near it. When true this pack
        ///   says nothing about where they are: <c>CurrentLocationNote</c> already tells them, and two sources
        ///   for one fact is how two sources come to disagree. The pack fills the gap and stays quiet elsewhere.
        /// </summary>
        public bool IsInsideThatPlace { get; init; }

        /// <summary>
        ///   How that place lies from here, in the words the mod already uses for a journey ("a short ride to
        ///   the north-east"), built by <c>GeographyDescriber</c>. Empty when they are inside it.
        /// </summary>
        public string? BearingToPlace { get; init; }

        /// <summary>Whose lands these are, when the country belongs to somebody. Null in the true wilds.</summary>
        public string? RealmName { get; init; }
    }
}

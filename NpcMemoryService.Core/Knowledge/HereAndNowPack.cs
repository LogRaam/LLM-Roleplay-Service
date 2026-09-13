// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 3: the hour and the weather, which everybody knows and nobody was told.

#region

using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   Where in the year and the day this conversation stands. The cheapest pack there is, and the only one
    ///   carried by literally everyone: a beggar knows it is snowing as surely as a king does.
    /// </summary>
    public sealed class HereAndNowPack : KnowledgePack
    {
        public override string Name => "here_and_now";

        /// <summary>
        ///   The hour, the sky and the place all change under the character, so this must sit below the
        ///   encounter marker or it takes the whole prefix with it every time the clock moves. Measured at
        ///   10,018 characters of a 37,267-character prompt on 2026-09-12.
        /// </summary>
        public override PackVolatility Volatility => PackVolatility.PerEncounter;

        public override string Covers =>
            "The season, the hour, whether it is night, and what the weather is doing. Everyone alive knows all "
            + "of it without being told, which is exactly why nobody noticed the characters had never been told.";

        /// <summary>Atmosphere, and cheap enough that it usually fits anyway.</summary>
        public override int DropPriority => 55;

        /// <summary>Everyone. Standing outdoors in the rain is not a privilege of rank.</summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
            => bearer == null ? KnowledgeDepth.None : KnowledgeDepth.Ordinary;

        /// <summary>
        ///   The host can always answer this: there is always a season and always an hour. So unlike the bonds
        ///   pack, an empty one here IS a fault, and the live sweep should say so.
        /// </summary>
        public override bool IsSupplied(EncounterContext context)
            => context?.HereAndNow != null && context.HereAndNow.Season != Season.Unknown;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            HereAndNowFacts? now = context?.HereAndNow;
            if (now == null || now.Season == Season.Unknown) return;

            sb.AppendLine($"It is {Moment(now)}{WeatherClause(now.Weather)}.");

            string where = lean ? CompactWhereClause(now) : WhereClause(now);
            if (where.Length > 0) sb.AppendLine(where);

            // Lean stops at the fact. A model that has been told the hour rarely needs telling not to contradict
            // it; the instruction below is worth its characters in a Full prompt and not in a Compact one, which
            // was cut from 22,500 to 15,150 chars on 2026-09-11 and is not being spent back a line at a time.
            if (lean) return;

            sb.AppendLine("Let that colour the scene where it naturally would, and never contradict it: do not "
                          + "greet a fine morning at midnight, nor speak of the heat in a blizzard. Do not open "
                          + "with the weather either. It is the room you are both standing in, not your subject.");
        }

        #region private

        /// <summary>"a winter night", "a summer afternoon" - the way a person places themselves, not a clock reading.</summary>
        private static string Moment(HereAndNowFacts now)
        {
            string season = now.Season.ToString().ToLowerInvariant();

            return $"a {season} {PartOfDay(now)}";
        }

        private static string PartOfDay(HereAndNowFacts now)
        {
            if (now.IsNight) return "night";

            if (now.HourOfDay < 6) return "night";
            if (now.HourOfDay < 11) return "morning";
            if (now.HourOfDay < 14) return "midday";
            if (now.HourOfDay < 18) return "afternoon";
            if (now.HourOfDay < 21) return "evening";

            return "night";
        }

        /// <summary>
        ///   Where they are, but ONLY out in the country. Inside a settlement, CurrentLocationNote has said it
        ///   since long before this pillar existed, and repeating it would be a second source for one fact.
        ///   This exists because both of the older fields go null the moment the party is on the open map,
        ///   which is most of the game (Gabriel, 2026-09-12).
        /// </summary>
        private static string WhereClause(HereAndNowFacts now)
        {
            if (now.IsInsideThatPlace) return "";

            string place = (now.NearestPlace ?? "").Trim();
            string realm = (now.RealmName ?? "").Trim();

            if (place.Length == 0)
                return realm.Length == 0
                    ? ""
                    : $"You are out in open country, in the lands of {realm}.";

            string bearing = (now.BearingToPlace ?? "").Trim();
            string from = bearing.Length == 0 ? $"near {place}" : $"{bearing} of {place}";
            string whose = realm.Length == 0 ? "" : $", in the lands of {realm}";

            return $"You are out in open country, {from}{whose}.";
        }

        /// <summary>
        ///   The same fact in a quarter of the words: "Near Ocs Hall, in Vlandia." A Compact prompt needs to
        ///   know where the character is standing, not to be told it in a full sentence.
        /// </summary>
        private static string CompactWhereClause(HereAndNowFacts now)
        {
            if (now.IsInsideThatPlace) return "";

            string place = (now.NearestPlace ?? "").Trim();
            string realm = (now.RealmName ?? "").Trim();

            if (place.Length == 0) return realm.Length == 0 ? "" : $"In {realm}.";

            return realm.Length == 0 ? $"Near {place}." : $"Near {place}, in {realm}.";
        }

        private static string WeatherClause(Weather weather)
        {
            switch (weather)
            {
                case Weather.LightRain: return ", and it is raining lightly";
                case Weather.HeavyRain: return ", and the rain is heavy";
                case Weather.Snow: return ", and snow is falling";
                case Weather.Blizzard: return ", and a blizzard is blowing";
                case Weather.Storm: return ", and a storm is over you";

                // Clear weather earns no clause: a sentence about the absence of rain is a sentence wasted, and
                // this pack is read in every single conversation.
                default: return "";
            }
        }

        #endregion
    }
}

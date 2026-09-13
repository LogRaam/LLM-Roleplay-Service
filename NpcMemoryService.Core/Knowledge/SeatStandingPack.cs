// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 4. The pack the live bench asked for: see SeatStandingFacts for the two
// transcripts that specified it.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   What a character knows about the place they answer for: how it fares, what is in the granary, whether
    ///   the garrison could hold it, and whether the people are with them. In bands, never in numbers.
    /// </summary>
    public sealed class SeatStandingPack : KnowledgePack
    {
        public override string Name => "seat_standing";

        public override string Covers =>
            "How the place this character answers for is actually doing: its prosperity against other places of "
            + "its kind, its granary, its garrison, the temper of its people, whether the roads around it are "
            + "safe, and whether it is under siege, raided, starving or in revolt. In bands rather than figures, "
            + "because a lord knows how his seat fares and has never counted its stores.";

        /// <summary>His own seat, and a siege at his walls outranks nearly anything else he might mention.</summary>
        public override int DropPriority => 30;

        /// <summary>
        ///   Anyone who answers for a place: the lord who holds it, the companion who governs it, the headman
        ///   who lives in it. Note it is not about rank - a village headman knows his own granary far better
        ///   than the count who owns him does.
        ///   <para>
        ///     This asks ONE thing, and an earlier draft asked two ("holds a seat OR is a notable"). That draft
        ///     would have reported a notable with no home settlement as a missing fact, when the truth is he
        ///     has no place to answer for - the personal_bonds lesson from the sweep's first live run, where my
        ///     rule and not the host was what was wrong. Whether a notable's home counts belongs in the one
        ///     place that can see the home.
        ///   </para>
        /// </summary>
        public override KnowledgeDepth DepthFor(KnowledgeBearer bearer)
        {
            if (bearer == null || bearer.IsCutOff || !bearer.HoldsASeat) return KnowledgeDepth.None;

            // The first real use of the depth axis, and it is Gabriel's own example the other way round: a
            // person who LIVES in a place knows it better than the person who owns it from three towns away.
            // A headman can tell you what is in the granary this week; his count knows how the place fares and
            // would have to send for the tally.
            return bearer.IsNotable ? KnowledgeDepth.Deep : KnowledgeDepth.Knowing;
        }

        /// <summary>
        ///   A place always has a name and always has a state, so an empty one means nobody read the engine.
        ///   That is the fault fkasad reported in its purest form, and this is the sweep that would catch it.
        /// </summary>
        public override bool IsSupplied(EncounterContext context)
            => Described(context).Count > 0;

        public override void Render(StringBuilder sb, EncounterContext context, bool lean)
        {
            List<SeatFacts> seats = Described(context);
            if (seats.Count == 0) return;

            // Lean says only the most closely held place, and only what would change what the character says.
            foreach (SeatFacts seat in lean ? seats.Take(1) : seats)
                sb.AppendLine(Describe(seat, lean, LivesThere(seat)));

            if (lean) return;

            // The line the whole increment exists for. A prohibition alone tends to be obeyed by silence, which
            // would be its own regression, so it also hands over the in-character way OUT: the same move that
            // made the purse probe read so well ("You keep your own purse, not I") rather than as a refusal.
            sb.AppendLine("You know how your own holdings FARE, in exactly those terms and no finer. Never give a "
                          + "figure for stores, garrison, coin, population or harvest, and never invent a detail "
                          + "of how the place is run that is not written above: you have not counted any of it, "
                          + "and a number you name is one the player can check and find false. If you are pressed "
                          + "for a tally, say plainly that you would have to send for your steward's books.");
        }

        #region private

        /// <summary>
        ///   Whether they are ON the spot, which is what decides how finely they know it. Read from the SEAT
        ///   rather than from the bearer, because the bearer cannot tell the two apart: a governor and an
        ///   absentee owner both merely "hold a seat", and the governor administers the granary daily while
        ///   the owner is three towns away.
        ///   <para>
        ///     This is the per-subject half of Gabriel's depth axis. The bearer-level half - is this a person
        ///     who lives in a place at all - answers the composition question in <see cref="DepthFor" />; how
        ///     finely they know THIS place is a question only the place can answer.
        ///   </para>
        /// </summary>
        private static bool LivesThere(SeatFacts seat)
            => seat.Role == SeatRole.Governor || seat.Role == SeatRole.Notable;

        /// <summary>The places worth describing: named, and with something actually read about them.</summary>
        private static List<SeatFacts> Described(EncounterContext? context)
            => (context?.SeatStanding?.Seats ?? new List<SeatFacts>())
               .Where(s => s != null && !string.IsNullOrWhiteSpace(s.PlaceName) && s.HasAnyReading)
               .Take(SeatStandingFacts.MaxSeatsDescribed)
               .ToList();

        private static string Describe(SeatFacts seat, bool lean, bool deep)
        {
            string place = seat.PlaceName!.Trim();
            var sentence = new StringBuilder($"{Holding(seat, place)}");

            List<string> clauses = Clauses(seat, lean, deep);
            if (clauses.Count > 0) sentence.Append($" {Sentence(clauses)}");

            return sentence.ToString();
        }

        /// <summary>"You govern Pravend, a town of the player's own house."</summary>
        private static string Holding(SeatFacts seat, string place)
        {
            string kind = seat.Kind == SettlementKind.Unknown ? "holding" : seat.Kind.ToString().ToLowerInvariant();
            string whose = seat.IsPlayerHolding ? " of the player's own house" : "";

            switch (seat.Role)
            {
                case SeatRole.Governor: return $"You govern {place}, a {kind}{whose}, day to day.";
                case SeatRole.Owner: return $"You hold {place}, a {kind}{whose}.";
                case SeatRole.Notable: return $"{place} is your own {kind}{whose}, and you have lived there all your life.";
                default: return $"You answer for {place}, a {kind}{whose}.";
            }
        }

        /// <summary>
        ///   What is true of the place, urgent first. An army at the walls outranks the state of the granary,
        ///   and a character who leads with the harvest while his town is besieged has not understood the room.
        /// </summary>
        private static List<string> Clauses(SeatFacts seat, bool lean, bool deep)
        {
            var clauses = new List<string>();

            if (seat.IsUnderSiege) clauses.Add("it is under siege as you speak");
            if (seat.IsInRebellion) clauses.Add("its people have risen against you");
            if (seat.IsBeingRaided) clauses.Add("raiders are burning its lands right now");
            if (seat.IsStarving) clauses.Add("its people are going hungry");
            if (seat.IsLooted) clauses.Add("it was sacked and has not recovered");

            // Lean stops at what is on fire. The bands are the difference between colour and crisis, and the
            // Compact prompt was cut from 22,500 to 15,150 chars on 2026-09-11 for reasons that still hold.
            if (lean) return clauses;

            AddIf(clauses, Prosperity(seat));

            // The granary is the steward's book, and only somebody who lives there reads it weekly. An
            // absentee holder knows how the place FARES; he does not know what came in on Tuesday.
            if (deep) AddIf(clauses, Food(seat));
            AddIf(clauses, Garrison(seat));
            AddIf(clauses, Loyalty(seat));
            AddIf(clauses, Security(seat));

            return clauses;
        }

        private static void AddIf(List<string> clauses, string? clause)
        {
            if (!string.IsNullOrEmpty(clause)) clauses.Add(clause!);
        }

        private static string? Prosperity(SeatFacts seat)
        {
            switch (seat.Prosperity)
            {
                case ProsperityBand.Poor: return "it is among the poorer places of its kind in Calradia";
                case ProsperityBand.Middling: return "it is a middling place, neither rich nor wanting";
                case ProsperityBand.Comfortable: return "it does comfortably well";
                case ProsperityBand.Rich: return "it is among the wealthiest of its kind in Calradia";
                default: return null;
            }
        }

        /// <summary>
        ///   Whole phrases rather than a stem with a suffix bolted on. A real prompt dump on 2026-09-12 showed
        ///   the composed form producing "its granaries will see it through and filling", which is not English
        ///   - and which no test caught, because every test asserted a substring rather than reading the
        ///   sentence.
        /// </summary>
        private static string? Food(SeatFacts seat)
        {
            bool falling = seat.FoodDirection == FoodTrend.Falling;
            bool rising = seat.FoodDirection == FoodTrend.Rising;

            switch (seat.FoodStores)
            {
                case FoodBand.Empty:
                    return falling ? "its granaries are all but empty and still emptying"
                        : rising ? "its granaries are all but empty, though filling again"
                        : "its granaries are all but empty";
                case FoodBand.Thin:
                    return falling ? "its granaries are thin and emptying"
                        : rising ? "its granaries are thin, though filling again"
                        : "its granaries are thin";
                case FoodBand.Adequate:
                    return falling ? "its granaries will see it through, though they are dropping"
                        : "its granaries will see it through";
                case FoodBand.Full:
                    return falling ? "its granaries are full, though drawing down"
                        : "its granaries are full";
                default: return null;
            }
        }

        private static string? Garrison(SeatFacts seat)
        {
            // A village keeps no garrison. Saying it has a skeleton one would be a fact the player can disprove
            // by looking, which is the exact failure this whole pack exists to close.
            if (seat.Kind == SettlementKind.Village) return null;

            switch (seat.Garrison)
            {
                case GarrisonBand.Skeleton: return "barely a garrison holds it";
                case GarrisonBand.UnderStrength: return "its garrison is under strength";
                case GarrisonBand.Adequate: return "its garrison is enough to hold it";
                case GarrisonBand.Strong: return "its garrison is strong";
                default: return null;
            }
        }

        private static string? Loyalty(SeatFacts seat)
        {
            switch (seat.Loyalty)
            {
                case LoyaltyBand.Rebellious: return "its people are near revolt";
                case LoyaltyBand.Restless: return "its people are restless";
                case LoyaltyBand.Content: return "its people are content enough";
                case LoyaltyBand.Devoted: return "its people are devoted to you";
                default: return null;
            }
        }

        private static string? Security(SeatFacts seat)
        {
            switch (seat.Security)
            {
                case SecurityBand.Lawless: return "the country around it is lawless";
                case SecurityBand.Uneasy: return "the roads around it are no longer certain";
                case SecurityBand.Settled: return "the roads around it are safe";
                default: return null;
            }
        }

        /// <summary>"a, b and c." - the clauses as one sentence rather than a bulleted inventory.</summary>
        private static string Sentence(List<string> clauses)
        {
            string joined = clauses.Count == 1
                ? clauses[0]
                : string.Join(", ", clauses.Take(clauses.Count - 1)) + " and " + clauses[clauses.Count - 1];

            return char.ToUpperInvariant(joined[0]) + joined.Substring(1) + ".";
        }

        #endregion
    }
}

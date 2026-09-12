// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 10: what word has brought this character about the wider world.
//
// THE AUDIT FINDING THAT SHAPED IT, and it is the reason this is not a new feature. Realm news already
// reached the prompt, by two routes: RealmNewsLine (the one great happening, "the talk of all the realm") and
// WorldRumorsBlock (a pre-formatted "what you've heard" list, drawn from WorldEventStore with an awareness
// filter and a confidence rating). Both are good and both are already tuned.
//
// What was MISSING is exactly what Gabriel's depth axis names: the host asked
// GetKnownEventsRated(hero, today, 6), then capped the result at FOUR LINES FOR EVERYBODY. The filter is
// geographic and temporal - their faction, their region, recency - and never once asks WHO the character is.
// So a village headman and a marshal of the realm hear precisely the same amount, from precisely as far away.
//
// That is wrong in the way a player would feel rather than see: a lord keeps couriers, a court and men whose
// business is knowing things; a villager has the market. Same world, different reach.
//
// So this pack does not add news. It takes ownership of a decision that was living as a magic number in the
// host, and makes it a rule about people that can be read, argued with and tested.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Knowledge
{
    /// <summary>
    ///   How well word travelled. Mirrors the rating the world-event store already produces, so nothing here
    ///   reinvents a scale the mod has been using since v1.2.
    /// </summary>
    public enum TidingConfidence
    {
        /// <summary>They have it from someone who was there, or were there themselves.</summary>
        Firsthand,

        /// <summary>Heard secondhand. Held loosely.</summary>
        Secondhand,

        /// <summary>A distant, unverified rumour. Repeated with real doubt, details possibly wrong.</summary>
        Distant
    }

    /// <summary>One thing word has brought.</summary>
    public sealed class Tiding
    {
        /// <summary>What happened, in one plain line.</summary>
        public string? Text { get; init; }

        /// <summary>How well it travelled, which decides how firmly they may state it.</summary>
        public TidingConfidence Confidence { get; init; } = TidingConfidence.Firsthand;
    }

    /// <summary>
    ///   The news that has reached this character. Supplied RAW and uncapped: how much of it they actually
    ///   carry is a rule about people, and belongs to the pack rather than to the host.
    /// </summary>
    public sealed class RealmNewsFacts
    {
        /// <summary>
        ///   The single greatest realm-level happening - a war declared, a peace struck - phrased as the talk
        ///   of the whole realm. Everyone hears this one, whoever they are: it is the news that reaches a
        ///   village square as surely as a council chamber.
        /// </summary>
        public string? Headline { get; init; }

        /// <summary>
        ///   Everything else word has brought, most worth telling first. The host does not cut this list; the
        ///   pack does, by how far this character's word reaches.
        /// </summary>
        public IReadOnlyList<Tiding> Tidings { get; init; } = new List<Tiding>();

        /// <summary>True when anything at all reached them.</summary>
        public bool HasAnyReading => !string.IsNullOrWhiteSpace(Headline) || Tidings.Count > 0;
    }
}

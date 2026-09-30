namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   The small spoken shifts to the player's standing the LLM signals in one exchange (a
    ///   <c>[STANCE]</c> block): how this conversation nudged the NPC's trust, respect, and fear.
    ///   The consumer caps these hard and rate-limits them — words only colour a standing, deeds
    ///   move it. Affection is NOT here; it goes through the gated change_relation action.
    /// </summary>
    public sealed class StanceShiftData
    {
        /// <summary>Spoken nudge to how far the NPC trusts the player's word (signed; capped by the consumer).</summary>
        public int Trust { get; init; }

        /// <summary>Spoken nudge to the NPC's respect for the player (signed; capped by the consumer).</summary>
        public int Respect { get; init; }

        /// <summary>Spoken nudge to the NPC's fear of the player (signed; capped by the consumer).</summary>
        public int Fear { get; init; }

        /// <summary>
        ///   How the exchange left the character's mood of the day: +1 lifted, -1 lowered, 0 neither (a "mood:" line in
        ///   the [STANCE] block, since 30/09/2026). The consumer moves the mood one band per turn at most.
        /// </summary>
        public int Mood { get; init; }
    }
}

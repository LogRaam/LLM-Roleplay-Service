namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// An NPC's identity and accumulated memory of the player.
    /// Persisted per save game in Phase 4 via INpcMemoryStore.
    /// </summary>
    public sealed class NpcProfile
    {
        public required string Id         { get; init; }
        public required string Name       { get; init; }
        public required string Faction    { get; init; }
        public required string Clan       { get; init; }

        /// <summary>Free-text personality description injected into the system prompt.</summary>
        public string? Personality        { get; init; }

        /// <summary>
        /// Running digest of past interactions, updated after each conversation.
        /// Null means the NPC has never met the player.
        /// </summary>
        public string? MemoryDigest       { get; set; }

        /// <summary>Player reputation score from this NPC's perspective. Negative = hostile.</summary>
        public int ReputationWithPlayer   { get; set; }
    }
}

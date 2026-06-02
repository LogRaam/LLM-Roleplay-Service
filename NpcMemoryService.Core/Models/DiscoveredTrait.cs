// Code written by Gabriel Mailhot, 01/06/2026. Sprint 10.5 — Discovery system.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   A personal preference or romantic trait that the player has discovered
    ///   about an NPC through conversation. Persisted with the NPC profile so
    ///   the player's knowledge accumulates across sessions.
    ///   Each entry reflects a natural moment where the NPC chose to reveal
    ///   or hint at something about themselves. Never fabricated by the mutator —
    ///   always LLM-emitted through genuine roleplay.
    /// </summary>
    public sealed class DiscoveredTrait
    {
        /// <summary>
        ///   Machine-readable slug identifying the trait category.
        ///   Examples: <c>orientation</c>, <c>archetype</c>,
        ///   <c>preference_dominant</c>, <c>kink_bondage_receiving</c>.
        ///   Used as the deduplication key — a given key appears at most once
        ///   per profile.
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        ///   Human-readable description written from the player's perspective,
        ///   as the player would perceive or remember it.
        ///   Example: "She seems drawn to men." or "She tends to lead."
        ///   Displayed verbatim in the encyclopedia discovery section.
        /// </summary>
        public required string Description { get; init; }

        /// <summary>The game day on which this trait was first discovered.</summary>
        public int GameDay { get; init; }
    }
}

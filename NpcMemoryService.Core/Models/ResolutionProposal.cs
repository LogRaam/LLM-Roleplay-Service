// Code written by Gabriel Mailhot, 21/07/2026.
// The council's mechanical afterlife: a [RESOLUTION] block is the positive channel that replaces the old pure
// "nothing may be sealed at this table" prohibition (see PromptBuilder's council section). Nothing executes
// when this is parsed: the consumer records it as provisional and only re-validates/executes it when the
// council is LIFTED (the window closes). Mirrors GameAction's shape (a free Type string the consumer alone
// interprets) so the parser stays single and the vocabulary of "kinds" lives in the consumer's own catalogue.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   A resolution the table took, as parsed from a <c>[RESOLUTION]</c> block. The consumer resolves
    ///   <see cref="Actor" /> to a seated member (never guessing when it cannot), then either records it
    ///   (provisional, reversible) or, for <see cref="Type" /> <c>"withdraw"</c>, reverses an already-recorded
    ///   one. <see cref="TargetSettlement" /> is the one piece of catalogue-specific data this first slice
    ///   needs (grounding a <c>"quest"</c> kind the same way an ordinary <c>[QUEST]</c> block would); a future
    ///   kind that needs more data extends this type rather than inventing a second parsed shape.
    /// </summary>
    public sealed class ResolutionProposal
    {
        /// <summary>The kind of resolution (e.g. "quest", or "withdraw" to reverse one already recorded). Free string, snake_cased like a GameAction type; the consumer's catalogue alone gives it meaning.</summary>
        public required string Type { get; init; }

        /// <summary>The seated member's name exactly as the model wrote it. Resolved against the roster by the consumer; never falls back to the turn's speaker.</summary>
        public string? Actor { get; init; }

        /// <summary>What was pledged (or, for a withdrawal, which pledge to pull), in the model's own words.</summary>
        public string? Detail { get; init; }

        /// <summary>For a "quest" kind: a named settlement the task is bound to, when the pledge needs one (e.g. "clear the bandits near X"). Null when the pledge names no place.</summary>
        public string? TargetSettlement { get; init; }

        /// <summary>For an "assign_party_role" kind: the party role named (Scout, Engineer, Quartermaster, or Surgeon), in the model's own words. Null for any other kind, or when none was named.</summary>
        public string? TargetRole { get; init; }
    }
}

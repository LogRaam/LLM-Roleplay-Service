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

        /// <summary>For a "dispatch_mission" kind: the companion-mission errand named (GatherNews, Spy, Steal, Barter, or Envoy), in the model's own words. Null for any other kind, or when none was named.</summary>
        public string? TargetMission { get; init; }

        /// <summary>For a "grant_stipend" kind: the proposed denars-per-day amount. Also REUSED by "give_gold" (Partie 1, bringing the existing 1:1 resource verbs to the council): a seated lord's own proposed denars pledged to the player, no separate field. Also REUSED by "tribute" (Partie 8, COUNCIL_ACTIONS.md's Kimi review): the seated enemy leader's own proposed denars-PER-DAY paid to the player, PAY-AS-YOU-GO from their own coffers rather than escrowed like grant_stipend. Null for any other kind, or when none was named/parseable.</summary>
        public int? TargetAmount { get; init; }

        /// <summary>For a "declare_war" kind (the first FACTION-CENTRIC kind, WarCouncil only): the faction named to declare war on, in the model's own words. Null for any other kind, or when none was named.</summary>
        public string? TargetFaction { get; init; }

        /// <summary>
        ///   For a "pledge_against" kind (Partie 1, COUNCIL_ACTIONS.md's schemes block, reusing the existing 1:1
        ///   pledge_against system end to end): the named RIVAL a seated member pledges to move against, in the
        ///   model's own words. A dedicated field rather than reusing TargetSettlement: unlike a fief or a
        ///   settlement, this names a HERO, and folding a person's name into a field literally called
        ///   "TargetSettlement" would be a confusing lie to the next reader. Null for any other kind, or when
        ///   none was named.
        /// </summary>
        public string? TargetRival { get; init; }

        /// <summary>
        ///   For an "arrange_marriage" kind (Partie 1, COUNCIL_ACTIONS.md, "juste arrange_mariage qui est
        ///   valide", WarCouncil only, REUSING the existing 1:1 arrange_marriage system end to end): the
        ///   player-clan kin named to wed, in the model's own words. Deliberately named "player_kin", the SAME
        ///   vocabulary the existing 1:1 <c>arrange_marriage</c> GameAction already teaches, rather than a
        ///   "Target*"-prefixed field: this is a PERSON'S name grounding, not a place/faction/role/rival
        ///   grounding, and reusing the identical wording means the model learns nothing new to propose this at
        ///   a council. Null for any other kind, or when none was named.
        /// </summary>
        public string? PlayerKinName { get; init; }

        /// <summary>
        ///   For an "arrange_marriage" kind: the seated ally's own kin (or the ally themselves, if unwed) named
        ///   to wed <see cref="PlayerKinName" />, in the model's own words. Mirrors the 1:1 action's own
        ///   "target_kin" parameter exactly, see <see cref="PlayerKinName" />'s own doc for why this is not a
        ///   "Target*"-prefixed field. Null for any other kind, or when none was named.
        /// </summary>
        public string? TargetKinName { get; init; }

        /// <summary>
        ///   For a "swap_fiefs" kind (Partie 8, COUNCIL_ACTIONS.md's Kimi review, the player and a seated
        ///   WarCouncil vassal atomically exchange one fief each): the PLAYER's own town or castle offered up in
        ///   the trade, in the model's own words. A "Target*"-prefixed field would be wrong here for the same
        ///   reason PlayerKinName's own doc gives (this names something the PLAYER's own side gives up, not the
        ///   counterpart's), so it mirrors PlayerKinName's naming instead. Null for any other kind, or when none
        ///   was named.
        /// </summary>
        public string? PlayerFiefName { get; init; }

        /// <summary>
        ///   For a "swap_fiefs" kind: the seated vassal's own town or castle offered in exchange for
        ///   <see cref="PlayerFiefName" />, in the model's own words. Mirrors <see cref="TargetKinName" />'s own
        ///   naming exactly. Null for any other kind, or when none was named.
        /// </summary>
        public string? TargetFiefName { get; init; }

        /// <summary>
        ///   For a "release_prisoner" kind (Parley toolkit, rounding out make_peace + tribute, 2026-08-04): the
        ///   named hero captive the PLAYER currently holds, whom the seated enemy leader asks freed as a
        ///   concession, REUSING the existing 1:1 free_prisoner mechanic end to end. A dedicated field rather
        ///   than reusing TargetRival (that field's own doc already reserves it for pledge_against's own
        ///   ADVERSARY grounding) or TargetSettlement (a captive is a HERO, not a place): folding a person's
        ///   name into either would be the same confusing lie TargetRival's own doc warns against. Null for any
        ///   other kind, or when none was named.
        /// </summary>
        public string? TargetPrisonerName { get; init; }
    }
}

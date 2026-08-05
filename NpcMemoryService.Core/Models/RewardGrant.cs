// Code written by Gabriel Mailhot, 13/06/2026.
// Negotiation framework: what an NPC grants the player when a conditional bargain is met.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   What the NPC does for the player when a conditional bargain is fulfilled — the
    ///   reward side of the negotiation framework. A bargain is an <see cref="InformalQuest" />
    ///   inverted: the player performs a verifiable deed, and instead of (or alongside)
    ///   gold and relation, the NPC honors the player's original request — joining their
    ///   party, consenting to a marriage, handing something over.
    ///
    ///   Every grant is executed by the game-state bridge ("the prompt advises, the bridge
    ///   is law"): the LLM may name a grant, but the bridge validates it is mechanically
    ///   possible and refuses otherwise. Values marked STUB are recognized but not yet
    ///   executable — the bridge declines them with a log until their action is built.
    /// </summary>
    public enum RewardGrant
    {
        /// <summary>No special grant — the reward is the ordinary gold/relation pair (default).</summary>
        None,

        /// <summary>The NPC takes service with the player (a wanderer joins the party). The deed is the payment.</summary>
        JoinParty,

        /// <summary>STUB: the NPC gives the player a specific item from their own gear.</summary>
        GiveItem,

        /// <summary>STUB: the NPC grants the player a number of troops from their party.</summary>
        GiveTroops,

        /// <summary>STUB: the NPC consents to a marriage the player requested (their daughter, themselves, a kin).</summary>
        MarriageConsent,

        /// <summary>STUB: the NPC releases a prisoner they hold into the player's custody.</summary>
        ReleasePrisoner,

        /// <summary>
        ///   COUNCIL_ACTIONS.md Partie 5 (the "Caladog" case): the giver's own clan transfers one of its
        ///   towns or castles to the player on completion. Gated ALL THE WAY DOWN behind the host's own
        ///   opt-in (mod: <c>ModSettings.AllowFiefAndMarriageQuestRewards</c>, mirrored here by
        ///   <see cref="EncounterContext.FiefAndMarriageQuestRewardsAllowed" />): with the toggle off the
        ///   consumer refuses this at issuance exactly like the GiveItem/GiveTroops/ReleasePrisoner stubs
        ///   above, never a broken promise. Distinct from a council <c>grant_fief</c> resolution (which moves
        ///   a fief FROM the player's own crown TO a vassal): this moves one FROM the giver's clan TO the
        ///   player, so it reuses the transfer machinery but not the sovereignty direction.
        /// </summary>
        GrantFief,

        /// <summary>
        ///   COUNCIL_ACTIONS.md Partie 5 (the "Caladog" case, Kimi review "Trou 2"): a HEAVIER sibling of
        ///   <see cref="MarriageConsent" />. Where MarriageConsent only records a family's BLESSING (the
        ///   player must still separately seal the wedding), this reward is the actual union itself,
        ///   sealed the moment the quest completes, the honest mechanical answer to a giver who promised
        ///   "do this and I will wed you" rather than merely "do this and you may court me". Same toggle
        ///   gate as <see cref="GrantFief" />; the spouse (the giver themselves, or a kin of their house
        ///   they have authority over) is carried on the SAME <see cref="InformalQuest.MarriageSpouseId" />
        ///   / <see cref="InformalQuest.MarriageSpouseName" /> fields MarriageConsent already uses, since
        ///   the resolution rules (whose hand, whose authority) are identical; only the payout differs.
        /// </summary>
        MarriageReward
    }
}

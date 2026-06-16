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
        ReleasePrisoner
    }
}

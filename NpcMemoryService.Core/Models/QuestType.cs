// Code written by Gabriel Mailhot, 01/06/2026.
// Sprint 11: informal quests — verifiable deed types.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   The kind of deed an <see cref="InformalQuest" /> asks the player to accomplish.
    ///   Every value here is verifiable from real game events (a battle outcome, a hero
    ///   captured or killed, a letter delivered) — the consumer stamps the quest as
    ///   satisfied only when the matching event actually fires with the player involved.
    ///   Roleplay alone never completes a quest.
    /// </summary>
    public enum QuestType
    {
        /// <summary>Defeat bandits in the open near a named settlement.</summary>
        BanditClear,

        /// <summary>Clear a bandit hideout near a named settlement.</summary>
        BanditHideout,

        /// <summary>Win a battle against parties of a specific enemy faction.</summary>
        AttackFaction,

        /// <summary>Defeat a specific enemy lord in battle.</summary>
        AttackLord,

        /// <summary>Raid a village belonging to an enemy faction.</summary>
        RaidVillage,

        /// <summary>Defeat an enemy caravan.</summary>
        AttackCaravan,

        /// <summary>Take part in a successful siege of an enemy town or castle.</summary>
        Siege,

        /// <summary>Take a specific enemy hero prisoner.</summary>
        CapturePrisoner,

        /// <summary>Kill a specific enemy hero.</summary>
        ExecuteEnemy,

        /// <summary>Free a specific allied hero held prisoner.</summary>
        RescuePrisoner,

        /// <summary>Carry a message to a specific recipient hero and deliver it in person.</summary>
        DeliverLetter,

        /// <summary>
        ///   Give a specific amount of gold to the quest giver. Issued by letter when a
        ///   mother asks the player to help support their child financially. Verified when
        ///   the player uses the <c>take_gold</c> action toward the giver in conversation.
        /// </summary>
        ProvideGold,

        /// <summary>
        ///   Locate an enemy army on the move, get close enough to observe its composition
        ///   (troop count, cavalry ratio, notable lords), and report back. Verified by
        ///   proximity to an active army on the campaign map — no battle required.
        /// </summary>
        ScoutArmy,

        /// <summary>
        ///   Hand over goods worth at least a required denar value to the quest giver. The
        ///   deed of the negotiation framework's item-barter: instead of paying in coin, the
        ///   player delivers items of equivalent (premium) value. Verified by a hand-over in
        ///   conversation (the bridge tallies the items' denar value), never by the LLM's
        ///   word. The required value is computed game-side from the reward's worth.
        ///   Appended last to preserve the integer ordinals of the values above in old saves.
        /// </summary>
        DeliverItems,

        /// <summary>
        ///   Hand over a captive the player holds to the quest giver — a specific enemy lord, or any
        ///   lord of an enemy faction. The negotiation framework's prisoner deed: when the player
        ///   already holds a match it is an immediate hand-over; when not, it is a capture-and-deliver
        ///   task. Verified by a real prisoner transfer in conversation, never by the LLM's word.
        ///   Appended last to preserve the integer ordinals above in old saves.
        /// </summary>
        DeliverPrisoner,

        /// <summary>
        ///   Declare war, as the player's own faction, on a faction the giver names. The negotiation
        ///   framework's player-declared-war deed: self-balancing, since the player takes on the war's
        ///   consequences. Verified by a WarDeclared event with the PLAYER's faction as the aggressor
        ///   against the demanded faction — never by being attacked. Appended last for save ordinals.
        /// </summary>
        DeclareWar,

        /// <summary>
        ///   A mission registered by an external mod through the public MissionBoard facade (Extension Surface,
        ///   Volet B). CR issues, journals, frames and rewards it like any quest, but the EXTERNAL mod owns the
        ///   completion signal — it calls <c>MissionBoard.Complete</c> when its own deed is truly done, which stamps
        ///   the quest satisfied. CR cannot verify a foreign deed from a game event, and never pays on the LLM's
        ///   word; the external signal is the gate. Appended last to preserve the integer ordinals above in old saves.
        /// </summary>
        External
    }
}

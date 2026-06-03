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
        ScoutArmy
    }
}

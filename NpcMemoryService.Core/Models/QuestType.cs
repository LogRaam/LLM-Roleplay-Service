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
        External,

        /// <summary>
        ///   Nemesis pillar increment 3 (bounty missions): a lord's scripted (non-LLM) offer to capture OR kill
        ///   a specific tracked nemesis. Unlike <see cref="CapturePrisoner" /> and <see cref="ExecuteEnemy" />,
        ///   which each verify a single outcome, this single quest is satisfied by EITHER a real capture of the
        ///   named hero (the player takes him prisoner) or a real kill (in battle or by execution) dealt by the
        ///   player, whichever comes first ends the hunt. Never issued from a <c>[QUEST]</c> block; the game
        ///   itself constructs and offers it once a living, unresolved nemesis exists. Appended last to preserve
        ///   the integer ordinals above in old saves.
        /// </summary>
        NemesisBounty,

        /// <summary>
        ///   A personal SUPPLY task: bring the giver a COUNT of goods of a named category (horses for their
        ///   cavalry, livestock or grain to feed their people). Verified by a real hand-over of matching goods
        ///   in conversation (the bag settles it, the bridge tallies the count), never by the LLM's word. Uses
        ///   <see cref="InformalQuest.RequiredItemCategory"/> + <see cref="InformalQuest.RequiredItemCount"/>.
        ///   Appended last to preserve the integer ordinals above in old saves.
        /// </summary>
        ProvideGoods,

        /// <summary>
        ///   Win the good graces of ANOTHER character: the giver asks the player to raise a named third person's
        ///   personal regard for them by a set amount (Lordfadooboo, Nexus, 01/10/2026: "a quest to earn the good
        ///   graces of another lord, earn 10 regard with Mesui of the Khuzaits, but issued by a different lord").
        ///   Verified on the mod's own regard ledger against the baseline taken when the task was given
        ///   (<see cref="InformalQuest.RegardBaseline" />, <see cref="InformalQuest.RegardGoal" />), never by the
        ///   LLM's word. Appended last to preserve the integer ordinals above in old saves.
        /// </summary>
        EarnRegard,

        /// <summary>
        ///   Plead the giver's case before a named person (their liege, the head of their house, a lord of another
        ///   realm): Lordfadooboo and fkasad, Nexus, 01/10/2026. Done when that person, in their own conversation
        ///   with the player, is truly won over TOWARD the giver (the sway_opinion verb, stance "for", about the
        ///   giver), never by the LLM's word. Appended last to preserve the integer ordinals above in old saves.
        /// </summary>
        PleadCase,

        /// <summary>
        ///   Bring men for the giver's thin garrison (fkasad, Nexus, 01/10/2026: "Works for garrison reinforcement
        ///   too"): <see cref="InformalQuest.TargetSettlement" /> names the place, <see cref="InformalQuest.RequiredItemCount" />
        ///   the number of soldiers. Verified by a real hand-over in that place (the reinforce_garrison verb moves the
        ///   player's own soldiers onto its walls), never by the LLM's word. Appended last to preserve the integer
        ///   ordinals above in old saves.
        /// </summary>
        ReinforceGarrison,

        /// <summary>
        ///   Go as the giver's envoy and buy back one of his own men from a realm the player is not at war with
        ///   (fkasad, Nexus, 01/10/2026: "Here is 5000 denars. Bring my lad alive. You can keep the change.").
        ///   The game advances the purse (<see cref="InformalQuest.AdvanceGold" />); done when the player frees
        ///   the man he bought, or hands him home, or breaks him out. If the man goes free some other way first,
        ///   the task is voided and the advance returned. Appended last to preserve the integer ordinals above.
        /// </summary>
        RansomEnvoy,

        /// <summary>
        ///   Scout an enemy town from INSIDE, through its gang leader (fkasad, Nexus, 01/10/2026: "actually entering the
        ///   city not like the vanilla where you just have to spend some time nearby. Make contact with local gang
        ///   leader, negotiate with him for intel"). <see cref="InformalQuest.TargetSettlement" /> names the town. Done
        ///   when its gang leader, in his own conversation, agrees to tell (the share_intel verb); the town's true state
        ///   is recorded as the evidence. Appended last to preserve the integer ordinals above.
        /// </summary>
        ScoutTown
    }
}

// Code written by Gabriel Mailhot, 17/05/2026.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Captures the volatile, per-encounter state that the LLM needs to react
    ///   appropriately: where the conversation takes place, the diplomatic
    ///   situation, the player's standing with respect to the NPC, and how much
    ///   time has passed since they last met.
    ///   This is INJECTED into the volatile section of the system prompt so the
    ///   LLM can adapt its tone without us having to bake it into the persistent
    ///   <see cref="NpcProfile" />.
    /// </summary>
    public sealed class EncounterContext
    {
        /// <summary>An empty context (all fields Unknown). Use when no game state is available.</summary>
        public static EncounterContext Empty { get; } = new EncounterContext();

        public int DaysSinceLastMeeting { get; init; } = -1;
        public PlayerStatusVsNpc PlayerStatus { get; init; } = PlayerStatusVsNpc.Unknown;
        public SceneType Scene { get; init; } = SceneType.Unknown;
        public DiplomaticStatus WarStatus { get; init; } = DiplomaticStatus.Unknown;

        /// <summary>
        ///   Ready-to-inject hint about heroes the player mentioned in their last message.
        ///   Null when no hero names were detected. Built by the game-side resolver so the
        ///   NPC can accurately answer questions about third parties — friends, enemies, or
        ///   strangers — and ask for clarification when several people share a name.
        /// </summary>
        public string? ContextualNames { get; init; }

        /// <summary>
        ///   What the NPC is currently doing on the campaign map (laying siege, marching
        ///   with an army toward an objective, traveling to a settlement). Null when it
        ///   cannot be read with confidence — the prompt then tells the NPC not to invent
        ///   a destination rather than feeding it a guess it would treat as fact.
        /// </summary>
        public string? NpcCurrentActivity { get; init; }

        /// <summary>
        ///   Name of the player's current spouse, or null if the player is single or widowed.
        ///   Used to inject the player's marital status into the NPC's consent section so the
        ///   NPC can react according to their own character — refusing to enable infidelity,
        ///   being indifferent, or finding the forbidden element appealing.
        /// </summary>
        public string? PlayerSpouseName { get; init; }

        /// <summary>
        ///   Name of the player's father, or null if unknown. Injected so an NPC that
        ///   refers to the player's parentage uses the real name instead of inventing one.
        ///   A "(the late …)" form is built game-side when the parent is deceased.
        /// </summary>
        public string? PlayerFatherName { get; init; }

        /// <summary>Name of the player's mother, or null if unknown. See <see cref="PlayerFatherName"/>.</summary>
        public string? PlayerMotherName { get; init; }

        /// <summary>
        ///   The player's clan tier (0–6). Tier 0 means no clan or a landless household;
        ///   tier 3+ represents genuine nobility with a fief. Used to signal to NPCs
        ///   whether the player is a credible prospect for an official match (marriage).
        /// </summary>
        public int PlayerClanTier { get; init; } = 0;

        /// <summary>
        ///   The player's rank in the world-wide tournament leaderboard (1 = champion,
        ///   0 = never won a tournament / not on the leaderboard). A rank ≤ 10 means the
        ///   player has a genuine reputation as a tournament fighter — the kind that turns
        ///   heads for reasons that have nothing to do with clan prestige.
        /// </summary>
        public int PlayerArenaRank { get; init; } = 0;

        /// <summary>Total tournament victories for the player. Accompanies <see cref="PlayerArenaRank"/>.</summary>
        public int PlayerArenaWins { get; init; } = 0;

        /// <summary>
        ///   NPCs present during this encounter who can hear the conversation.
        ///   When non-empty, the prompt instructs the NPC to adjust candor accordingly.
        ///   Null or empty list means the conversation is private.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<WitnessEntry>? Witnesses { get; init; }

        /// <summary>
        ///   True when the player has requested a private audience this turn.
        ///   Injected once — cleared after the NPC responds (accepted or refused).
        /// </summary>
        public bool PrivacyRequested { get; init; }

        /// <summary>
        ///   True when this turn is an automatic NPC response to a witness who just
        ///   reacted (Sprint 15C). The final user message in the session is a witness
        ///   statement ([Name]: …), not a player message. When set, the prompt instructs
        ///   the NPC to address the witness rather than the player.
        /// </summary>
        public bool IsWitnessExchangeTurn { get; init; }

        /// <summary>
        ///   True on the final witness-exchange turn before control returns to the player
        ///   (Sprint 15C, cap enforcement). The prompt instructs the NPC not to pose a
        ///   question to another NPC — they may close the beat or redirect to the player.
        /// </summary>
        public bool IsLastWitnessExchange { get; init; }

        /// <summary>
        ///   True when the player is the NPC's captive AND the witnesses present are
        ///   active participants rather than bystanders (Sprint 17 collective captive scene).
        ///   Causes the prompt to instruct the NPC to involve the witnesses explicitly.
        /// </summary>
        public bool IsCollectiveCaptiveScene { get; init; }

        /// <summary>
        ///   The captor's intent for this encounter. Only meaningful when
        ///   <see cref="PlayerStatus"/> == <see cref="PlayerStatusVsNpc.Captive"/>.
        ///   Controls the specific framing injected into the captive prompt section
        ///   and the scene-opening cue sent to the LLM as the first stimulus.
        /// </summary>
        public CaptiveSceneIntent CaptiveIntent { get; init; } = CaptiveSceneIntent.Interrogation;

        /// <summary>
        ///   True on the final beat of a captive scene continuation (mirrors
        ///   <see cref="IsLastWitnessExchange"/> for the 15C loop). When set, the prompt
        ///   instructs the NPC to bring the scene to a definitive conclusion this turn —
        ///   resolve the act and dismiss the prisoner with end_conversation — rather than
        ///   continuing to escalate.
        /// </summary>
        public bool IsFinalSceneBeat { get; init; }

        /// <summary>
        ///   The current dramatic stage of a captive scene, advanced by the host's scene director
        ///   and injected as a single per-turn directive (Intro → Initiate → Intensify → Climax →
        ///   Conclude). Steering the STRUCTURE from outside the model — one named beat at a time,
        ///   with a reminder of what is already done — stops it looping a beat or cramming the arc.
        /// </summary>
        public CaptiveSceneStage SceneStage { get; init; } = CaptiveSceneStage.Intro;

        /// <summary>
        ///   Who is acting this beat: the lead captor, another single member of the band taking
        ///   their turn, or the remaining members acting on the prisoner together at once.
        /// </summary>
        public CaptiveAggressorKind AggressorKind { get; init; } = CaptiveAggressorKind.Lead;

        /// <summary>
        ///   True when the player just resisted, negotiated, or otherwise intervened: the captor
        ///   should REACT to it within the current beat rather than ignore it, but the scene's
        ///   structure still holds.
        /// </summary>
        public bool ReactToPlayerIntervention { get; init; }

        /// <summary>
        ///   True when the captor is a faceless bandit/pirate rather than a named lord.
        ///   Bandits live apart from the world of nobles: they do NOT know the player's
        ///   name, clan, or titles. The prompt suppresses the player's identity and lets
        ///   the bandit only SUSPECT high birth from outward signs (see
        ///   <see cref="PlayerLooksWealthy"/> / <see cref="PlayerLooksImportant"/>).
        /// </summary>
        public bool CaptorIsBandit { get; init; }

        /// <summary>
        ///   True when the player's gear is rich enough that a captor could guess they are
        ///   no common traveler (fine armor, a costly mount, a noble's weapons). Only used
        ///   to shape a bandit captor's suspicion; never reveals the actual name.
        /// </summary>
        public bool PlayerLooksWealthy { get; init; }

        /// <summary>
        ///   True when the player carries enough political weight (high clan standing /
        ///   influence) that word of someone important may have reached even the wilds —
        ///   so a bandit might suspect a valuable, ransomable noble without knowing who.
        /// </summary>
        public bool PlayerLooksImportant { get; init; }

        /// <summary>
        ///   The asking price (denars) for hiring this NPC into the player's party, when
        ///   they are a recruitable companion — computed game-side from the same model the
        ///   vanilla tavern hire uses. Null = not recruitable (a lord, a notable, already
        ///   in service, or the player's clan has no room): the recruitment section and
        ///   the join_party action are then not taught at all.
        /// </summary>
        public int? CompanionAskingPrice { get; init; }

        /// <summary>
        ///   Ready-to-inject block describing a marriage the player could seek from THIS NPC's
        ///   house: the unmarried, suitable kin of the NPC's clan (and the NPC themselves when
        ///   eligible), built game-side so the NPC names only real, marriageable people. Null
        ///   when a marriage request does not apply — the NPC is not their clan's head, has no
        ///   eligible kin, the player is already wed, or this is the player's own clan. When
        ///   present, the prompt teaches the NPC to grant or withhold the family's blessing.
        /// </summary>
        public string? MarriageProspects { get; init; }

        /// <summary>
        ///   DEPRECATED — intentionally left unfed (always null). It once told a recruitable
        ///   NPC the player's exact gold so it would not seal an unaffordable hire, but an NPC
        ///   cannot see the player's liquid coin and naming the sum to the denar broke
        ///   immersion. Affordability is now enforced by the game bridge at execution time, not
        ///   foreseen in the prompt. Kept only to preserve the model's shape; do not re-feed it.
        /// </summary>
        public int? PlayerPurseGold { get; init; }

        /// <summary>
        ///   Produces a natural-language description of this encounter, suitable
        ///   for injection into the LLM system prompt. Returns an empty string
        ///   when all fields are Unknown.
        /// </summary>
        public string ToPromptDescription()
        {
            var parts = new List<string>();

            switch (Scene)
            {
                case SceneType.Outdoor: parts.Add("The encounter takes place outdoors, on the open road or wilderness."); break;
                case SceneType.Settlement: parts.Add("The encounter takes place inside a settlement."); break;
                case SceneType.Keep: parts.Add("The encounter takes place within the walls of a keep."); break;
                case SceneType.Tavern: parts.Add("The encounter takes place in a tavern, surrounded by patrons."); break;
                case SceneType.Dungeon: parts.Add("The encounter takes place in a dungeon."); break;
                case SceneType.Battlefield: parts.Add("The encounter takes place on a battlefield, immediately after combat."); break;
            }

            switch (WarStatus)
            {
                case DiplomaticStatus.AtWar: parts.Add("Your faction and the player's faction are currently at war."); break;
                case DiplomaticStatus.AtPeace: parts.Add("Your factions are at peace."); break;
                case DiplomaticStatus.Allied: parts.Add("Your factions are formally allied or the same."); break;
            }

            switch (PlayerStatus)
            {
                case PlayerStatusVsNpc.Captive: parts.Add("The player is currently your captive."); break;
                case PlayerStatusVsNpc.NpcIsCaptive: parts.Add("You are currently the player's captive."); break;
                case PlayerStatusVsNpc.ClanMember: parts.Add("The player belongs to your own clan."); break;
                case PlayerStatusVsNpc.Vassal: parts.Add("The player is your sworn vassal."); break;
                case PlayerStatusVsNpc.Liege: parts.Add("The player is your liege lord."); break;
                case PlayerStatusVsNpc.Mercenary: parts.Add("The player is a mercenary in your service."); break;
                case PlayerStatusVsNpc.Stranger: parts.Add("You have never met this person before."); break;
                case PlayerStatusVsNpc.Free: /* default, no special note */ break;
            }

            if (DaysSinceLastMeeting == 0) parts.Add("You met the player earlier today.");
            else if (DaysSinceLastMeeting == 1) parts.Add("You last saw the player yesterday.");
            else if (DaysSinceLastMeeting > 1) parts.Add($"You last saw the player {DaysSinceLastMeeting} days ago.");

            return string.Join(" ", parts);
        }
    }

    /// <summary>
    ///   The dramatic beats of a captive scene, walked by the host's scene director. Each captor
    ///   turn performs exactly one stage; the director advances it so the model cannot loop a beat.
    /// </summary>
    public enum CaptiveSceneStage
    {
        Intro,      // brought before the captor; intent stated, menace — no physical act yet
        Initiate,   // the aggression begins — the first act
        Intensify,  // escalation — a different / harder act, never a repeat
        Climax,     // the aggressor finishes / reaches satisfaction
        Conclude    // the scene wraps: prisoner removed, end_conversation
    }

    /// <summary>Who acts this beat in a (possibly collective) captive scene.</summary>
    public enum CaptiveAggressorKind
    {
        Lead,          // the chief / lead captor
        AnotherSingle, // another single member of the band takes their turn
        GroupTogether  // the remaining members act on the prisoner together, at once
    }

    public enum PlayerStatusVsNpc
    {
        Unknown,
        Stranger,
        Free,
        Captive,
        NpcIsCaptive,
        ClanMember,
        Vassal,
        Liege,
        Mercenary
    }

    public enum DiplomaticStatus
    {
        Unknown,
        AtPeace,
        AtWar,
        Allied
    }

    public enum SceneType
    {
        Unknown,
        Outdoor,
        Settlement,
        Keep,
        Tavern,
        Dungeon,
        Battlefield
    }
}
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
// Code written by Gabriel Mailhot, 17/05/2026.

#region

using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>
    ///   Assembles the system prompt for a single NPC dialogue turn.
    ///   Sprint 7.3.B adds the optional <paramref name="encounterContext" />
    ///   parameter for per-encounter volatile state.
    /// </summary>
    public interface IPromptBuilder
    {
        string BuildSystemPrompt(NpcProfile npc, WorldState world, EncounterContext? encounterContext = null);
    }
}
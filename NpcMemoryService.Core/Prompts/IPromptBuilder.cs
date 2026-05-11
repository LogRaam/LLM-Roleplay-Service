using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>
    /// Assembles the system prompt sent to the LLM for a given NPC and world context.
    /// Implement this to customize prompt structure, tone, or injected instructions.
    /// </summary>
    public interface IPromptBuilder
    {
        string BuildSystemPrompt(NpcProfile npc, WorldState world);
    }
}

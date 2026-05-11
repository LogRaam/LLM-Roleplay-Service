using System.Text;
using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>
    /// Default implementation. Assembles a system prompt from the NPC profile
    /// and world context, then appends the canonical response format instructions.
    /// </summary>
    public sealed class PromptBuilder : IPromptBuilder
    {
        public string BuildSystemPrompt(NpcProfile npc, WorldState world)
        {
            var sb = new StringBuilder();

            AppendIdentity(sb, npc);
            AppendMemory(sb, npc);
            AppendWorldState(sb, world);
            AppendFormatInstructions(sb);

            return sb.ToString();
        }

        // ── Sections ──────────────────────────────────────────────────────────

        private static void AppendIdentity(StringBuilder sb, NpcProfile npc)
        {
            sb.AppendLine($"You are {npc.Name}, a character belonging to the {npc.Clan} clan of the {npc.Faction} faction.");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(npc.Personality))
            {
                sb.AppendLine("PERSONALITY:");
                sb.AppendLine(npc.Personality);
                sb.AppendLine();
            }

            string reputationLabel = npc.ReputationWithPlayer switch
            {
                >= 30  => "trusted ally",
                >= 10  => "friendly acquaintance",
                >= -9  => "neutral stranger",
                >= -29 => "distrusted individual",
                _      => "bitter enemy"
            };
            sb.AppendLine($"PLAYER STANDING: {reputationLabel} (score: {npc.ReputationWithPlayer})");
            sb.AppendLine();
        }

        private static void AppendMemory(StringBuilder sb, NpcProfile npc)
        {
            sb.AppendLine("YOUR MEMORY OF THE PLAYER:");
            sb.AppendLine(string.IsNullOrWhiteSpace(npc.MemoryDigest)
                ? "You have never met this player before. This is your first encounter."
                : npc.MemoryDigest);
            sb.AppendLine();
        }

        private static void AppendWorldState(StringBuilder sb, WorldState world)
        {
            sb.AppendLine($"CURRENT WORLD STATE (Day {world.CurrentDay}):");

            if (!string.IsNullOrWhiteSpace(world.ActiveConflicts))
            {
                sb.AppendLine("Active conflicts:");
                sb.AppendLine(world.ActiveConflicts);
            }

            if (!string.IsNullOrWhiteSpace(world.Rumors))
            {
                sb.AppendLine("Rumors circulating:");
                sb.AppendLine(world.Rumors);
            }

            sb.AppendLine();
        }

        private static void AppendFormatInstructions(StringBuilder sb)
        {
            sb.AppendLine("RESPONSE FORMAT:");
            sb.AppendLine("Always structure your response using the sections below.");
            sb.AppendLine("Only include [EVENT] when something significant occurs.");
            sb.AppendLine("Only include [REPUTATION] when the player's standing genuinely changes.");
            sb.AppendLine();
            sb.AppendLine("[DIALOGUE]");
            sb.AppendLine("Your in-character response here.");
            sb.AppendLine("[/DIALOGUE]");
            sb.AppendLine();
            sb.AppendLine("[MEMORY]");
            sb.AppendLine("topic: brief_topic_keyword");
            sb.AppendLine("sentiment: your_current_feeling_toward_player");
            sb.AppendLine("decision: any_decision_reached (omit if none)");
            sb.AppendLine("[/MEMORY]");
            sb.AppendLine();
            sb.AppendLine("[EVENT]");
            sb.AppendLine("type: conflict|betrayal|confrontation|collaboration|flirt|intimacy|first_meeting|other");
            sb.AppendLine("summary: One sentence describing what happened.");
            sb.AppendLine("[/EVENT]");
            sb.AppendLine();
            sb.AppendLine("[REPUTATION]");
            sb.AppendLine("clan_delta: +N or -N");
            sb.AppendLine("faction_delta: +N or -N");
            sb.AppendLine("[/REPUTATION]");
            sb.AppendLine();
            sb.AppendLine("Stay in character at all times. Never break the fourth wall. Never explain the format.");
        }
    }
}

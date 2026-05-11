namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// A reputation change declared by the LLM via the [REPUTATION] section.
    /// Either delta may be null when not applicable.
    /// </summary>
    public sealed record ReputationDelta(
        int? ClanDelta = null,
        int? FactionDelta = null);
}
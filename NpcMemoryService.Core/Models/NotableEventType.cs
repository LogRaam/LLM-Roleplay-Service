namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// Categorizes a notable event between the player and an NPC.
    /// <c>Other</c> is the fallback when the LLM emits an unrecognized type.
    /// </summary>
    public enum NotableEventType
    {
        FirstMeeting,
        Conflict,
        Collaboration,
        Flirt,
        Intimacy,
        Betrayal,
        Confrontation,
        Other
    }
}
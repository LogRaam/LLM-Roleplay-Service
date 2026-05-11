namespace NpcMemoryService.Core.Models
{
    /// <summary>One turn in a conversation, in our internal protocol.</summary>
    public sealed record LlmMessage(MessageRole Role, string Content);
}

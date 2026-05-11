// Code written by Gabriel Mailhot, 10/05/2026.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   Compact summary of a single conversation, emitted by the LLM
   ///   via the [MEMOIRE] section. Captures topic, NPC sentiment toward
   ///   the player, and any meaningful decision reached.
   /// </summary>
   public sealed record ConversationMemory(string Topic, string Sentiment, string? Decision = null);
}
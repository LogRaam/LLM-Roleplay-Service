using System.Collections.Generic;

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// Holds the in-progress conversation history for a single dialogue session.
    /// Create a new instance per conversation; do not reuse across dialogues.
    /// </summary>
    public sealed class ChatSession
    {
        private readonly List<LlmMessage> _messages = new List<LlmMessage>();

        public IReadOnlyList<LlmMessage> Messages => _messages;

        /// <summary>Records a player message and appends it to the history.</summary>
        public void AddPlayerMessage(string content) =>
            _messages.Add(new LlmMessage(MessageRole.User, content));

        /// <summary>Records an NPC response and appends it to the history.</summary>
        public void AddNpcMessage(string content) =>
            _messages.Add(new LlmMessage(MessageRole.Assistant, content));

        public bool IsEmpty => _messages.Count == 0;

        /// <summary>
        /// Removes the last message. Used by the service to roll back
        /// an unanswered player message when the LLM call fails.
        /// </summary>
        public void RollbackLastMessage()
        {
            if (_messages.Count > 0)
                _messages.RemoveAt(_messages.Count - 1);
        }
    }
}

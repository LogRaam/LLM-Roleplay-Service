// Code written by Gabriel Mailhot, 11/05/2026.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Holds the in-progress conversation history for a single dialogue session.
    ///   Create a new instance per conversation; do not reuse across dialogues.
    /// </summary>
    public sealed class ChatSession
    {
        private readonly List<LlmMessage> _messages = new List<LlmMessage>();

        public bool IsEmpty => _messages.Count == 0;

        public IReadOnlyList<LlmMessage> Messages => _messages;

        /// <summary>Records an NPC response and appends it to the history.</summary>
        public void AddNpcMessage(string content) =>
            _messages.Add(new LlmMessage(MessageRole.Assistant, content));

        /// <summary>Records a player message and appends it to the history.</summary>
        public void AddPlayerMessage(string content) =>
            _messages.Add(new LlmMessage(MessageRole.User, content));

        /// <summary>
        ///   Removes the last message. Used by the service to roll back
        ///   an unanswered player message when the LLM call fails.
        /// </summary>
        public void RollbackLastMessage()
        {
            if (_messages.Count > 0)
                _messages.RemoveAt(_messages.Count - 1);
        }

        /// <summary>
        ///   Injects a witness statement into the conversation history as a user-role
        ///   message so the main NPC's LLM can see and react to it on the next turn.
        ///   Format: "[SpeakerName]: content" — the NPC recognises this as a third-party
        ///   voice, not the player's words, and may acknowledge it naturally.
        /// </summary>
        public void AddWitnessStatement(string speakerName, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            var prefix = string.IsNullOrWhiteSpace(speakerName) ? "" : $"[{speakerName}]: ";
            _messages.Add(new LlmMessage(MessageRole.User, prefix + content));
        }

        // Purpose: lets the caller seed the conversation with the NPC's opening line
        // from the vanilla game dialogue, so the LLM continues from what the player
        // just heard rather than starting cold.
        // ─────────────────────────────────────────────────────────────────────────────
        public void SeedNpcOpening(string openingText)
        {
            if (string.IsNullOrWhiteSpace(openingText)) return;
            if (Messages.Count > 0) return; // only seed on a fresh session

            // The session uses the same Add path as AddNpcMessage but does not require
            // a prior player turn. The role string matches what the LLM client expects
            // when serializing to the OpenRouter "assistant" role.
            AddNpcMessage(openingText);
        }
    }
}
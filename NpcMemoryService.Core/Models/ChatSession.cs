// Code written by Gabriel Mailhot, 03/06/2026.

#region

using System;
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
      private readonly List<LlmMessage> _messages = new();

      public bool IsEmpty => _messages.Count == 0;

      public IReadOnlyList<LlmMessage> Messages => _messages;

      /// <summary>Records an NPC response and appends it to the history.</summary>
      public void AddNpcMessage(string content) =>
         _messages.Add(new LlmMessage(MessageRole.Assistant, content));

      /// <summary>Records a player message and appends it to the history.</summary>
      public void AddPlayerMessage(string content) =>
         _messages.Add(new LlmMessage(MessageRole.User, content));

      /// <summary>
      ///   Injects a witness statement into the conversation history as a user-role
      ///   message so the main NPC's LLM can see and react to it on the next turn.
      ///   Format: "[SpeakerName]: content" — the NPC recognises this as a third-party
      ///   voice, not the player's words, and may acknowledge it naturally.
      /// </summary>
      public void AddWitnessStatement(string speakerName, string content)
      {
         if (string.IsNullOrWhiteSpace(content)) return;
         string prefix = string.IsNullOrWhiteSpace(speakerName)
            ? ""
            : $"[{speakerName}]: ";
         _messages.Add(new LlmMessage(MessageRole.User, prefix + content));
      }

      /// <summary>Cap on the prior-exchange block. Its job is to bridge, not to replay a monologue.</summary>
      public const int PriorExchangeMaxChars = 1200;

      /// <summary>
      ///   Records the vanilla exchange that happened immediately BEFORE this chat opened, as a single
      ///   labelled block of history rather than as turns.
      ///   <para>
      ///     Player report 2026-09-08: a wanderer restarted the conversation at every reply, greeting the player
      ///     by name and introducing himself over and over, once even under the wrong name. The cause was that
      ///     the nine lines of his vanilla backstory ritual had been seeded as alternating TURNS, and that
      ///     sequence opens with the player saying "My name is Arwa, sir. Tell me about yourself". So the
      ///     conversation the model saw began with an introduction, and it kept answering it. The history was
      ///     never lost (the prompt grew every turn); it was framed as something awaiting a reply.
      ///   </para>
      ///   <para>
      ///     Gabriel's ruling: the vanilla conversation is HISTORY, and its only value is to bridge the vanilla
      ///     exchange into the chat. So it arrives as one message that says so, in the same spirit as
      ///     <see cref="AddWitnessStatement" />, where a prefix is what tells the model whose words these are.
      ///     Trimmed from the FRONT when long, because a bridge needs the most recent ground, not the oldest.
      ///   </para>
      /// </summary>
      public void AddPriorExchange(string exchange)
      {
         if (string.IsNullOrWhiteSpace(exchange)) return;

         string body = exchange.Trim();
         if (body.Length > PriorExchangeMaxChars)
            body = "..." + body.Substring(body.Length - PriorExchangeMaxChars);

         _messages.Add(new LlmMessage(MessageRole.User,
            "[What was already said, just before this conversation. This is the ground you are both standing on, "
            + "not a question waiting on you. Do not greet again, do not introduce yourself again, and do not "
            + "answer any of it afresh: carry on from here.]" + Environment.NewLine + body));
      }

      /// <summary>
      ///   Removes the last message. Used by the service to roll back
      ///   an unanswered player message when the LLM call fails.
      /// </summary>
      public void RollbackLastMessage()
      {
         if (_messages.Count > 0)
            _messages.RemoveAt(_messages.Count - 1);
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
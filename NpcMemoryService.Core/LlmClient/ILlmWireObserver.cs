// Code written by Gabriel Mailhot, 15/09/2026.
// A seat beside the wire, so the next cache defect is found from the inside.
//
// fkasad found the prompt cache dying on turn two (Nexus, 15/09/2026) by saving two successive prompts
// and diffing them by hand. He had no source access and no hook: he worked from whatever his provider's
// dashboard would show him. That the defect was real is one thing; that finding it took that is the
// thing worth fixing.
//
// The capture point has to be HERE, at the client, and not in the mod above it. This is where the wire
// format is assembled and where the cache_control breakpoint is placed, and the breakpoint is the whole
// subject. A capture in the host would show the text and miss the only detail that decides the bill.
//
// Null by default and never in the way: no observer, no work, no allocation. It is deliberately an
// interface on the client rather than a file writer, so this assembly stays free of I/O and whoever
// attaches one decides what it costs - a log line, a file, a counter, a test assertion.

#region

using System;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.LlmClient
{
   /// <summary>
   ///   Sees each request as it goes out. Implementations MUST NOT throw and MUST NOT block: the client
   ///   calls this on the request path, guards it, and discards anything it raises rather than failing a
   ///   player's conversation over a diagnostic.
   /// </summary>
   public interface ILlmWireObserver
   {
      void OnRequest(LlmWireSnapshot snapshot);
   }

   /// <summary>
   ///   One outgoing request, described in the terms its cost is decided by. The prefix and tail are kept
   ///   as TEXT rather than only as lengths so an observer can diff them itself - which is exactly what
   ///   the player had to do by hand.
   /// </summary>
   public sealed record LlmWireSnapshot
   {
      /// <summary>The model this went to, which is also who honours (or ignores) the breakpoint.</summary>
      public string? Model { get; init; }

      /// <summary>
      ///   WHO the request was about, when the caller said. An observer MUST key its history on this as well
      ///   as the model: two characters speaking in turn share a model and legitimately differ in prompt, so
      ///   comparing across them reports a loss that never happened.
      /// </summary>
      public string? Subject { get; init; }

      /// <summary>Whether a cache breakpoint was actually requested. False means every turn pays full price by design.</summary>
      public bool CachingRequested { get; init; }

      /// <summary>
      ///   The part carrying the breakpoint, or null when the prompt was not split - in which case the
      ///   WHOLE system prompt is the cached block and any per-turn change busts all of it.
      /// </summary>
      public string? StablePrefix { get; init; }

      /// <summary>The per-turn remainder, sent fresh every time.</summary>
      public string? DynamicTail { get; init; }

      /// <summary>Characters in the conversation messages, which grow with the conversation and are NOT behind the breakpoint.</summary>
      public int MessageChars { get; init; }

      public int MessageCount { get; init; }

      /// <summary>The serialized body, for an observer that wants the literal bytes.</summary>
      public string? Json { get; init; }

      public DateTime AtUtc { get; init; } = DateTime.UtcNow;

      /// <summary>Total system prompt size, prefix and tail together.</summary>
      public int SystemChars => (StablePrefix?.Length ?? 0) + (DynamicTail?.Length ?? 0);
   }
}

// Code written by Gabriel Mailhot, 23/06/2026.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   A completion request in our internal protocol.
   ///   Providers map this to their own wire format.
   /// </summary>
   public sealed class LlmRequest
   {
      /// <summary>Conversation history, alternating User / Assistant turns.</summary>
      public required IReadOnlyList<LlmMessage> Messages { get; init; }

      public LlmParameters Parameters { get; init; } = new();

      /// <summary>
      ///   Overrides the model this ONE request targets, ignoring the client's normally-resolved model
      ///   (its <c>ModelProvider</c> / configured id). Null or blank leaves the resolved model unchanged.
      ///   Lets a single call (the PROSE+EXTRACT extractor) hit a different, cheaper model without touching
      ///   any global model selection, so there is no async race with a shared model-mode setting.
      /// </summary>
      public string? ModelOverride { get; init; }

      /// <summary>
      ///   The STABLE prefix of <see cref="SystemPrompt" /> (must be a literal prefix) — the part that does
      ///   not change turn-to-turn within a conversation (identity, persona, instructions). When set and
      ///   caching is on, the cache breakpoint goes here, so only this prefix is cached and the dynamic
      ///   per-turn tail (current encounter, rumours, names) is sent fresh. Null = cache the whole prompt.
      /// </summary>
      public string? StableSystemPrompt { get; init; }

      /// <summary>System instructions: NPC profile, memory, world state, format rules.</summary>
      public required string SystemPrompt { get; init; }

      /// <summary>
      ///   WHO this request is about — a stable identity for the character being spoken to (or read for).
      ///   DIAGNOSTIC ONLY: nothing in the request path reads it, it is never sent to any provider, and a
      ///   null changes no behaviour.
      ///
      ///   It exists because a cache report without it is wrong in the normal case. Two characters speaking
      ///   in turn on the SAME model legitimately have different prompts, so comparing one's prefix against
      ///   the other's reports a catastrophic loss where nothing went wrong at all — which is exactly what
      ///   cr.wire printed on its first run in a group scene ("81.5% discarded", for a turn that had simply
      ///   been routed to another lord). An observer keys its history on this instead.
      /// </summary>
      public string? Subject { get; init; }
   }
}
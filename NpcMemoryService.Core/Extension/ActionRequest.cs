// Code written by Gabriel Mailhot, 03/07/2026.
// Extension Surface, Actions volet (spike): what the host mod hands an external verb's Execute callback
// (the request), and what it hands back (the result). Companion pair to VerbFacts.

using System.Collections.Generic;

namespace NpcMemoryService.Core.Extension
{
   /// <summary>One invocation of an external verb: which one, its parsed [ACTION] parameters, and the live facts.</summary>
   public sealed class ActionRequest
   {
      /// <summary>The verb name being invoked (matches <see cref="ExternalVerbContract.Name" />).</summary>
      public string VerbName { get; set; } = string.Empty;

      /// <summary>The [ACTION] block's free-form parameters, exactly as the LLM emitted them (null when the action carried none).</summary>
      public IReadOnlyDictionary<string, string>? Parameters { get; set; }

      /// <summary>The engine-agnostic facts about the conversation partner, freshly projected by the host (always set by the caller).</summary>
      public VerbFacts Facts { get; set; } = null!;
   }

   /// <summary>The outcome of executing an external verb, mirrored back into the host's own chat-line outcome.</summary>
   public sealed class ActionResult
   {
      /// <summary>Short, in-character-adjacent description shown in the chat window.</summary>
      public string Message { get; set; } = string.Empty;

      /// <summary>True when the outcome is favorable to the player (drives the chat line color on the host side).</summary>
      public bool Success { get; set; }
   }
}

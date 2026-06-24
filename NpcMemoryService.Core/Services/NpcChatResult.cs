// Code written by Gabriel Mailhot, 10/06/2026.

#region

using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Services
{
   /// <summary>
   ///   The result of a single NPC chat turn.
   ///   Check <see cref="IsSuccess" /> before accessing <see cref="Response" />.
   /// </summary>
   public sealed class NpcChatResult
   {
      public string? ErrorMessage { get; private init; }

      /// <summary>Provider finish reason for this turn ("stop", "length", …); null when not reported.</summary>
      public string? FinishReason { get; private init; }

      public bool IsSuccess { get; private init; }
      public ParsedResponse? Response { get; private init; }
      public LlmUsage? Usage { get; private init; }

      public static NpcChatResult Failure(string errorMessage) => new() {IsSuccess = false, ErrorMessage = errorMessage};

      public static NpcChatResult Success(ParsedResponse response, LlmUsage? usage, string? finishReason = null) => new() {IsSuccess = true, Response = response, Usage = usage, FinishReason = finishReason};
   }
}
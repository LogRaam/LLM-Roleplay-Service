using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.Services
{
    /// <summary>
    /// The result of a single NPC chat turn.
    /// Check <see cref="IsSuccess"/> before accessing <see cref="Response"/>.
    /// </summary>
    public sealed class NpcChatResult
    {
        public bool           IsSuccess    { get; private init; }
        public ParsedResponse? Response    { get; private init; }
        public LlmUsage?      Usage        { get; private init; }
        public string?        ErrorMessage { get; private init; }

        public static NpcChatResult Success(ParsedResponse response, LlmUsage? usage) =>
            new NpcChatResult { IsSuccess = true, Response = response, Usage = usage };

        public static NpcChatResult Failure(string errorMessage) =>
            new NpcChatResult { IsSuccess = false, ErrorMessage = errorMessage };
    }
}

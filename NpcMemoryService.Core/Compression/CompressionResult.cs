using System.Collections.Generic;
using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.Compression
{
    /// <summary>
    /// The outcome of a compression pass. On success, <see cref="KeptEvents"/>
    /// contains the subset of the original events to retain, and
    /// <see cref="BackgroundSummary"/> captures the gist of those dropped.
    /// </summary>
    public sealed class CompressionResult
    {
        public bool                       IsSuccess         { get; init; }
        public IReadOnlyList<NotableEvent> KeptEvents       { get; init; } = new List<NotableEvent>();
        public int                        DroppedCount      { get; init; }
        public string?                    BackgroundSummary { get; init; }
        public string?                    ErrorMessage      { get; init; }
        public LlmUsage?                  Usage             { get; init; }

        public static CompressionResult Success(
            IReadOnlyList<NotableEvent> kept,
            int droppedCount,
            string? backgroundSummary,
            LlmUsage? usage) =>
            new CompressionResult
            {
                IsSuccess         = true,
                KeptEvents        = kept,
                DroppedCount      = droppedCount,
                BackgroundSummary = backgroundSummary,
                Usage             = usage
            };

        public static CompressionResult Failure(string errorMessage) =>
            new CompressionResult
            {
                IsSuccess    = false,
                ErrorMessage = errorMessage
            };

        /// <summary>
        /// Returned when no compression is needed (e.g., too few events to bother).
        /// </summary>
        public static CompressionResult NoOp(IReadOnlyList<NotableEvent> originalEvents) =>
            new CompressionResult
            {
                IsSuccess  = true,
                KeptEvents = originalEvents
            };
    }
}

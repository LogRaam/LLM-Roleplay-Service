using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.Parsing
{
    /// <summary>
    /// Parses a raw LLM response string into a structured <see cref="ParsedResponse"/>.
    /// Implementations must be robust to malformed input and never throw —
    /// missing or invalid sections degrade to <c>null</c>, dialogue degrades to empty.
    /// </summary>
    public interface IResponseParser
    {
        ParsedResponse Parse(string rawResponse);
    }
}

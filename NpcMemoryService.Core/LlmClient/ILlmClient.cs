using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.LlmClient
{
    /// <summary>
    /// Sends a completion request and returns a response,
    /// both expressed in our internal protocol.
    /// Implementations never throw — failures are returned as
    /// an unsuccessful <see cref="LlmResponse"/>.
    /// </summary>
    public interface ILlmClient
    {
        Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default);
    }
}

using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.Compression
{
    /// <summary>
    /// Compresses an NPC's event history by identifying which past events can
    /// be dropped because later, stronger events have superseded them.
    /// Implementations must apply the result to the profile separately —
    /// this interface only computes the decision.
    /// </summary>
    public interface IMemoryCompressor
    {
        Task<CompressionResult> CompressAsync(NpcProfile profile, CancellationToken ct = default);
    }
}

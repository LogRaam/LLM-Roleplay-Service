using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.Models;

namespace NpcMemoryService.Core.Storage
{
    /// <summary>
    /// In-memory store for NPC profiles. All read/write operations are synchronous
    /// and perform no I/O — they operate on an in-memory working set.
    ///
    /// Persistence is explicit:
    /// - Call <see cref="InitializeAsync"/> once at session start to populate memory
    ///   from the underlying storage (files, campaign save, etc.).
    /// - Call <see cref="CommitAsync"/> when the game saves to flush memory to storage.
    ///
    /// This design ensures that a crash mid-conversation leaves no partial state
    /// on disk — only a clean CommitAsync persists changes.
    /// </summary>
    public interface INpcMemoryStore
    {
        /// <summary>
        /// Loads all persisted profiles into the in-memory working set.
        /// Clears any prior in-memory state (simulates a Load Game).
        /// Must be called once before any Get/Set operations.
        /// </summary>
        Task InitializeAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns a tracked reference to the profile for <paramref name="npcId"/>,
        /// or null if not found. Mutations to the returned instance are visible in
        /// subsequent Get calls and persisted on Commit — no further Set call is required
        /// for an existing profile.
        /// </summary>
        NpcProfile? Get(string npcId);

        /// <summary>
        /// Registers a new profile in the working set, or replaces an existing one.
        /// Required only for profiles created outside the store; mutations on a profile
        /// obtained via <see cref="Get"/> are tracked automatically.
        /// </summary>
        void Set(NpcProfile profile);

        /// <summary>Returns all NPC ids currently in the working set.</summary>
        IReadOnlyList<string> ListIds();

        /// <summary>
        /// Removes a profile from the working set. The underlying storage is updated
        /// only at the next <see cref="CommitAsync"/>.
        /// </summary>
        void Remove(string npcId);

        /// <summary>
        /// Flushes the entire working set to persistent storage atomically:
        /// either all changes succeed, or the prior state is preserved on disk.
        /// Profiles removed from the working set are also removed from storage.
        /// In Bannerlord, call this from the save hook of CampaignBehaviorBase.
        /// </summary>
        Task CommitAsync(CancellationToken ct = default);
    }
}

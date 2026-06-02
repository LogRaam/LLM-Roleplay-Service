// Code written by Gabriel Mailhot, 11/05/2026.

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Storage
{
    /// <summary>
    ///   File-based implementation of <see cref="INpcMemoryStore" />.
    ///   One JSON file per NPC: <c>{directory}/{npcId}.json</c>.
    ///   Commit strategy: each profile is serialized to a sibling <c>.tmp</c> file
    ///   first, then atomically renamed to its final location. Profiles removed
    ///   from the working set have their backing file deleted. On crash mid-commit,
    ///   either the new state is in place or the previous state is preserved per file.
    /// </summary>
    public sealed class JsonFileNpcMemoryStore : INpcMemoryStore
    {
        private const string TempSuffix = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly JsonFileStoreConfig _config;
        private readonly Dictionary<string, NpcProfile> _profiles = new Dictionary<string, NpcProfile>();

        public JsonFileNpcMemoryStore(JsonFileStoreConfig config)
        {
            _config = config;
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            EnsureDirectoryExists();

            // Phase 1: write all profiles to temp files (no destructive change yet).
            var tempPaths = new List<(string Temp, string Final)>();
            try
            {
                foreach (NpcProfile profile in _profiles.Values)
                {
                    ct.ThrowIfCancellationRequested();

                    var finalPath = GetPath(profile.Id);
                    var tempPath = finalPath + TempSuffix;

                    using (FileStream stream = File.Open(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        await JsonSerializer
                              .SerializeAsync(stream, profile, SerializerOptions, ct)
                              .ConfigureAwait(false);
                    }

                    tempPaths.Add((tempPath, finalPath));
                }
            }
            catch
            {
                // Roll back: best-effort cleanup of any temp files we wrote.
                foreach (var (temp, _) in tempPaths)
                    try
                    {
                        if (File.Exists(temp)) File.Delete(temp);
                    }
                    catch { }

                throw;
            }

            // Phase 2: atomically swap each temp into its final location.
            foreach (var (temp, final) in tempPaths)
            {
                if (File.Exists(final)) File.Delete(final);
                File.Move(temp, final);
            }

            // Phase 3: delete orphan files (profiles removed from the working set).
            var keptIds = new HashSet<string>(_profiles.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(_config.Directory, "*.json"))
            {
                var id = Path.GetFileNameWithoutExtension(file);
                if (!keptIds.Contains(id))
                    File.Delete(file);
            }
        }

        // ── In-memory operations (no I/O) ─────────────────────────────────────

        public NpcProfile? Get(string npcId) =>
            _profiles.TryGetValue(npcId, out NpcProfile? profile)
                ? profile
                : null;

        // ── Persistence ───────────────────────────────────────────────────────

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            _profiles.Clear();

            if (!Directory.Exists(_config.Directory)) return;

            foreach (var file in Directory.GetFiles(_config.Directory, "*.json"))
            {
                ct.ThrowIfCancellationRequested();

                using FileStream stream = File.OpenRead(file);
                NpcProfile? profile = await JsonSerializer
                                            .DeserializeAsync<NpcProfile>(stream, SerializerOptions, ct)
                                            .ConfigureAwait(false);

                if (profile != null)
                    _profiles[profile.Id] = profile;
            }
        }

        public IReadOnlyList<string> ListIds() =>
            new List<string>(_profiles.Keys);

        public void Remove(string npcId) =>
            _profiles.Remove(npcId);

        public void Set(NpcProfile profile) =>
            _profiles[profile.Id] = profile;

        #region private

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_config.Directory))
                Directory.CreateDirectory(_config.Directory);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GetPath(string npcId) =>
            Path.Combine(_config.Directory, $"{npcId}.json");

        #endregion
    }
}
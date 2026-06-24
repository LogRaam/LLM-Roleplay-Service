// Code written by Gabriel Mailhot, 09/06/2026.

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

      // Newtonsoft is case-insensitive on read by default, so no equivalent of
      // PropertyNameCaseInsensitive is needed.
      private static readonly JsonSerializerSettings SerializerSettings = new() {
         Formatting = Formatting.Indented,
         NullValueHandling = NullValueHandling.Ignore
      };

      private readonly JsonFileStoreConfig _config;
      private readonly Dictionary<string, NpcProfile> _profiles = new();

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

               string finalPath = GetPath(profile.Id);
               string tempPath = finalPath + TempSuffix;

               string serialized = JsonConvert.SerializeObject(profile, SerializerSettings);
               using (var writer = new StreamWriter(File.Open(tempPath, FileMode.Create, FileAccess.Write)))
               {
                  await writer.WriteAsync(serialized).ConfigureAwait(false);
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
         foreach (string file in Directory.GetFiles(_config.Directory, "*.json"))
         {
            string? id = Path.GetFileNameWithoutExtension(file);
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

         foreach (string file in Directory.GetFiles(_config.Directory, "*.json"))
         {
            ct.ThrowIfCancellationRequested();

            string serialized;
            using (var reader = new StreamReader(File.OpenRead(file)))
            {
               serialized = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            var profile = JsonConvert.DeserializeObject<NpcProfile>(serialized, SerializerSettings);

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
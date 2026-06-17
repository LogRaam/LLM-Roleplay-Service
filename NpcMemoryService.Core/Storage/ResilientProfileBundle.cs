// Code written by Gabriel Mailhot, 16/06/2026.

#region

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Storage
{
   /// <summary>
   ///   Resilient JSON (de)serialization of a whole profile set into a single string blob.
   ///
   ///   Built for stores that persist every profile as ONE document — e.g. a game's native
   ///   single-slot save (Bannerlord's <c>IDataStore</c>, a config blob, a database column).
   ///   In that layout a single un-round-trippable or malformed profile would otherwise make
   ///   the entire memory unreadable. This helper isolates each profile so a bad one is dropped
   ///   (with a diagnostic) while its siblings survive — both on write and on read.
   ///
   ///   File-per-profile stores (e.g. <see cref="JsonFileNpcMemoryStore" />) get this isolation
   ///   for free from the filesystem and do not need this helper.
   ///
   ///   The helper never touches any game API; it is engine-agnostic. A consumer wraps the
   ///   returned/accepted string in whatever its host save system expects (a byte[], a field,
   ///   a row), keeping the fragile part written once and tested here.
   /// </summary>
   public static class ResilientProfileBundle
   {
      /// <summary>
      ///   Called when a profile is left OUT of the serialized blob because it does not survive
      ///   a serialize→deserialize round-trip. Lets the consumer log the offending NPC.
      /// </summary>
      /// <param name="profileId">The key of the dropped profile.</param>
      /// <param name="profileName">The profile's display name, if available.</param>
      /// <param name="error">The exception raised while proving the round-trip.</param>
      public delegate void DroppedProfileHandler(string profileId, string? profileName, Exception error);

      /// <summary>
      ///   Called when a profile is SKIPPED while reading the blob because that single entry
      ///   could not be materialized. Sibling entries are still loaded.
      /// </summary>
      /// <param name="profileId">The key of the skipped profile.</param>
      /// <param name="error">The exception raised while materializing the entry.</param>
      public delegate void SkippedProfileHandler(string profileId, Exception error);

      /// <summary>
      ///   Serializes <paramref name="profiles" /> into a single JSON document. Each profile is
      ///   first proven to survive a full serialize→deserialize round-trip; any that cannot is
      ///   omitted (reported via <paramref name="onDropped" />) so one bad profile can never make
      ///   the whole blob unreadable. The result is always a valid document, even if empty.
      /// </summary>
      public static string Serialize(
         IReadOnlyDictionary<string, NpcProfile> profiles,
         DroppedProfileHandler? onDropped = null)
      {
         var safe = new Dictionary<string, NpcProfile>(profiles.Count);

         foreach (KeyValuePair<string, NpcProfile> kvp in profiles)
         {
            try
            {
               string one = JsonConvert.SerializeObject(kvp.Value);
               JsonConvert.DeserializeObject<NpcProfile>(one); // prove it reads back
               safe[kvp.Key] = kvp.Value!; // non-null by the IReadOnlyDictionary value contract
            }
            catch (Exception ex)
            {
               onDropped?.Invoke(kvp.Key, kvp.Value?.Name, ex);
            }
         }

         return JsonConvert.SerializeObject(safe, Formatting.None);
      }

      /// <summary>
      ///   Reads a JSON document produced by <see cref="Serialize" /> back into a profile map.
      ///   The outer document is parsed as raw tokens, then each entry is materialized
      ///   individually so a single malformed entry is skipped (reported via
      ///   <paramref name="onSkipped" />) rather than failing the whole load.
      ///
      ///   A null/empty input yields an empty map. A malformed OUTER document still throws —
      ///   the consumer is expected to catch that and decide how to recover (e.g. drop memory
      ///   for this load so the game can still open).
      /// </summary>
      public static Dictionary<string, NpcProfile> Deserialize(
         string? json,
         SkippedProfileHandler? onSkipped = null)
      {
         var result = new Dictionary<string, NpcProfile>();
         if (string.IsNullOrEmpty(json)) return result;

         var root = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json!); // non-null past the IsNullOrEmpty guard
         if (root == null) return result;

         foreach (KeyValuePair<string, JToken> kvp in root)
         {
            try
            {
               NpcProfile? profile = kvp.Value.ToObject<NpcProfile>();
               if (profile != null) result[kvp.Key] = profile;
            }
            catch (Exception ex)
            {
               onSkipped?.Invoke(kvp.Key, ex);
            }
         }

         return result;
      }
   }
}

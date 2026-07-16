// Code written by Gabriel Mailhot, 15/07/2026.
// Companion to ReasoningTraceStripper: the stripper stops NEW chain-of-thought leaks at parse time, but a save
// made before the fix already holds corrupted memories (a player reported memory lines reading "The user wants
// me to write a memory line from Mesui's perspective..."), and those are re-displayed on the memory screen and
// re-injected into every prompt forever. This one-shot pass heals the stored prose in place. It is idempotent
// by construction (stripping already-clean text changes nothing), so the host can run it on every session
// launch without a version marker.

#region

using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Services
{
   /// <summary>
   ///   Heals a profile whose stored prose carries a leaked reasoning trace: each event summary and the
   ///   background context are run through <see cref="ReasoningTraceStripper" />. A summary that strips to
   ///   NOTHING (the whole memory was trace, the reported truncated-<c>&lt;think&gt;</c> case) is replaced by a
   ///   modest "the details have faded" line rather than invented content or a dropped event: the event's day
   ///   and type are real history worth keeping, only its prose was lost to the leak.
   /// </summary>
   public static class ReasoningTraceBackfill
   {
      /// <summary>What a memory becomes when its entire stored prose was reasoning trace.</summary>
      public const string LostMemoryLine = "We spoke, though the details of it have faded from me.";

      /// <summary>
      ///   Scrubs <paramref name="profile" /> in place. Returns true when anything changed, so the caller
      ///   persists only profiles that were actually healed.
      /// </summary>
      public static bool Scrub(NpcProfile profile)
      {
         if (profile == null) return false;

         var changed = false;

         for (var i = 0; i < profile.Events.Count; i++)
         {
            NotableEvent ev = profile.Events[i];
            if (string.IsNullOrEmpty(ev?.summary)) continue;

            string cleaned = ReasoningTraceStripper.Strip(ev!.summary);
            if (cleaned == ev.summary) continue;

            profile.Events[i] = ev with {summary = cleaned.Length > 0 ? cleaned : LostMemoryLine};
            changed = true;
         }

         if (!string.IsNullOrEmpty(profile.BackgroundContext))
         {
            string cleaned = ReasoningTraceStripper.Strip(profile.BackgroundContext);

            if (cleaned != profile.BackgroundContext)
            {
               // Unlike an event, the background paragraph carries no date worth preserving: when the whole
               // paragraph was trace, it simply goes back to "no background context" rather than a filler line.
               profile.BackgroundContext = cleaned.Length > 0 ? cleaned : null;
               changed = true;
            }
         }

         return changed;
      }
   }
}

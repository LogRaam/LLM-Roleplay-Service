// Code written by Gabriel Mailhot, 15/08/2026.

#region

using System;
using System.Collections.Generic;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Parsing
{
   /// <summary>
   ///   A deterministic safety net that drops any [ACTION] whose type is really an [EVENT] TYPE word
   ///   (first_meeting, farewell, conflict, collaboration, agreement, flirt, intimacy, betrayal,
   ///   confrontation, other, captivity). Those label the [EVENT] block ONLY and are never actions; both
   ///   the integrated (direct) and the Prose+Interpreter paths occasionally emit one as an [ACTION]
   ///   (a format lapse the prompt guard cannot guarantee against), and the game would only refuse it as an
   ///   unknown verb. Applied once in <see cref="SectionResponseParser" />, so BOTH composition paths are
   ///   protected equally: the interpret step is not penalised for a lapse the direct model shares. Nothing
   ///   is dropped that a real action verb needs: no <see cref="GameActionCatalog" /> verb shares a name with
   ///   an event type, so the match set (built from <see cref="NotableEventType" />) can never hit one.
   /// </summary>
   public static class ActionTagSanitizer
   {
      // The event-type vocabulary in a separator-free, lower-case form, so an [ACTION] whose type is really an
      // event type is recognised whatever casing or separators it arrived in ("first_meeting", "First-Meeting",
      // "FirstMeeting" all canonicalise to "firstmeeting"). Built from the enum so it stays in sync automatically.
      private static readonly HashSet<string> _eventTypeKeys = BuildEventTypeKeys();

      /// <summary>
      ///   Returns <paramref name="actions" /> with every entry whose type is an [EVENT] type word removed.
      ///   Returns the input unchanged when there is nothing to strip (never allocates on the common path).
      /// </summary>
      public static IReadOnlyList<GameAction> StripEventTypeActions(IReadOnlyList<GameAction> actions)
      {
         if (actions == null || actions.Count == 0) return actions;

         List<GameAction> kept = null;
         for (var i = 0; i < actions.Count; i++)
         {
            GameAction a = actions[i];
            bool drop = a == null || _eventTypeKeys.Contains(Canonical(a.Type));

            if (drop && kept == null)
            {
               kept = new List<GameAction>(actions.Count);
               for (var j = 0; j < i; j++) kept.Add(actions[j]);
            }
            else if (!drop)
            {
               kept?.Add(a);
            }
         }

         return kept ?? actions;
      }

      private static HashSet<string> BuildEventTypeKeys()
      {
         var set = new HashSet<string>();
         foreach (NotableEventType t in (NotableEventType[]) Enum.GetValues(typeof(NotableEventType)))
            set.Add(Canonical(t.ToString()));

         return set;
      }

      private static string Canonical(string s)
         => string.IsNullOrWhiteSpace(s)
            ? string.Empty
            : s.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "").Replace(" ", "");
   }
}

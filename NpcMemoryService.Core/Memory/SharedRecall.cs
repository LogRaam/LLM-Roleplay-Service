// Code written by Gabriel Mailhot, 16/09/2026.
// A recall that is safe to render in front of other people, guaranteed by the type rather than by care.
//
// One hero's memories reach another hero's prompt by exactly two paths: a WITNESS's recall, and — since the
// council began carrying its seats' memories — a COUNCIL seat's. Both are one prompt covering several
// people, so a memory the player asked a character to keep must never travel either one.
//
// The filter for that existed, and it was correct, and it lived in a private method of a chat view model
// with a comment explaining why it mattered. That is a rule kept by care: the next person to need a recall
// in a third place reads NotableEvent.Events, writes two lines, and the confidence is in the room. It very
// nearly happened while wiring the council, which is what prompted this.
//
// So the rule moves to the type. A SharedRecall cannot be constructed from a string; the only way to obtain
// one is From(), which applies the filter. A field typed SharedRecall therefore cannot be handed raw event
// text at all — the leak stops compiling rather than stopping in review.
//
// It lives in the SDK because NotableEvent.IsPrivate lives here: the rule about a flag belongs beside the
// flag, not in whichever caller last needed it.

#region

using System.Collections.Generic;
using System.Linq;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Memory
{
   /// <summary>
   ///   What a character may be shown to REMEMBER in front of others: their own memories, minus anything held
   ///   in confidence, folded to a bounded recall. Obtainable only through <see cref="From" />.
   /// </summary>
   public sealed class SharedRecall
   {
      private SharedRecall(string text) => Text = text;

      /// <summary>The recall itself, already filtered and bounded. Never null or blank on a live instance.</summary>
      public string Text { get; }

      /// <summary>
      ///   A bounded, shareable recall from this character's own events, or NULL when they have nothing they
      ///   could say in company — which covers both a stranger and somebody whose every memory is a confidence.
      ///
      ///   <para>
      ///     THE FILTER. A memory marked <see cref="NotableEvent.IsPrivate" /> is the holder's alone. It still
      ///     reaches their OWN prompt through the ordinary history, where it is rendered with its own
      ///     "do not repeat it in front of others" marker; it never reaches a room.
      ///   </para>
      ///   <para>
      ///     THE BOUND. At most <paramref name="cap" /> summaries, oldest-first. When more remain, the FIRST
      ///     kept summary (the folded compression summary, or the first meeting) is always retained plus the
      ///     most recent, so the past is never silently dropped while a group prompt stays bounded.
      ///   </para>
      /// </summary>
      public static SharedRecall? From(IReadOnlyList<NotableEvent>? events, int cap)
      {
         if (events == null || cap <= 0) return null;

         var clean = new List<string>();

         foreach (NotableEvent ev in events)
         {
            if (ev == null || ev.IsPrivate) continue;
            if (string.IsNullOrWhiteSpace(ev.summary)) continue;

            clean.Add(ev.summary.Trim());
         }

         if (clean.Count == 0) return null;

         IEnumerable<string> chosen = clean.Count <= cap
            ? clean
            : new[] {clean[0]}.Concat(clean.Skip(clean.Count - (cap - 1)));

         string joined = string.Join("; ", chosen);

         return string.IsNullOrWhiteSpace(joined) ? null : new SharedRecall(joined);
      }

      public override string ToString() => Text;
   }
}

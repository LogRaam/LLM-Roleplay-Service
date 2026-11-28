// Code written by Gabriel Mailhot, 28/11/2026.
// Stage 2 of the group-scene plan, "what a meeting leaves" (Calradia Remembers docs/design/group-chat/integration-plan-
// 2026-11-28.md). Gabriel, 28/11/2026: "go pour tout le système ... je testerai demain". In the simulator a second meeting
// replayed with no memory scored 4.2 blind, with the events the game already gives 5.2, and with lasting changes plus the
// bond 6.8 (state-memory-2026-11-28.md): the largest measured gain of the whole simulator work.
//
// What a character carries away from a meeting with the player, beside the events the host already writes (NotableEvent):
// what changed in them that will last, the four-part bond, what stands unsettled, and the secrets this record holds. One
// background call at a chat's close UPDATES it (MeetingMemoryUpdater); it is never rewritten whole. Days are absolute
// campaign days; the prompt counts them at reading ("3 days ago") and never says a day number.
//
// THE BOND IS PROMPT TEXTURE ONLY. It never feeds ReputationWithPlayer, RelationGate, progressive regard, or any gate or
// action: those stay the mechanism (the plan: "NOT a third regard ledger"). Nothing outside the prompt reads it.

#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   What a character carries away from its meetings with the player: lasting changes, the bond, unsettled matters and
   ///   the secrets this record holds. Additive on <see cref="NpcProfile.MeetingMemory" />: null on every save that never
   ///   used it, and then nothing in the prompt changes.
   /// </summary>
   public sealed class MeetingMemory
   {
      /// <summary>Most lasting changes kept; the oldest goes first (long-term consolidation is still open in the design).</summary>
      public const int MaxChanges = 8;

      /// <summary>Most unsettled matters kept (the simulator's consolidated list: six at most).</summary>
      public const int MaxUnsettled = 6;

      /// <summary>Most secrets kept in this record.</summary>
      public const int MaxSecrets = 6;

      /// <summary>Longest text kept for one item, in characters; a longer one is cut at a word.</summary>
      public const int MaxItemChars = 300;

      /// <summary>
      ///   The player this record is about (the name the host gave at the update). A record about another player (after a
      ///   succession, the heir) is not shown, and the next update starts a fresh one.
      /// </summary>
      public string? AboutPlayer { get; set; }

      /// <summary>What meetings changed in the character that will last, dated, oldest first.</summary>
      public List<MeetingChange>? Changes { get; set; } = new();

      /// <summary>The four-part bond toward the player. PROMPT TEXTURE ONLY: never read by a gate, an action or regard.</summary>
      public MeetingBond? Bond { get; set; }

      /// <summary>What stands unsettled between them, consolidated whole at each update, <see cref="MaxUnsettled" /> at most.</summary>
      public List<string>? Unsettled { get; set; } = new();

      /// <summary>Secrets this record holds, each with whom it was told to.</summary>
      public List<MeetingSecret>? Secrets { get; set; } = new();

      /// <summary>The campaign day of the last update.</summary>
      public int LastUpdatedDay { get; set; }

      /// <summary>
      ///   The meeting (the host's memory-anchor id) the last update came from. The same meeting applied again (a replayed
      ///   job, a resumed sitting) replaces what it wrote rather than adding it twice.
      /// </summary>
      public string? LastAnchorId { get; set; }

      /// <summary>Whether the record holds anything a prompt would show.</summary>
      public bool HasContent
         => (Changes?.Count ?? 0) > 0 || Bond?.HasContent == true || (Unsettled?.Count ?? 0) > 0 || (Secrets?.Count ?? 0) > 0;

      /// <summary>The record when it is about this player; null when there is none or it is about someone else.</summary>
      public static MeetingMemory? About(MeetingMemory? record, string playerName)
      {
         if (record == null) return null;
         if (string.IsNullOrWhiteSpace(record.AboutPlayer) || string.IsNullOrWhiteSpace(playerName)) return record;

         return string.Equals(record.AboutPlayer!.Trim(), playerName.Trim(), StringComparison.OrdinalIgnoreCase) ? record : null;
      }

      /// <summary>The secrets not yet told to this person, oldest first (the order the update call numbers them in).</summary>
      public List<string> UntoldTo(string playerName)
         => (Secrets ?? new List<MeetingSecret>()).Where(s => !s.WasToldTo(playerName)).Select(s => s.Text).ToList();

      /// <summary>A deep copy: the chat freezes the record as it stood when a conversation opened.</summary>
      public MeetingMemory Clone() => new() {
         AboutPlayer = AboutPlayer,
         Changes = (Changes ?? new List<MeetingChange>()).Select(c => new MeetingChange {Day = c.Day, Text = c.Text, AnchorId = c.AnchorId}).ToList(),
         Bond = Bond == null ? null : new MeetingBond {Understands = Bond.Understands, Forgives = Bond.Forgives, Loves = Bond.Loves, Trusts = Bond.Trusts},
         Unsettled = (Unsettled ?? new List<string>()).ToList(),
         Secrets = (Secrets ?? new List<MeetingSecret>()).Select(s => new MeetingSecret {
            Text = s.Text, SinceDay = s.SinceDay, ToldOnDay = s.ToldOnDay, ToldTo = (s.ToldTo ?? new List<string>()).ToList(), AnchorId = s.AnchorId
         }).ToList(),
         LastUpdatedDay = LastUpdatedDay,
         LastAnchorId = LastAnchorId
      };
   }

   /// <summary>One lasting change, with the campaign day it came from.</summary>
   public sealed class MeetingChange
   {
      /// <summary>The absolute campaign day of the meeting.</summary>
      public int Day { get; set; }

      /// <summary>What changed, in the character's own words.</summary>
      public string Text { get; set; } = "";

      /// <summary>The meeting it came from (the host's memory-anchor id), so a replay replaces it.</summary>
      public string? AnchorId { get; set; }
   }

   /// <summary>
   ///   Where the character stands with the player, four ways, each "yes", "partly" or "no" (empty when never said).
   ///   PROMPT TEXTURE ONLY: ReputationWithPlayer, RelationGate and progressive regard stay the mechanism.
   /// </summary>
   public sealed class MeetingBond
   {
      /// <summary>Understands the player.</summary>
      public string Understands { get; set; } = "";

      /// <summary>Forgives the player.</summary>
      public string Forgives { get; set; } = "";

      /// <summary>Loves the player.</summary>
      public string Loves { get; set; } = "";

      /// <summary>Trusts the player.</summary>
      public string Trusts { get; set; } = "";

      /// <summary>Whether any of the four was ever said.</summary>
      public bool HasContent
         => !string.IsNullOrEmpty(Understands) || !string.IsNullOrEmpty(Forgives) || !string.IsNullOrEmpty(Loves) || !string.IsNullOrEmpty(Trusts);
   }

   /// <summary>A secret the character keeps, with whom it was told to.</summary>
   public sealed class MeetingSecret
   {
      /// <summary>The secret, in the character's words.</summary>
      public string Text { get; set; } = "";

      /// <summary>The campaign day the character came to keep it.</summary>
      public int SinceDay { get; set; }

      /// <summary>The campaign day it was last told, when it was.</summary>
      public int? ToldOnDay { get; set; }

      /// <summary>Who it was told to (names as the host gave them).</summary>
      public List<string>? ToldTo { get; set; } = new();

      /// <summary>The meeting that gave it (the host's memory-anchor id), so a replay replaces it.</summary>
      public string? AnchorId { get; set; }

      /// <summary>Whether it was told to this person.</summary>
      public bool WasToldTo(string name)
         => !string.IsNullOrWhiteSpace(name) && (ToldTo ?? new List<string>()).Any(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase));
   }
}

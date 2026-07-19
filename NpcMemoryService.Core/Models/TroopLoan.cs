// Code written by Gabriel Mailhot, 18/07/2026.
// Soldiers an NPC has lent the player FOR A TASK, and which go home when that task is over.
//
// Ratified 2026-07-18 after a player-facing gap: Mina offered to send men along with the hideout quest she
// had just given, and nothing happened, because lend_troops was never eligible (it wants relation >= 20 and
// she was a stranger at 0). Scoping the fix turned up the deeper problem: lend_troops MOVES the soldiers one
// way and there is no ledger at all, so what the verb calls a loan has always been a permanent gift.
//
// The design that follows from Gabriel's calls:
//   - A LOAN is bound to a quest. No quest means no way to know when the men go home, so that case is a
//     GIFT instead, and a gift keeps no ledger at all.
//   - Casualties carry NO consequence, even if every man dies. The quest's own success or failure is the
//     consequence; the lender does not also grieve the arithmetic.
//   - The loan closes when the DEED is done (the hideout falls), NOT when the player walks back to collect
//     the reward. In quest terms that is InformalQuest.IsOutstanding going false, which also covers the
//     failure paths (expired, abandoned, cancelled): the men come home either way, so failing a task can
//     never become the way to keep them.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   One outstanding loan of soldiers, held on the LENDER's profile. Records what was handed over so the
   ///   same troop types can be handed back; nothing here judges what happened to them in the field.
   /// </summary>
   public sealed class TroopLoan
   {
      /// <summary>
      ///   The quest this loan serves, as <c>CalradiaRemembersQuest.BuildQuestId</c> composes it
      ///   (giver + type + issue day). Stable across save/load, which the engine's own StringId is not.
      /// </summary>
      public string QuestKey { get; set; } = "";

      /// <summary>What was lent: troop character id to the number handed over.</summary>
      public Dictionary<string, int> Troops { get; set; } = new();

      public int OpenedOnDay { get; set; }

      /// <summary>
      ///   Null while the men are still with the player. Stamped when they go home, which is what keeps the
      ///   return idempotent: a resolution seen twice must not claw back a second batch of soldiers.
      /// </summary>
      public int? ReturnedOnDay { get; set; }

      /// <summary>True while the soldiers are still in the player's ranks.</summary>
      public bool IsOpen => ReturnedOnDay == null;
   }
}

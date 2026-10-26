// Code written by Gabriel Mailhot, 26/10/2026.
// A ruler who heads her house offers the player one road to her hand: give up the rule of their own house. When that road
// is closed for a reason the player could mend, she must say which, or the player says "I will leave my house for you"
// and nothing happens, the very failure MarriageBlockReason was made against (the Ira report, 23/07/2026). The host
// resolves the reason from the same checks that decide whether the road is taught, so the two never disagree.

namespace NpcMemoryService.Core.Models
{
   /// <summary>Why the road to a ruler's hand (leaving one's house for her) is closed right now, when it is.</summary>
   public enum LeaveHouseObstacle
   {
      /// <summary>Nothing the player could mend: the road is open, or the question does not arise.</summary>
      None,

      /// <summary>She does not yet hold the player dear enough to take them as a spouse on any terms.</summary>
      HerHeartIsNotWon,

      /// <summary>Their peoples are at war, or one of them is a captive.</summary>
      WarOrCaptivity,

      /// <summary>One of them marches in an army or stands in a battle; it must wait.</summary>
      ArmyOrBattle
   }
}

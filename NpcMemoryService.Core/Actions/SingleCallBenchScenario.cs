// Code written by Gabriel Mailhot, 19/08/2026.
// One scenario for the single-call bench (increment 3 of the single-call experiment). Unlike ActionBenchCase, this
// does NOT carry a fixed prose: the single-call model AUTHORS the reply itself from the facts and the player's line,
// then self-tags it. So a scenario is only a SETUP (who is who + what the player just said, engineered so a deed is
// the natural outcome) plus an IntendedDeed note for the human reader. It is scored by AGREEMENT - the model's own
// tags vs a dedicated interpreter reading the SAME authored prose - never against a fixed expected action, because a
// faithful reply may legitimately take a different but valid turn.

#region

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>A single-call bench setup: facts + player line the model composes a reply from, then self-tags.</summary>
   public sealed class SingleCallBenchScenario
   {
      /// <summary>Creates a scenario. <paramref name="intendedDeed" /> is a human-reader note, never a scored expectation.</summary>
      public SingleCallBenchScenario(string id, string facts, string playerMessage, string intendedDeed)
      {
         Id = id;
         Facts = facts;
         PlayerMessage = playerMessage;
         IntendedDeed = intendedDeed;
      }

      /// <summary>Stable identifier, used to group passes in the report.</summary>
      public string Id { get; }

      /// <summary>The WHO-IS-WHO digest, fed to BOTH the single-call composer and the interpreter cross-check.</summary>
      public string Facts { get; }

      /// <summary>The player's line the NPC reply reacts to.</summary>
      public string PlayerMessage { get; }

      /// <summary>What a faithful reply is expected to bring about, for the reader; NOT used in scoring.</summary>
      public string IntendedDeed { get; }
   }
}

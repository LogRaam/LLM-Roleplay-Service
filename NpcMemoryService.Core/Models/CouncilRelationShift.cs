// Code written by Gabriel Mailhot, 22/08/2026.
// Ratified 2026-07-24 (see PromptBuilder's own council section): a council seals no deed at the table, but a
// genuine shift in a seated member's regard IS immediate, never deferred to a [RESOLUTION]. In the new one-call
// group scene this is an actor-attributed [ACTION] type: change_relation block, since more than one member may
// speak (and feel) in the same reply, unlike an ordinary two-person exchange where the actor is always the one
// NPC replying.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   A live regard shift a seated member registered this turn (an actor-attributed <c>change_relation</c>
   ///   <c>[ACTION]</c> block from the council group scene). The mod's own RelationGate still caps and
   ///   rate-limits the delta exactly as it does for an ordinary 1:1 turn: the parser only reports what the
   ///   model proposed, never validates it.
   /// </summary>
   public sealed class CouncilRelationShift
   {
      /// <summary>
      ///   The member whose regard moved, exactly as the model wrote the <c>actor:</c> field. Null when the
      ///   block carried no actor at all (a malformed emission): the shift is still reported rather than
      ///   dropped, so the mod can decide whether an un-attributed shift is safe to ignore.
      /// </summary>
      public string? Actor { get; init; }

      /// <summary>The signed regard change proposed. 0 when the block carried no parseable <c>delta:</c> field.</summary>
      public int Delta { get; init; }
   }
}

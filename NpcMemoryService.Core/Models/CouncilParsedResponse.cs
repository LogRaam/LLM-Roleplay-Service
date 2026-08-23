// Code written by Gabriel Mailhot, 22/08/2026.
// The whole result of parsing one council one-call group-scene response (see CouncilResponseParser). Kept as
// its own type rather than folding into ParsedResponse: the group scene has no single [DIALOGUE] speaker at
// all (every seat is a [SPEAKER: Name] block), so the two shapes should never be confused for one another.

using System.Collections.Generic;

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   A council group-scene response decomposed into its structured pieces. Never null even for a blank or
   ///   entirely malformed reply: every list defaults to empty and <see cref="SceneNarration" /> to null, so the
   ///   mod's rendering loop can iterate <see cref="Contributions" /> unconditionally.
   /// </summary>
   public sealed class CouncilParsedResponse
   {
      /// <summary>
      ///   BACK-COMPAT ONLY: the FIRST shared <c>[SCENE]</c> beat's text (the room, the mood), belonging to no
      ///   single speaker. Null when the model emitted none. Now that <c>[SCENE]</c> beats may be interleaved
      ///   anywhere between speakers (not just a single leading one), this property can no longer represent the
      ///   whole scene; <see cref="Contributions" /> (walking its <see cref="CouncilContribution.IsScene" />
      ///   entries in order, alongside the spoken ones) is the AUTHORITATIVE render order.
      /// </summary>
      public string? SceneNarration { get; init; }

      /// <summary>
      ///   Every element of the reply, in the order the model wrote it: both <c>[SPEAKER: Name]</c> blocks
      ///   (<see cref="CouncilContribution.IsScene" /> false) and interleaved <c>[SCENE]</c> narrator beats
      ///   (<see cref="CouncilContribution.IsScene" /> true), so the mod can walk ONE list to reconstruct the
      ///   whole scene, hand-offs included, in true output order. A member who spoke twice (cross-talk) appears
      ///   twice; a member the model left silent this turn simply has no entry here at all, the parser does not
      ///   invent one.
      /// </summary>
      public IReadOnlyList<CouncilContribution> Contributions { get; init; } = new List<CouncilContribution>();

      /// <summary>
      ///   Commitments the table recorded this turn (<c>[RESOLUTION]</c> blocks), zero or more. Reuses
      ///   <see cref="ResolutionProposal" /> unchanged: it is the SAME shape, and the SAME mod-side catalogue
      ///   (<c>ResolutionOfferingResolver</c> / <c>CouncilLift</c>) settles a resolution regardless of which of
      ///   the two council prompt paths produced it.
      /// </summary>
      public IReadOnlyList<ResolutionProposal> Resolutions { get; init; } = new List<ResolutionProposal>();

      /// <summary>
      ///   Live, actor-attributed regard shifts this turn (<c>[ACTION] type: change_relation</c> blocks), zero
      ///   or more since more than one seated member may register a shift in the same reply.
      /// </summary>
      public IReadOnlyList<CouncilRelationShift> RelationShifts { get; init; } = new List<CouncilRelationShift>();
   }
}

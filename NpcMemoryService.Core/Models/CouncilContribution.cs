// Code written by Gabriel Mailhot, 22/08/2026.
// The council's new ONE-CALL GROUP SCENE (Discord/chat style, every seated councillor speaks equally in a
// single response) replaces the old anchor-plus-[WITNESS_REACTION] model, where one seated member spoke the
// real [DIALOGUE] and the rest merely reacted (and often stayed silent). A [SPEAKER: Name] block is this new
// format's own unit of speech: one member's own, full, substantive contribution.
//
// Refinement (22/08/2026, same day): the author wants brief narrator "camera" hand-offs interleaved BETWEEN
// speakers ("Ajin turns to Hophtalamos, who takes up the thread"), not just one leading beat. Rather than a
// second, separate ordered list, a [SCENE] beat is folded into this SAME ordered sequence as an entry with
// IsScene true, so the mod's renderer can walk ONE list, in true output order, to reconstruct the whole scene.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   One element of a parsed council group-scene response, in the order the model wrote it: either a
   ///   <c>[SPEAKER: Name]</c> block (a member's own contribution) or a <c>[SCENE]</c> beat (a brief, unattributed
   ///   narrator transition). A single member may appear more than once (cross-talk: a retort later in the same
   ///   reply), and a <c>[SCENE]</c> beat may appear between any two speaker entries, so this is a flat, ordered
   ///   list rather than one entry per roster seat.
   /// </summary>
   public sealed class CouncilContribution
   {
      /// <summary>
      ///   The speaker's name, resolved against the seated roster the caller passed to
      ///   <see cref="Parsing.CouncilResponseParser.Parse" /> (tolerant match: exact, then first-name, then
      ///   contains). When <see cref="SpeakerMatched" /> is false this is instead the RAW name the model wrote,
      ///   kept rather than dropped so the mod can still render the line under some name. Empty for a
      ///   <see cref="IsScene" /> entry, which belongs to no single speaker.
      /// </summary>
      public required string SpeakerName { get; init; }

      /// <summary>The spoken contribution, or (for a <see cref="IsScene" /> entry) the narrator beat's own text. Trimmed.</summary>
      public required string Text { get; init; }

      /// <summary>
      ///   True when <see cref="SpeakerName" /> was resolved against a real seated member; false when the model
      ///   wrote a name matching no one at the table (a hallucinated or misspelled member), in which case
      ///   <see cref="SpeakerName" /> carries the model's raw text unresolved. The mod decides how to render an
      ///   unmatched block (e.g. fall back to a generic "a voice at the table" portrait) rather than the parser
      ///   silently guessing or dropping it. Always false for a <see cref="IsScene" /> entry.
      /// </summary>
      public bool SpeakerMatched { get; init; }

      /// <summary>
      ///   True when this entry is a shared <c>[SCENE]</c> narrator beat rather than a member's own
      ///   <c>[SPEAKER: Name]</c> block. A scene entry carries an empty <see cref="SpeakerName" /> and a false
      ///   <see cref="SpeakerMatched" />: it belongs to no one, and the mod should render it as narration (no
      ///   portrait/speaker bubble) rather than attempt to attribute it.
      /// </summary>
      public bool IsScene { get; init; }
   }
}

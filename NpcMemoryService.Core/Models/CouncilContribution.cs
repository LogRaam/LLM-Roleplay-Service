// Code written by Gabriel Mailhot, 22/08/2026.
// The council's new ONE-CALL GROUP SCENE (Discord/chat style, every seated councillor speaks equally in a
// single response) replaces the old anchor-plus-[WITNESS_REACTION] model, where one seated member spoke the
// real [DIALOGUE] and the rest merely reacted (and often stayed silent). A [SPEAKER: Name] block is this new
// format's own unit of speech: one member's own, full, substantive contribution.

namespace NpcMemoryService.Core.Models
{
   /// <summary>
   ///   One <c>[SPEAKER: Name]</c> block from a parsed council group-scene response, in the order it was spoken.
   ///   A single member may appear more than once (cross-talk: a retort later in the same reply), so this is a
   ///   flat, ordered list rather than one entry per roster seat.
   /// </summary>
   public sealed class CouncilContribution
   {
      /// <summary>
      ///   The speaker's name, resolved against the seated roster the caller passed to
      ///   <see cref="Parsing.CouncilResponseParser.Parse" /> (tolerant match: exact, then first-name, then
      ///   contains). When <see cref="SpeakerMatched" /> is false this is instead the RAW name the model wrote,
      ///   kept rather than dropped so the mod can still render the line under some name.
      /// </summary>
      public required string SpeakerName { get; init; }

      /// <summary>The member's own words for this block, trimmed.</summary>
      public required string Text { get; init; }

      /// <summary>
      ///   True when <see cref="SpeakerName" /> was resolved against a real seated member; false when the model
      ///   wrote a name matching no one at the table (a hallucinated or misspelled member), in which case
      ///   <see cref="SpeakerName" /> carries the model's raw text unresolved. The mod decides how to render an
      ///   unmatched block (e.g. fall back to a generic "a voice at the table" portrait) rather than the parser
      ///   silently guessing or dropping it.
      /// </summary>
      public bool SpeakerMatched { get; init; }
   }
}

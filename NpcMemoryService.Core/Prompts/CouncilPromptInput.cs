// Code written by Gabriel Mailhot, 22/08/2026.
// Input shape for CouncilPromptBuilder, the council's dedicated ONE-CALL GROUP SCENE prompt: it does not reuse
// EncounterContext (that type carries the whole 1:1/whole-table/narrator machinery the council is being taken
// OFF of) but a small, purpose-built input the mod fills fresh each council turn.

using System.Collections.Generic;

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   One seated councillor for a <see cref="CouncilPromptInput" />. Deliberately lighter than
   ///   <see cref="Models.WitnessEntry" />: a council seat needs its regard and culture stated explicitly (both
   ///   colour how that member argues and votes), which an ordinary passive witness never needs.
   /// </summary>
   public sealed class CouncilMemberInput
   {
      /// <summary>
      ///   The member's display name. This is the EXACT string the model is taught to echo back in its own
      ///   <c>[SPEAKER: Name]</c> and <c>[RESOLUTION] actor:</c> blocks, and the exact string
      ///   <see cref="Parsing.CouncilResponseParser" /> resolves a spoken block against.
      /// </summary>
      public required string Name { get; init; }

      /// <summary>A short persona/role line ("a blunt old marshal, proud of his own troops"). Null renders no persona clause for this seat.</summary>
      public string? PersonaLine { get; init; }

      /// <summary>
      ///   True when the member is a woman, false (the default) for a man. A real bug this closes: with no sex
      ///   stated the model guessed one from the name alone and got it wrong, voicing a male councillor as a
      ///   woman. <see cref="CouncilPromptBuilder" /> renders this explicitly and unmissably on every roster
      ///   line, right beside the name, so the model never has to guess.
      /// </summary>
      public bool IsFemale { get; init; }

      /// <summary>This member's current regard toward the player (the mod's own ledger, not vanilla relation), so their tone at the table is calibrated rather than generic.</summary>
      public int RegardTowardPlayer { get; init; }

      /// <summary>The member's culture (e.g. "Battanian"), when known. Null omits the clause.</summary>
      public string? Culture { get; init; }
   }

   /// <summary>
   ///   Everything <see cref="CouncilPromptBuilder.Build" /> needs to assemble one council group-scene turn.
   ///   Pure data: the mod is responsible for gathering it fresh (the seated roster, what has been said so far
   ///   this sitting, and which <c>[RESOLUTION]</c> kinds the lift could currently honour) each call.
   /// </summary>
   public sealed class CouncilPromptInput
   {
      /// <summary>Every seat at the table this turn. The prompt lists each one and requires each to speak at least once.</summary>
      public IReadOnlyList<CouncilMemberInput> Roster { get; init; } = new List<CouncilMemberInput>();

      /// <summary>The player's own line this turn (what they just said to the table). Empty for the sitting's opening turn, where the table speaks first.</summary>
      public string PlayerLine { get; init; } = string.Empty;

      /// <summary>The player's own name/label, as used elsewhere in the prompt vocabulary. Null falls back to a neutral "the player".</summary>
      public string? PlayerName { get; init; }

      /// <summary>The current campaign day, when known. Null omits the day clause.</summary>
      public int? Day { get; init; }

      /// <summary>The current season, when known (e.g. "spring"). Null omits the clause.</summary>
      public string? Season { get; init; }

      /// <summary>Where the council sits (e.g. "the great hall of Pravend"). Null omits the clause.</summary>
      public string? Place { get; init; }

      /// <summary>
      ///   The conversation so far THIS sitting, Discord-style ("Name: their words") lines, oldest first. Empty
      ///   for the opening turn. Kept as the caller's own pre-rendered lines (like every other builder's
      ///   "conversation so far" tail) rather than a richer structure the SDK would have to re-render.
      /// </summary>
      public IReadOnlyList<string> TranscriptSoFar { get; init; } = new List<string>();

      /// <summary>
      ///   The <c>[RESOLUTION] type:</c> values the mod's own <c>ResolutionOfferingResolver</c> has proven the
      ///   lift could honour right now, beyond the always-available <c>quest</c> kind. Empty means only
      ///   <c>quest</c> is on offer this turn.
      /// </summary>
      public IReadOnlyList<string> OfferedResolutionKinds { get; init; } = new List<string>();
   }
}

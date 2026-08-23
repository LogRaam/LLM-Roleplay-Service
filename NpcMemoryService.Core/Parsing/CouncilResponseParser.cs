// Code written by Gabriel Mailhot, 22/08/2026.
// Parses the council's new ONE-CALL GROUP SCENE response (see CouncilPromptBuilder for the taught format):
// an optional shared [SCENE], then one or more [SPEAKER: Name] blocks (every seated member, possibly more than
// once for cross-talk), then zero or more [RESOLUTION] and actor-attributed [ACTION] type: change_relation
// blocks. Mirrors SectionResponseParser's own conventions closely (truncation tolerance on every block family,
// tolerant key/value parsing) and REUSES its [RESOLUTION] field-mapping and small tolerant helpers verbatim
// (widened to internal there) rather than a second, divergent copy: a resolution recorded through either
// council prompt path lands on the exact same NpcMemoryService.Core.Models.ResolutionProposal shape the mod's
// CouncilLift already settles.

#region

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Parsing
{
   /// <summary>
   ///   Regex-based, tolerant parser for the council group-scene output format. Never throws: a blank or
   ///   entirely malformed reply yields an empty-but-non-null <see cref="CouncilParsedResponse" />.
   /// </summary>
   public sealed class CouncilResponseParser
   {
      /// <summary>
      ///   Matches the START of any recognised block boundary EXCEPT [SCENE]'s own close tag: the point where an
      ///   open (unclosed) [SCENE]/[SPEAKER: Name]/[RESOLUTION]/[ACTION] block's body must stop. [SPEAKER: Name]
      ///   is matched on its opening "[SPEAKER:" only (its name/close bracket varies); the other three are exact
      ///   bracket tags in this format.
      /// </summary>
      private const string BoundaryPattern = @"\[(?:SPEAKER\s*:|SCENE\]|RESOLUTION\]|ACTION\])";

      /// <summary>
      ///   Parses <paramref name="raw" /> against the <paramref name="seatedNames" /> roster for this turn (used
      ///   to resolve each [SPEAKER: Name] block, tolerantly, to a real seat). A null/blank <paramref name="raw" />
      ///   or a null <paramref name="seatedNames" /> both degrade cleanly rather than throwing.
      /// </summary>
      public CouncilParsedResponse Parse(string raw, IReadOnlyList<string> seatedNames)
      {
         if (string.IsNullOrWhiteSpace(raw)) return new CouncilParsedResponse();

         IReadOnlyList<string> seats = seatedNames ?? new List<string>();

         return new CouncilParsedResponse {
            SceneNarration = ExtractScene(raw),
            Contributions = ParseContributions(raw, seats),
            Resolutions = ParseResolutions(raw),
            RelationShifts = ParseRelationShifts(raw)
         };
      }

      #region private

      /// <summary>Cuts <paramref name="text" /> at the first recognised block boundary, or returns it whole when none follows.</summary>
      private static string TrimAtFirstBoundary(string text)
      {
         Match m = Regex.Match(text, BoundaryPattern, RegexOptions.IgnoreCase);

         return m.Success ? text.Substring(0, m.Index) : text;
      }

      /// <summary>
      ///   The shared [SCENE] narration. A properly closed block is preferred; an OPEN block with no close (a
      ///   max-tokens truncation, or simply the model omitting the close tag) is still recovered up to the next
      ///   boundary, mirroring every other block family's own truncation tolerance. Null (never empty) when
      ///   absent or blank, so the mod can treat "no shared narration" as a single clean case.
      /// </summary>
      private static string? ExtractScene(string text)
      {
         Match closed = Regex.Match(text, @"\[SCENE\](.*?)\[/SCENE\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
         if (closed.Success) return NullIfBlank(closed.Groups[1].Value);

         Match open = Regex.Match(text, @"\[SCENE\]", RegexOptions.IgnoreCase);
         if (!open.Success) return null;

         return NullIfBlank(TrimAtFirstBoundary(text.Substring(open.Index + open.Length)));
      }

      /// <summary>
      ///   Extracts every [SPEAKER: Name] block, in output order. A block's body runs up to the NEXT recognised
      ///   boundary (there is no [/SPEAKER] close tag in this format), so a model that forgets one block never
      ///   swallows the next: the following [SPEAKER:.../[SCENE]/[RESOLUTION]/[ACTION] tag ends it regardless.
      ///   A block with a blank name or blank body (a stray "[SPEAKER: ]" the model typed and abandoned) is
      ///   skipped rather than recorded as an empty contribution.
      /// </summary>
      private static IReadOnlyList<CouncilContribution> ParseContributions(string text, IReadOnlyList<string> seatedNames)
      {
         var contributions = new List<CouncilContribution>();
         const string pattern =
            @"\[SPEAKER\s*:\s*(?<name>[^\]]+)\](?<body>.*?)(?=\[SPEAKER\s*:|\[SCENE\]|\[RESOLUTION\]|\[ACTION\]|\z)";

         foreach (Match match in Regex.Matches(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
         {
            string rawName = match.Groups["name"].Value.Trim();
            string body = match.Groups["body"].Value.Trim();
            if (rawName.Length == 0 || body.Length == 0) continue;

            (string resolvedName, bool matched) = ResolveSpeaker(rawName, seatedNames);
            contributions.Add(new CouncilContribution {SpeakerName = resolvedName, Text = body, SpeakerMatched = matched});
         }

         return contributions;
      }

      /// <summary>
      ///   Resolves a spoken [SPEAKER: Name] against the seated roster, tolerantly: (1) an exact, trimmed,
      ///   case-insensitive match; else (2) the seat's OWN first name/word, case-insensitively (a model that
      ///   wrote "Ajin" for a seat listed as "Ajin the Hawk"); else (3) a substring match either direction (a
      ///   model that wrote a title/epithet along with the name, or only part of it). A name matching no seat at
      ///   all is returned UNRESOLVED (the raw, trimmed text) rather than dropped or guessed onto some seat, and
      ///   flagged via the returned <c>Matched</c> flag so the mod can render it distinctly.
      /// </summary>
      private static (string Name, bool Matched) ResolveSpeaker(string rawName, IReadOnlyList<string> seatedNames)
      {
         foreach (string seat in seatedNames)
         {
            if (string.IsNullOrWhiteSpace(seat)) continue;
            if (string.Equals(seat.Trim(), rawName, StringComparison.OrdinalIgnoreCase))
               return (seat.Trim(), true);
         }

         foreach (string seat in seatedNames)
         {
            if (string.IsNullOrWhiteSpace(seat)) continue;
            string trimmedSeat = seat.Trim();
            string firstWord = trimmedSeat.Split(' ')[0];
            if (string.Equals(firstWord, rawName, StringComparison.OrdinalIgnoreCase))
               return (trimmedSeat, true);
         }

         foreach (string seat in seatedNames)
         {
            if (string.IsNullOrWhiteSpace(seat)) continue;
            string trimmedSeat = seat.Trim();
            if (trimmedSeat.IndexOf(rawName, StringComparison.OrdinalIgnoreCase) >= 0
                || rawName.IndexOf(trimmedSeat, StringComparison.OrdinalIgnoreCase) >= 0)
               return (trimmedSeat, true);
         }

         return (rawName, false);
      }

      /// <summary>
      ///   Extracts every [RESOLUTION] block, mirroring <see cref="SectionResponseParser" />'s own conventions: a
      ///   trailing block cut off before its close tag (a max-tokens truncation) is still recovered. Field
      ///   mapping is REUSED verbatim from <see cref="SectionResponseParser.BuildResolution" /> (widened to
      ///   internal for exactly this reuse), so both council prompt paths settle onto the identical shape.
      /// </summary>
      private static IReadOnlyList<ResolutionProposal> ParseResolutions(string text)
      {
         var resolutions = new List<ResolutionProposal>();
         const string pattern = @"\[RESOLUTION\](.*?)\[/RESOLUTION\]";

         foreach (Match match in Regex.Matches(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
         {
            ResolutionProposal? resolution =
               SectionResponseParser.BuildResolution(SectionResponseParser.ParseKeyValueLines(match.Groups[1].Value));
            if (resolution != null) resolutions.Add(resolution);
         }

         MatchCollection opens = Regex.Matches(text, @"\[RESOLUTION\]", RegexOptions.IgnoreCase);
         if (opens.Count > 0)
         {
            Match lastOpen = opens[opens.Count - 1];
            string afterOpen = text.Substring(lastOpen.Index + lastOpen.Length);

            if (!Regex.IsMatch(afterOpen, @"\[/RESOLUTION\]", RegexOptions.IgnoreCase))
            {
               ResolutionProposal? truncated =
                  SectionResponseParser.BuildResolution(SectionResponseParser.ParseKeyValueLines(TrimAtFirstBoundary(afterOpen)));
               if (truncated != null) resolutions.Add(truncated);
            }
         }

         return resolutions;
      }

      /// <summary>
      ///   Extracts every actor-attributed <c>change_relation</c> [ACTION] block, the council's own "your regard
      ///   is real" channel. Any OTHER [ACTION] type is out of format for a council group scene (the prompt
      ///   teaches only this one) and is deliberately ignored here rather than guessed into some other shift.
      /// </summary>
      private static IReadOnlyList<CouncilRelationShift> ParseRelationShifts(string text)
      {
         var shifts = new List<CouncilRelationShift>();
         const string pattern = @"\[ACTION\](.*?)\[/ACTION\]";

         foreach (Match match in Regex.Matches(text, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
         {
            CouncilRelationShift? shift = BuildRelationShift(match.Groups[1].Value);
            if (shift != null) shifts.Add(shift);
         }

         MatchCollection opens = Regex.Matches(text, @"\[ACTION\]", RegexOptions.IgnoreCase);
         if (opens.Count > 0)
         {
            Match lastOpen = opens[opens.Count - 1];
            string afterOpen = text.Substring(lastOpen.Index + lastOpen.Length);

            if (!Regex.IsMatch(afterOpen, @"\[/ACTION\]", RegexOptions.IgnoreCase))
            {
               CouncilRelationShift? truncated = BuildRelationShift(TrimAtFirstBoundary(afterOpen));
               if (truncated != null) shifts.Add(truncated);
            }
         }

         return shifts;
      }

      /// <summary>A block missing "type", or whose type is not change_relation, is not a regard shift at all and is skipped.</summary>
      private static CouncilRelationShift? BuildRelationShift(string body)
      {
         Dictionary<string, string> fields = SectionResponseParser.ParseKeyValueLines(body);

         if (!fields.TryGetValue("type", out string? type) || string.IsNullOrWhiteSpace(type)) return null;
         if (!string.Equals(SectionResponseParser.NormalizeActionType(type), "change_relation", StringComparison.OrdinalIgnoreCase))
            return null;

         fields.TryGetValue("actor", out string? actor);

         return new CouncilRelationShift {
            Actor = SectionResponseParser.NullIfBlank(actor),
            Delta = SectionResponseParser.TryParseSignedInt(fields, "delta") ?? 0
         };
      }

      private static string? NullIfBlank(string value)
         => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

      #endregion
   }
}

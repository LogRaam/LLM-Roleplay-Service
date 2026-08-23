// Code written by Gabriel Mailhot, 22/08/2026.
// Parses the council's new ONE-CALL GROUP SCENE response (see CouncilPromptBuilder for the taught format):
// [SCENE] beats freely interleaved with [SPEAKER: Name] blocks (every seated member, possibly more than once
// for cross-talk), then zero or more [RESOLUTION] and actor-attributed [ACTION] type: change_relation blocks.
// Mirrors SectionResponseParser's own conventions closely (truncation tolerance on every block family, tolerant
// key/value parsing) and REUSES its [RESOLUTION] field-mapping and small tolerant helpers verbatim (widened to
// internal there) rather than a second, divergent copy: a resolution recorded through either council prompt
// path lands on the exact same NpcMemoryService.Core.Models.ResolutionProposal shape the mod's CouncilLift
// already settles.
//
// Refinement (22/08/2026, same day): the author wants brief narrator "camera" hand-offs interleaved BETWEEN
// speakers, not just one leading beat. [SCENE] and [SPEAKER: Name] are now parsed in a SINGLE pass into one
// ordered Contributions list (CouncilContribution.IsScene distinguishes the two), so the mod's renderer walks
// one list, in true output order, instead of reassembling narration and speech from two separate results.

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
      ///   Matches the START of any recognised block: the point where an open (unclosed) [SCENE]/[SPEAKER:
      ///   Name]/[RESOLUTION]/[ACTION] block's body must stop, since none of the four has a close tag a model is
      ///   taught to write. [SPEAKER: Name] is matched on its opening "[SPEAKER:" only (its name/close bracket
      ///   varies); the other three are exact bracket tags in this format. Shared by <see cref="TrimAtFirstBoundary" />
      ///   (RESOLUTION/ACTION truncation recovery) and <see cref="ContributionPattern" /> (the SCENE/SPEAKER scan).
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
         IReadOnlyList<CouncilContribution> contributions = ParseContributions(raw, seats);

         return new CouncilParsedResponse {
            SceneNarration = FirstSceneText(contributions),
            Contributions = contributions,
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
      ///   Matches EITHER a [SCENE] beat or a [SPEAKER: Name] block, in one single pass, so a combined scan of
      ///   the reply visits both kinds in TRUE output order (a second, separate scan for each kind would lose
      ///   their relative interleaving). Either alternative's body runs up to the NEXT recognised boundary: there
      ///   is no close tag for either in this format (a model may still write a stray "[/SCENE]"/"[/SPEAKER]",
      ///   cleaned out of the captured body by <see cref="CleanBody" /> rather than left to leak into the text).
      /// </summary>
      private static readonly string ContributionPattern =
         @"\[(?<scenetag>SCENE)\](?<scenebody>.*?)(?=" + BoundaryPattern + @"|\z)"
         + @"|\[SPEAKER\s*:\s*(?<name>[^\]]+)\](?<speakerbody>.*?)(?=" + BoundaryPattern + @"|\z)";

      /// <summary>The first [SCENE] beat's own text, for the back-compat <see cref="CouncilParsedResponse.SceneNarration" /> property. Null when there is none.</summary>
      private static string? FirstSceneText(IReadOnlyList<CouncilContribution> contributions)
      {
         foreach (CouncilContribution contribution in contributions)
            if (contribution.IsScene) return contribution.Text;

         return null;
      }

      /// <summary>
      ///   Extracts every [SCENE] beat AND every [SPEAKER: Name] block, interleaved, in the single true output
      ///   order the model wrote them. A blank name (for a speaker block) or a blank body (either kind, a stray
      ///   tag the model typed and abandoned) is skipped rather than recorded as an empty entry.
      /// </summary>
      private static IReadOnlyList<CouncilContribution> ParseContributions(string text, IReadOnlyList<string> seatedNames)
      {
         var contributions = new List<CouncilContribution>();

         foreach (Match match in Regex.Matches(text, ContributionPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
         {
            if (match.Groups["scenetag"].Success)
            {
               string sceneBody = CleanBody(match.Groups["scenebody"].Value);
               if (sceneBody.Length == 0) continue;

               contributions.Add(new CouncilContribution {SpeakerName = string.Empty, Text = sceneBody, SpeakerMatched = false, IsScene = true});
               continue;
            }

            string rawName = match.Groups["name"].Value.Trim();
            string body = CleanBody(match.Groups["speakerbody"].Value);
            if (rawName.Length == 0 || body.Length == 0) continue;

            (string resolvedName, bool matched) = ResolveSpeaker(rawName, seatedNames);
            contributions.Add(new CouncilContribution {SpeakerName = resolvedName, Text = body, SpeakerMatched = matched, IsScene = false});
         }

         return contributions;
      }

      /// <summary>
      ///   Trims a captured body and strips any stray close tag the model wrote for a format that has none
      ///   ("[/SCENE]", "[/SPEAKER]") so it never leaks into the rendered text, mirroring
      ///   <see cref="SectionResponseParser" />'s own "the player never reads our plumbing" discipline.
      /// </summary>
      private static string CleanBody(string raw)
         => Regex.Replace(raw, @"\[/(?:SCENE|SPEAKER|ACTION|RESOLUTION)\]", "", RegexOptions.IgnoreCase).Trim();

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

      #endregion
   }
}

// Code written by Gabriel Mailhot, 13/08/2026.

#region

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   Projects a live <see cref="EncounterContext" /> (plus the speaking NPC's <see cref="NpcProfile" />) into the
   ///   compact SETTING / PLAYER / YOU digest the PROSE+EXTRACT extractor receives as its VARIABLE per-turn tail,
   ///   after <see cref="ExtractionPromptBuilder.StablePrefix" />. The digest GROUNDS the memories the extractor
   ///   writes (where it happened, who witnessed it, the player's standing) and CALIBRATES them to the NPC's own
   ///   nature and regard, so a recorded event is anchored in the real scene instead of invented. This replaces the
   ///   hardcoded context-facts string the prose/extract spike hand-built.
   ///   <para>
   ///     Pure and defensive: it reads ONLY fields that exist on the two inputs, OMITS a clause whenever its source
   ///     is null/empty (never printing the word "null"), never throws on a partial (or null) context, and refers to
   ///     the player by a real label, never an epithet. It deliberately carries NO timing/recency: the host re-adds
   ///     recency when a stored memory is reinjected, so baking "days ago" here would double it and stale it.
   ///   </para>
   /// </summary>
   public static class ExtractionContextBuilder
   {
      /// <summary>
      ///   The neutral, non-epithet fallback used for the player when the caller supplies no real label. "the
      ///   player" is a plain reference, not an epithet: it never bakes a one-time insult ("the coward") into a
      ///   permanent memory the way the spike's first extractor did.
      /// </summary>
      private const string DefaultPlayerLabel = "the player";

      /// <summary>
      ///   Projects <paramref name="context" /> and <paramref name="npc" /> into the three-line digest, referring to
      ///   the player by the neutral <see cref="DefaultPlayerLabel" />. For callers (the SDK's own tests, any
      ///   consumer without the live player name) that cannot supply the real, prompt-safe label.
      /// </summary>
      public static string Project(EncounterContext? context, NpcProfile? npc)
         => Project(context, npc, null);

      /// <summary>
      ///   Projects <paramref name="context" /> and <paramref name="npc" /> into the three-line SETTING / PLAYER / YOU
      ///   digest, referring to the player by <paramref name="playerLabel" /> (the SAME prompt-safe label the rest of
      ///   the system prompt uses, e.g. the live hero name the mod feeds to <see cref="PromptBuilder.PlayerName" />).
      ///   A null/blank label degrades to the neutral <see cref="DefaultPlayerLabel" />, never an epithet.
      /// </summary>
      public static string Project(EncounterContext? context, NpcProfile? npc, string? playerLabel)
      {
         string player = string.IsNullOrWhiteSpace(playerLabel) ? DefaultPlayerLabel : playerLabel!.Trim();

         var sb = new StringBuilder();
         sb.AppendLine("SETTING: " + BuildSetting(context));
         sb.AppendLine("PLAYER: " + BuildPlayer(context, player));
         sb.Append("YOU: " + BuildYou(npc, player));

         return sb.ToString();
      }

      #region private

      /// <summary>The place (or the ship at sea) plus who, if anyone, is within earshot. Always non-empty (defaults to "alone").</summary>
      private static string BuildSetting(EncounterContext? context)
      {
         var clauses = new List<string>();

         // At sea replaces the land place entirely: a captor-camp-on-land would contradict a ship on open water.
         string? place = context != null && context.AtSea ? "aboard a ship at sea" : DescribePlace(context);
         if (!string.IsNullOrEmpty(place)) clauses.Add(place!);

         clauses.Add(DescribeWitnesses(context));

         return string.Join("; ", clauses) + ".";
      }

      /// <summary>The named settlement/scene when known, else a scene-type descriptor, else null (place unknown, clause omitted).</summary>
      private static string? DescribePlace(EncounterContext? context)
      {
         if (context == null) return null;

         if (!string.IsNullOrWhiteSpace(context.CurrentLocationName))
            return "in " + context.CurrentLocationName!.Trim();

         switch (context.Scene)
         {
            case SceneType.Outdoor: return "on the road, in open country";
            case SceneType.Settlement: return "within a settlement";
            case SceneType.Keep: return "within a keep";
            case SceneType.Tavern: return "in a tavern";
            case SceneType.Dungeon: return "in a dungeon";
            case SceneType.Battlefield: return "on a battlefield, the fighting just done";
            default: return null;
         }
      }

      /// <summary>The witnesses by name, or the explicit "alone" default so a memory is never wrongly grounded as public.</summary>
      private static string DescribeWitnesses(EncounterContext? context)
      {
         List<string>? names = context?.Witnesses?
                                      .Where(w => w != null && !string.IsNullOrWhiteSpace(w.Name))
                                      .Select(w => w.Name.Trim())
                                      .ToList();

         if (names == null || names.Count == 0)
            return "the two of you are alone, no witnesses";

         return "within earshot: " + string.Join(", ", names);
      }

      /// <summary>The player's label, then their standing (status / presence / war) folded compactly onto one line.</summary>
      private static string BuildPlayer(EncounterContext? context, string player)
      {
         var clauses = new List<string>();

         string? standing = DescribePlayerStanding(context);
         if (!string.IsNullOrEmpty(standing)) clauses.Add(standing!);

         string? presence = TrimNote(context?.PresenceNote);
         if (!string.IsNullOrEmpty(presence)) clauses.Add(presence!);

         string? war = DescribeWar(context);
         if (!string.IsNullOrEmpty(war)) clauses.Add(war!);

         return clauses.Count == 0 ? player : player + ", " + string.Join("; ", clauses);
      }

      /// <summary>A short standing phrase from the player's status toward this NPC; null for the ordinary free/unknown case.</summary>
      private static string? DescribePlayerStanding(EncounterContext? context)
      {
         if (context == null) return null;

         switch (context.PlayerStatus)
         {
            case PlayerStatusVsNpc.Captive: return "your prisoner";
            case PlayerStatusVsNpc.NpcIsCaptive: return "your captor, holding you prisoner";
            case PlayerStatusVsNpc.ClanMember: return "of your own clan";
            case PlayerStatusVsNpc.Vassal: return "your sworn vassal";
            case PlayerStatusVsNpc.Liege: return "your liege lord";
            case PlayerStatusVsNpc.Mercenary: return "a mercenary in your realm's pay";
            case PlayerStatusVsNpc.Employer: return "your paymaster, employing your clan";
            case PlayerStatusVsNpc.SameRealm: return "a fellow vassal of your realm";
            case PlayerStatusVsNpc.Stranger: return "a stranger you have not met before";
            default: return null; // Free / Unknown: nothing worth stating.
         }
      }

      /// <summary>The war/alliance state, only for the states worth a token; peace and unknown are omitted to stay lean.</summary>
      private static string? DescribeWar(EncounterContext? context)
      {
         if (context == null) return null;

         switch (context.WarStatus)
         {
            case DiplomaticStatus.AtWar: return "your faction and theirs are at war";
            case DiplomaticStatus.Allied: return "your factions are allied";
            default: return null; // AtPeace / Unknown.
         }
      }

      /// <summary>The NPC's own label and nature, then their current regard toward the player as a signed number.</summary>
      private static string BuildYou(NpcProfile? npc, string player)
      {
         var sb = new StringBuilder();
         sb.Append(DescribeNpcLabel(npc));

         string? nature = DescribeNpcNature(npc);
         if (!string.IsNullOrEmpty(nature)) sb.Append(", " + nature);

         int regard = npc?.ReputationWithPlayer ?? 0;
         sb.Append(". Your current regard toward " + player + ": " + FormatRegard(regard) + ".");

         return sb.ToString();
      }

      /// <summary>"Name of Clan" when a house is known, else the bare name, else a neutral placeholder (never null).</summary>
      private static string DescribeNpcLabel(NpcProfile? npc)
      {
         if (npc == null || string.IsNullOrWhiteSpace(npc.Name)) return "the NPC";

         string name = npc.Name.Trim();

         return string.IsNullOrWhiteSpace(npc.Clan) ? name : name + " of " + npc.Clan.Trim();
      }

      /// <summary>
      ///   The NPC's nature descriptor to calibrate the extractor's tone: the derived <see cref="NpcProfile.Trait" />
      ///   archetype if present, else the free-text <see cref="NpcProfile.Personality" />. The SDK profile has no raw
      ///   five-trait levels (those live engine-side in the mod), so this projects only what genuinely exists here.
      /// </summary>
      private static string? DescribeNpcNature(NpcProfile? npc)
      {
         if (npc == null) return null;

         return !string.IsNullOrWhiteSpace(npc.Trait) ? npc.Trait!.Trim() : TrimNote(npc.Personality);
      }

      /// <summary>Formats regard with an explicit sign, e.g. +12, -5, +0, in an invariant, locale-proof form.</summary>
      private static string FormatRegard(int regard)
         => (regard >= 0 ? "+" : string.Empty) + regard.ToString(CultureInfo.InvariantCulture);

      /// <summary>Trims a host-composed note and strips a trailing period so it folds cleanly into our own punctuation; null when blank.</summary>
      private static string? TrimNote(string? note)
      {
         if (string.IsNullOrWhiteSpace(note)) return null;

         return note!.Trim().TrimEnd('.').Trim();
      }

      #endregion
   }
}

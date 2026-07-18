// Code written by Gabriel Mailhot, 04/07/2026.
// P0.1 of the affect-coherence reform: ONE canonical band table for NpcProfile.ReputationWithPlayer,
// replacing three vocabularies that used to disagree at the same numeric value (the encyclopedia's own
// 14-band table, PromptBuilder.DescribePersonalRegard's 5-band table, and StanceDescriptor.AffectionPhrase's
// Mild/Strong thresholds) — a hero at -15 used to read "wary" on the encyclopedia page but "neutral" in
// the prompt. The encyclopedia's words are canon (already player-visible, first written 01/06/2026); this
// only centralizes them so a given value reads the same everywhere that renders it.

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   The single canonical banding of <see cref="Models.NpcProfile.ReputationWithPlayer" /> into the 14
   ///   named tiers first established by the encyclopedia's "Personal regard" row
   ///   (<c>EncyclopediaHeroPagePersonalRelationMixin.Describe</c>). Every site that turns this number
   ///   into words must delegate to <see cref="Describe" /> so no two places can ever disagree again.
   /// </summary>
   public static class RegardBands
   {
      /// <summary>The canonical word/phrase for a personal-regard value, exactly as shown on the encyclopedia page.</summary>
      public static string Describe(int value) => value switch {
         // === RELATIONS LÉGENDAIRES ===
         >= 95 => "oathbound devotion",
         >= 85 => "unshakable devotion",
         >= 70 => "most cherished ally",

         // === RELATIONS SOUDÉES ===
         >= 50 => "profound admiration",
         // 5.5 (regard audit): "deep affection", not "deep trust". This is the AFFECT band; naming it "trust"
         // collided head-on with the separate StanceTrust axis, so an NPC at high regard but low stance-trust
         // printed "deep trust" beside "you deeply distrust the player" in the same prompt.
         >= 30 => "deep affection",

         // === BONNES RELATIONS ===
         >= 15 => "genuine regard",
         >= 5 => "cordial regard",

         // === PIVOT ===
         >= -5 => "neutral",

         // === MÉFIANCE ===
         >= -20 => "wary",
         >= -35 => "quiet distrust",

         // === ANTIPATHIE MARQUÉE ===
         >= -55 => "ill-disposed",
         >= -70 => "bitter grudge",

         // === HOSTILITÉ DÉCLARÉE ===
         >= -85 => "open enmity",
         >= -95 => "mortal foe",

         // === HAINE ABSOLUE ===
         _ => "implacable hatred"
      };
   }
}

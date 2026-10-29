// Code written by Gabriel Mailhot, 28/10/2026.
// Guards that every character is told where the player stands beside them, in rank and at arms, in the part of the
// prompt that is never budgeted away. Gabriel (28/10/2026): "Un paysan qui parle au joueur niveau 1 sera plus rude que si
// il parle au joueur niveau 20 Clan tier 3". avalanche24 (Nexus, 28/10/2026): a weak companion boasted before a master of
// arms. If this regresses, the line is dropped in the compact prompt or never reaches a character without a rank.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class BesideThePlayerPromptTests
   {
      private const string Line = "In the eyes of the world the player is a nobody: no house of note, no lands, no name yet.";

      private static string Prompt(EncounterContext context)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
           .BuildSystemPrompt(new NpcProfile {Id = "v", Name = "Osric", Faction = "Vlandia", Clan = "", Age = 40}, new WorldState {CurrentDay = 30}, context);

      // STAKE: a villager with no rank of his own still hears where the player stands, in Lean and Full alike.
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_a_character_of_no_rank_WHEN_building_the_prompt_THEN_he_hears_where_the_player_stands(LeanPromptLevel lean)
      {
         Prompt(new EncounterContext {LeanLevel = lean, BesideThePlayer = Line}).Should().Contain(Line);
      }

      // STAKE: a ruler, whose rank line used to end the station block, still hears it.
      [Test]
      public void GIVEN_a_ruler_WHEN_building_the_prompt_THEN_he_hears_where_the_player_stands_after_his_crown()
      {
         string prompt = Prompt(new EncounterContext {RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King", BesideThePlayer = Line});

         prompt.Should().Contain("YOU RULE VLANDIA").And.Contain(Line);
         prompt.IndexOf(Line, System.StringComparison.Ordinal).Should().BeGreaterThan(prompt.IndexOf("YOU RULE VLANDIA", System.StringComparison.Ordinal));
      }
   }
}

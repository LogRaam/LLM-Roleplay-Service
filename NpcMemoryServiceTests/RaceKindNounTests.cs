// Code written by Gabriel Mailhot, 26/11/2026.
// The identity line names a character's people when the host gives a race noun. CatWithFourEyes (Discord, 26/11/2026):
// overhaul mods with non-human races (LOTR, GoT, Forgotten Realms) saw every character called a man or a woman; Gabriel
// (26/11/2026) chose a race file on the mod side, which hands its noun over as EncounterContext.NpcKindNoun. If this is
// wrong, an elf reads "an elf woman" in its appearance and "a woman" in the line that says who it is.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class RaceKindNounTests
   {
      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(new NpcProfile {Id = "npc_test", Name = "Test Lady", Faction = "Vlandia", Clan = "dey Meroc", IsFemale = true, Age = 30}, new WorldState {CurrentDay = 10}, context);

      // STAKE: the elf the appearance names must be an elf in the line that says who she is.
      [Test]
      public void GIVEN_a_race_noun_WHEN_building_THEN_the_identity_line_names_the_people_with_its_article()
      {
         Build(new EncounterContext {NpcKindNoun = "elf woman"}).Should().Contain("YOU ARE TEST LADY, an elf woman of 30 years");
      }

      // STAKE: a stock game hands no race noun; the line must read exactly as before.
      [Test]
      public void GIVEN_no_race_noun_WHEN_building_THEN_the_identity_line_says_woman_as_before()
      {
         Build(new EncounterContext()).Should().Contain("YOU ARE TEST LADY, a woman of 30 years");
      }
   }
}

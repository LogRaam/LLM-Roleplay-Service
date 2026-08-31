// Code written by Gabriel Mailhot, 31/08/2026.
// Player report: a lord asked how many men rode under his command could not answer, only the
// PLAYER's troop count ever reached the prompt (PlayerPartyTroopCount), never the NPC's own. The
// NPC's count now flows as NpcPartyTroopCount and renders as a coarse qualitative band in the NPC's
// own voice (never the exact metagame figure), so a lord can speak of his own strength. These tests
// pin the bands, the section's presence/absence, the solo case, and that no raw number leaks.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class NpcForcePromptTests
   {
      private const string SectionHeading = "YOUR OWN COMMAND";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(int troopCount)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {NpcPartyTroopCount = troopCount});

      // The qualitative bands, pinned at representative values AND at their exact boundaries: an
      // off-by-one in a comparison would silently move a real game state into the wrong description.
      [TestCase(2, "only a handful of men")]
      [TestCase(24, "only a handful of men")]
      [TestCase(25, "a company of a few dozen soldiers")]
      [TestCase(99, "a company of a few dozen soldiers")]
      [TestCase(100, "a warband of well over a hundred soldiers")]
      [TestCase(399, "a warband of well over a hundred soldiers")]
      [TestCase(400, "several hundred soldiers")]
      [TestCase(999, "several hundred soldiers")]
      [TestCase(1000, "an army numbering in the thousands")]
      [TestCase(5000, "an army numbering in the thousands")]
      public void GIVEN_an_npc_troop_count_WHEN_the_prompt_is_built_THEN_the_matching_force_band_is_rendered(int count, string band)
      {
         string prompt = Build(count);

         prompt.Should().Contain(SectionHeading);
         prompt.Should().Contain(band);
      }

      // 0 = not provided (the NPC is a captive, or leads no field party, a governor keeping to his
      // settlement): the section must stay silent rather than claim a strength that is not there.
      [Test]
      public void GIVEN_no_npc_troop_count_WHEN_the_prompt_is_built_THEN_the_command_section_is_absent()
      {
         string prompt = Build(0);

         prompt.Should().NotContain(SectionHeading);
         prompt.Should().NotContain("You command");
      }

      // The solo case: the roster counts the NPC themselves, so 1 means riding ALONE, saying "a
      // handful of men" would invent an escort that is not there (the same class of lie the player
      // force fix shipped with, see PlayerForcePromptTests).
      [Test]
      public void GIVEN_an_npc_party_of_one_WHEN_the_prompt_is_built_THEN_the_npc_is_not_given_men()
      {
         string prompt = Build(1);

         prompt.Should().Contain(SectionHeading);
         prompt.Should().Contain("no soldiers of your own");
         prompt.Should().NotContain("only a handful of men");
      }

      // The whole point of the qualitative band: the exact metagame figure never reaches the model.
      [Test]
      public void GIVEN_a_positive_npc_troop_count_WHEN_the_prompt_is_built_THEN_the_exact_figure_does_not_leak()
         => Build(321).Should().NotContain("321 soldiers");
   }
}

// Code written by Gabriel Mailhot, 18/07/2026.
// Player report: NPCs taunted the player for "approaching alone, brave or foolish" while a whole army
// stood at their back — the troop count only ever reached the player's own companions, so every other
// interlocutor's prompt said nothing about the player's force. The count is now fed to ANY face-to-face
// speaker, but rendered as a coarse qualitative band (never the exact metagame figure) with a tone
// directive, so the NPC weighs what it sees. These tests pin the bands, the section's presence/absence,
// and that the old exact-number phrasing is gone.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PlayerForcePromptTests
   {
      private const string SectionHeading = "THE FORCE AT THE PLAYER'S BACK";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(int troopCount)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {PlayerPartyTroopCount = troopCount});

      // The five bands, pinned at representative values AND at their exact boundaries: an off-by-one in a
      // comparison would silently move a real game state into the wrong description.
      [TestCase(2, "only a handful of companions ride with them.")]
      [TestCase(24, "only a handful of companions ride with them.")]
      [TestCase(25, "a company of a few dozen soldiers rides at their back.")]
      [TestCase(99, "a company of a few dozen soldiers rides at their back.")]
      [TestCase(100, "a warband of well over a hundred soldiers rides at their back.")]
      [TestCase(399, "a warband of well over a hundred soldiers rides at their back.")]
      [TestCase(400, "several hundred soldiers march at their back.")]
      [TestCase(999, "several hundred soldiers march at their back.")]
      [TestCase(1000, "an army numbering in the thousands marches at their back.")]
      [TestCase(5000, "an army numbering in the thousands marches at their back.")]
      public void GIVEN_a_troop_count_WHEN_the_prompt_is_built_THEN_the_matching_force_band_is_rendered(int count, string band)
         => Build(count).Should().Contain(band);

      // The root fix: any interlocutor with a force behind the player gets the section, with the tone
      // directive that kills the "you approach me alone" cliché.
      [Test]
      public void GIVEN_a_positive_troop_count_WHEN_the_prompt_is_built_THEN_the_force_section_and_tone_directive_render()
      {
         string prompt = Build(300);

         prompt.Should().Contain(SectionHeading);
         prompt.Should().Contain("The player does not stand alone:");
         prompt.Should().Contain("Never call the player alone, unescorted, brave-or-foolish for approaching, or an easy mark");
      }

      // 0 = not provided (a captive player, or no main party): the section must stay silent rather than
      // claim a force that is not there.
      [Test]
      public void GIVEN_no_troop_count_WHEN_the_prompt_is_built_THEN_the_force_section_is_absent()
      {
         string prompt = Build(0);

         prompt.Should().NotContain(SectionHeading);
         prompt.Should().NotContain("does not stand alone");
      }

      // THE SOLO CASE, and it shipped WRONG (Gabriel, in play, 2026-07-19). A party of one is the player
      // themselves: the roster counts them. Reported through the escort band it told the NPC "only a handful
      // of companions ride with them" about someone riding alone, and the test above pinned that as correct
      // (it read TestCase(1, ...) until today), which is precisely how it survived.
      [Test]
      public void GIVEN_a_party_of_one_WHEN_the_prompt_is_built_THEN_the_player_is_not_given_companions()
      {
         string prompt = Build(1);

         prompt.Should().NotContain("only a handful of companions");
         prompt.Should().NotContain("does not stand alone");
      }

      // And the silence had to be broken, not merely corrected. With no line at all the model furnishes the
      // road itself: the scene narration put riders behind a solitary player because nothing said otherwise.
      // The solitude is now a stated visible fact, like the force it replaces.
      [Test]
      public void GIVEN_a_party_of_one_WHEN_the_prompt_is_built_THEN_the_solitude_is_stated_outright()
      {
         string prompt = Build(1);

         prompt.Should().Contain("THE PLAYER RIDES ALONE");
         prompt.Should().Contain("no escort, no retinue, no soldiers, not one companion");
         prompt.Should().Contain("Do not invent riders, guards or servants");
      }

      // The distinction the whole fix rests on: one is alone, two is an escort. Pinned at the boundary
      // because an off-by-one here is the entire bug.
      [Test]
      public void GIVEN_a_party_of_two_WHEN_the_prompt_is_built_THEN_it_is_an_escort_and_not_solitude()
      {
         string prompt = Build(2);

         prompt.Should().Contain(SectionHeading);
         prompt.Should().NotContain("THE PLAYER RIDES ALONE");
      }

      // A captive has no force AND is not riding anywhere: neither block applies, which is why 0 stays
      // distinct from 1 rather than being folded into the new solitude wording.
      [Test]
      public void GIVEN_no_troop_count_WHEN_the_prompt_is_built_THEN_the_solitude_block_is_absent_too()
         => Build(0).Should().NotContain("THE PLAYER RIDES ALONE");

      // The whole point of the qualitative band: the exact metagame figure never reaches the model.
      // The old rendering ("The player currently leads N soldiers in their party.") is gone.
      [Test]
      public void GIVEN_a_positive_troop_count_WHEN_the_prompt_is_built_THEN_the_exact_figure_does_not_leak()
         => Build(321).Should().NotContain("321 soldiers");
   }
}

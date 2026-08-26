// Code written by Gabriel Mailhot, 26/08/2026.
// Guards the history block when CR is added to a campaign ALREADY under way. A fresh profile starts with zero
// recorded events, and the block used to say "You have never met this player before" purely on Events.Count == 0.
// For an established spouse or close relative that flatly contradicts the marital/kin framing elsewhere in the
// same prompt, so they opened as strangers (field report, in-progress-save spouse "Lady Liena", 2026-08-25). The
// block now names the standing bond when the encounter says this NPC is the player's spouse or household, and
// keeps the genuine first-encounter line for everyone else. If this regresses, either an existing spouse greets
// their own husband/wife as a stranger again, or a true stranger is wrongly told they already know the player.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AppendHistoryEstablishedBondTests
   {
      // A profile with no recorded events, exactly what CR mints the first time it meets an NPC on a save that
      // predates the mod. This is the untested path the fix targets: empty ledger, but a real standing bond.
      private static NpcProfile FreshNpc() => new() {
         Id = "npc_test", Name = "Test Hero", Faction = "Vlandia", Clan = "dey Meroc",
         Romantic = new RomanticProfile {IsFemale = true, Orientation = SexualOrientation.Heterosexual}
      };

      private static string Prompt(EncounterContext context)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
           .BuildSystemPrompt(FreshNpc(), new WorldState {CurrentDay = 10}, context);

      // THE FIX, spouse half: an established spouse on an in-progress save has no CR events yet, but must never be
      // told this is a first encounter, or they answer their own husband/wife as a stranger.
      [Test]
      public void GIVEN_no_events_AND_the_spouse_is_the_player_WHEN_building_the_prompt_THEN_the_bond_is_named_not_a_first_encounter()
      {
         string prompt = Prompt(new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcSpouseIsPlayer = true, NpcIsPlayerHousehold = true});

         prompt.Should().Contain("this player is your own spouse");
         prompt.Should().NotContain("You have never met this player before");
      }

      // THE FIX, kin half: a parent, child, or sibling of the player (household but not the spouse) gets the
      // family framing rather than the stranger line, and never the spouse wording meant only for a spouse.
      [Test]
      public void GIVEN_no_events_AND_the_npc_is_close_kin_WHEN_building_the_prompt_THEN_the_kin_bond_is_named()
      {
         string prompt = Prompt(new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcSpouseIsPlayer = false, NpcIsPlayerHousehold = true});

         prompt.Should().Contain("this player is close kin of yours");
         prompt.Should().NotContain("You have never met this player before");
         prompt.Should().NotContain("this player is your own spouse");
      }

      // The regression guard: an actual stranger (no bond, empty ledger) must still be told plainly that this is
      // a first encounter, or CR would invent a shared past with everyone the player has genuinely never met.
      [Test]
      public void GIVEN_no_events_AND_no_household_bond_WHEN_building_the_prompt_THEN_it_is_still_a_first_encounter()
      {
         string prompt = Prompt(new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcSpouseIsPlayer = false, NpcIsPlayerHousehold = false});

         prompt.Should().Contain("You have never met this player before");
         prompt.Should().NotContain("this player is your own spouse");
         prompt.Should().NotContain("this player is close kin of yours");
      }
   }
}

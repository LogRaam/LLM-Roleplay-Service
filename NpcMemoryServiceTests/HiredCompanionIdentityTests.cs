// Code written by Gabriel Mailhot, 26/10/2026.
// Guards who a hired companion is told they are. fearente (Nexus, 26/10/2026): "a companion that I hired a week ago
// talked like we were of the same clan from childhood to now", playing hide and seek in the same house. The game counts a
// hired companion in the player's clan, and the prompt said so with nothing else, so a model invented the rest. Identity
// now says what they are, in the compact prompt too. If this regresses, a hireling speaks of a childhood with the player
// again, or a true kinsman is told he is a stranger hired for coin. avalanche24 (Nexus, 28/10/2026): a hireling read "of the
// {player's clan} clan, Vlandia faction" and reasoned she was a lady of that realm "loyal to her house"; another told him to
// "take her men and prove" himself.

#region

using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class HiredCompanionIdentityTests
   {
      private const string HiredLine = "You joined the player's company as a hired companion";

      private static string Prompt(EncounterContext context)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
           .BuildSystemPrompt(new NpcProfile {Id = "w", Name = "Ymira", Faction = "Battania", Clan = "the player's clan", Age = 28, IsFemale = true},
              new WorldState {CurrentDay = 30}, context);

      // STAKE: the reported childhood. A hireling must hear she shares no past with the player, Lean and Full alike.
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_a_hired_companion_WHEN_building_the_prompt_THEN_she_is_told_she_shares_no_childhood_with_the_player(LeanPromptLevel lean)
      {
         string prompt = Prompt(new EncounterContext {LeanLevel = lean, IsHiredCompanion = true, DaysSinceJoiningPlayer = 7});

         prompt.Should().Contain(HiredLine + " some 7 days ago");
         prompt.Should().Contain("share no childhood or family past with them");
      }

      // STAKE: an older save never kept the day she joined; the line must still say what she is, without a false date.
      [Test]
      public void GIVEN_the_day_she_joined_was_never_kept_WHEN_building_the_prompt_THEN_she_is_told_what_she_is_without_a_date()
      {
         string prompt = Prompt(new EncounterContext {IsHiredCompanion = true});

         prompt.Should().Contain(HiredLine + ":");
         prompt.Should().NotContain("days ago");
      }

      // STAKE: kin and lords of the house are not hirelings; telling them so would erase a real family past.
      [Test]
      public void GIVEN_a_character_who_is_not_a_hired_companion_WHEN_building_the_prompt_THEN_no_such_line_appears()
      {
         Prompt(new EncounterContext()).Should().NotContain(HiredLine);
      }

      // STAKE: avalanche24's companion reasoned she was "from the Rauvegroving clan of Vlandia ... loyal to her house":
      // the header must not name the player's clan and realm as hers.
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_a_hired_companion_WHEN_building_the_prompt_THEN_her_header_names_no_house_or_realm_as_hers(LeanPromptLevel lean)
      {
         string prompt = Prompt(new EncounterContext {LeanLevel = lean, IsHiredCompanion = true});

         prompt.Should().Contain("YOU ARE YMIRA, a woman of 28 years, with no noble house of your own, riding for hire in the player's company.");
         prompt.Should().NotContain("of the the player's clan clan").And.NotContain("Battania faction");
      }

      // STAKE: "take her men and prove yourself", a duel for being told to follow orders: she must hear who commands.
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_a_hired_companion_WHEN_building_the_prompt_THEN_she_hears_the_player_is_her_captain(LeanPromptLevel lean)
      {
         string prompt = Prompt(new EncounterContext {LeanLevel = lean, IsHiredCompanion = true});

         prompt.Should().Contain("the player is your captain and pays your wage");
         prompt.Should().Contain("you never command their men, never set them tests or ask them to prove themselves to you");
      }

      // STAKE: the house she serves is the player's, not hers: "You are of the player's house" is what made her a lady.
      [TestCase(true)]
      [TestCase(false)]
      public void GIVEN_a_hired_companion_WHEN_her_house_is_told_THEN_she_serves_it_rather_than_belongs_to_it(bool lean)
      {
         var sb = new StringBuilder();
         new HouseStandingPack().Render(sb, new EncounterContext {
            IsHiredCompanion = true,
            HouseStanding = new HouseStandingFacts {HouseName = "Rauvegroving", IsPlayerHouse = true, Fiefs = new List<string> {"Jaculan"}}
         }, lean);

         sb.ToString().Should().Contain("You serve the Rauvegroving, the player's own house").And.Contain("The house you serve holds Jaculan");
         sb.ToString().Should().NotContain("You are of the Rauvegroving");
      }

      // STAKE: a kinsman of the house still belongs to it; the hireling wording must not reach him.
      [Test]
      public void GIVEN_kin_of_the_player_house_WHEN_his_house_is_told_THEN_he_is_of_it()
      {
         var sb = new StringBuilder();
         new HouseStandingPack().Render(sb, new EncounterContext {HouseStanding = new HouseStandingFacts {HouseName = "Rauvegroving", IsPlayerHouse = true}}, false);

         sb.ToString().Should().Contain("You are of the Rauvegroving, which is the player's own house.");
      }
   }
}

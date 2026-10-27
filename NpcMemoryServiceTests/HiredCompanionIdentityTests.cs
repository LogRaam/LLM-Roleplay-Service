// Code written by Gabriel Mailhot, 26/10/2026.
// Guards who a hired companion is told they are. fearente (Nexus, 26/10/2026): "a companion that I hired a week ago
// talked like we were of the same clan from childhood to now", playing hide and seek in the same house. The game counts a
// hired companion in the player's clan, and the prompt said so with nothing else, so a model invented the rest. Identity
// now says what they are, in the compact prompt too. If this regresses, a hireling speaks of a childhood with the player
// again, or a true kinsman is told he is a stranger hired for coin.

#region

using FluentAssertions;
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
   }
}

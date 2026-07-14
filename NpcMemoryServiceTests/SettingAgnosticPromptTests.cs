// Code written by Gabriel Mailhot, 13/07/2026.
// Calradia Remembers is meant to be the inbound narrative BUS of a modlist (the Extension Surface pillar), and the
// mods with the most to gain from it are TOTAL CONVERSIONS: the ones that replace Calradia with another world
// entirely. A prompt that hardcodes "Calradia" is actively hostile to exactly those mods, and it told a character
// authored for another setting to "import no lore from outside Calradia" (Gabriel, 2026-07-13).
//
// The mechanism to do this properly already existed: PromptLore.WorldName / WorldAdjective, loaded from the
// setting profile a conversion ships. Four prompt lines had simply been written before it existed and never
// converted. These tests pin the rule so the leak cannot come back: nothing the model reads may name the world
// except through PromptLore. Swap the world, and every sentence follows.

#region

using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using FluentAssertions;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SettingAgnosticPromptTests
   {
      private const string OtherWorld = "Middle-earth";
      private const string OtherAdjective = "Gondorian";

      [SetUp]
      public void SwapTheWorld()
      {
         PromptLore.WorldName = OtherWorld;
         PromptLore.WorldAdjective = OtherAdjective;
      }

      [TearDown]
      public void RestoreCalradia()
      {
         PromptLore.WorldName = "Calradia";
         PromptLore.WorldAdjective = "Calradian";
      }

      private static string BuildPromptFor(NpcProfile npc)
         => new PromptBuilder().BuildSystemPrompt(npc, new WorldState(), new EncounterContext());

      /// <summary>An NPC with an authored backstory, which is where the reported leak lived.</summary>
      private static NpcProfile Authored() => new() {
         Id = "test_hero",
         Name = "Beregond",
         Clan = "Guard",
         Faction = "Gondor",
         AuthoredBackstory = "A weary guardsman who speaks in short, clipped sentences."
      };

      // The reported leak. A player writing a character for a total conversion was being told, in our own prompt,
      // to keep lore out of a world their mod had replaced. The guard itself is right (do not import lore from
      // OUTSIDE the setting); it just has to mean the setting the player is actually playing.
      [Test]
      public void GIVEN_a_conversion_that_replaced_the_world_WHEN_the_backstory_guard_is_written_THEN_it_names_THEIR_world()
      {
         string prompt = BuildPromptFor(Authored());

         prompt.Should().Contain($"outside {OtherWorld}");
         prompt.Should().NotContain("outside Calradia");
      }

      // The whole prompt, not just the one line that was reported. A leak anywhere else would break immersion just
      // as badly for a conversion, and would be found the same slow way: by a player, months later.
      [Test]
      public void GIVEN_a_conversion_that_replaced_the_world_WHEN_the_whole_prompt_is_built_THEN_Calradia_is_never_named()
      {
         string prompt = BuildPromptFor(Authored());

         prompt.Should().NotContain("Calradia");
         prompt.Should().NotContain("Calradian");
      }

      // And the default must be untouched: a player on ordinary Bannerlord, who ships no setting profile of their
      // own, must still be told they are in Calradia. A fix for conversions that broke the base game would be no fix.
      [Test]
      public void GIVEN_no_conversion_WHEN_the_prompt_is_built_THEN_it_still_speaks_of_Calradia()
      {
         RestoreCalradia();

         string prompt = BuildPromptFor(Authored());

         prompt.Should().Contain("Calradia");
      }
   }
}

// Code written by Gabriel Mailhot, 05/10/2026.
// "The player" is a word of these instructions, never of the story. Felido (Nexus, 05/10/2026), Qwen 3.5 locally with
// the compact prompt: characters wrote "the player" in their own gestures. The prompt says "the player" hundreds of
// times as instruction vocabulary; the council prompt already forbade echoing it ("a system label, not a name"), the
// conversation prompt only mentioned it inside the rule against thinking aloud, and the Lean reminder, the one a small
// model reads last, not at all. If this breaks, a character addresses the hero as "the player".

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PlayerLabelTests
   {
      private static string Prompt(LeanPromptLevel lean)
         => new PromptBuilder {PlayerName = "Bran"}.BuildSystemPrompt(
            new NpcProfile {Id = "n", Name = "Darim", Faction = "Vlandia", Clan = ""},
            new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = lean});

      // The small model's last reminder says it plainly: the compact prompt is where Felido met it.
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_any_prompt_WHEN_the_format_is_recalled_THEN_the_character_is_told_never_to_write_the_player(LeanPromptLevel lean)
      {
         Prompt(lean).Should().Contain("Never write \"the player\"");
      }
   }
}

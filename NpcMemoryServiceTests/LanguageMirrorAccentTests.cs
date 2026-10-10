// Code written by Gabriel Mailhot, 10/10/2026.
// elpyf0 (Nexus, 09/10/2026): asked in the behaviour guidelines for Vlandians with a French cadence and foul-mouthed
// Scots for Battanians, the model "refuses to write stuff phonetically". The language rule near the end of the prompt
// says the reply follows the player's language and "overrides everything else", which a careful model reads as a ban on
// accented speech. The rule now says an accent or dialect is that same language. If this breaks, an accent a player or
// modder asked for is quietly corrected back to clean speech.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LanguageMirrorAccentTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_accent", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};

      // Both prompt sizes keep the accent once the language rule has been stated.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_the_language_rule_WHEN_the_prompt_is_built_THEN_an_accent_is_named_as_the_same_language(LeanPromptLevel lean)
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = lean});

         int rule = prompt.IndexOf("LANGUAGE OF YOUR REPLY", System.StringComparison.Ordinal);
         int accent = prompt.IndexOf("accent", rule, System.StringComparison.Ordinal);

         rule.Should().BeGreaterThan(0);
         accent.Should().BeGreaterThan(rule);
      }
   }
}

// Code written by Gabriel Mailhot, 19/11/2026.
// The DIALOGUE STYLE block shows a stage direction and a spoken line as an example. Eisendrache2 (Nexus, 19/11/2026,
// Gemma 3 12B): "Many lords claim, that their fathers have died at the battle at Pendraic. But more than often enough,
// their fathers are still alive and well! And whenever I pointed that out, many doubt me, or even claim that I am
// lying." The example line was "My father said much the same, before he fell at Pendraic.", in every prompt, the
// compact one too, and a small model copies the example as its own life. If an example states a fact again, every
// character who copies it gains a dead parent, a battle or a place that is not theirs.

#region

using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class DialogueStyleExampleTests
   {
      private static readonly string[] FactWords = {
         "father", "mother", "brother", "sister", "son", "daughter", "wife", "husband",
         "fell", "died", "dead", "killed", "battle", "war", "Pendraic", "at", "in"
      };

      // STAKE: a character copies the example; it must not hand them a dead father or a battle they never fought.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_the_dialogue_style_block_WHEN_built_THEN_its_example_lines_state_nothing_about_the_world(LeanPromptLevel level)
      {
         var npc = new NpcProfile {Id = "npc_test", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};

         string prompt = new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = level});

         int start = prompt.IndexOf("stage directions", prompt.IndexOf("DIALOGUE STYLE:"));
         start.Should().BeGreaterThan(-1);
         int end = prompt.IndexOf("Use them sparingly", start);
         string[] examples = prompt.Substring(start, end - start).Split('\n').Where(l => l.StartsWith("  ")).ToArray();

         examples.Should().NotBeEmpty();
         foreach (string line in examples)
            foreach (string word in FactWords)
               Regex.IsMatch(line, $@"\b{word}\b", RegexOptions.IgnoreCase).Should().BeFalse($"the example \"{line.Trim()}\" must state no fact ({word})");
      }
   }
}

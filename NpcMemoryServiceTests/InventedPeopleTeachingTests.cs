// Code written by Gabriel Mailhot, 09/11/2026.
// Pins the line of STAY WITHIN WHAT YOU KNOW that forbids the character to make up a person or a house.
//
// The stake (Lordfadooboo, Nexus, 03/11/2026, 2.8.1): "was asking a character about people they have opinions on and
// they started listing names ive never seen to houses that ive never heard of. i checked the encyclopedia page for
// these names and they were nowhere to be found." The rule named only the player's parents; nothing forbade inventing
// anyone else. NPCs never lie (Gabriel's rule): the answer is vagueness or ignorance, never a made-up name. A small
// local model runs the Lean prompt, so the line must reach it too. If this line is dropped or loses its people and
// houses, characters fill an opinion with invented lords again.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class InventedPeopleTeachingTests
   {
      private static string Build(LeanPromptLevel level)
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"},
            new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = level});

      // Lordfadooboo's invented lords and houses: the character is told, in the full prompt and in the one small local
      // models get, never to name a person or house it was not given, and how to speak of others instead.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_any_prompt_level_WHEN_built_THEN_the_character_is_told_never_to_make_up_a_person_or_a_house(LeanPromptLevel level)
      {
         string prompt = Build(level);

         prompt.Should().Contain("Never make up a name: no person or house");
         prompt.Should().Contain("that is not named in this prompt");
         prompt.Should().Contain("or say you do not know them.");
      }
   }
}

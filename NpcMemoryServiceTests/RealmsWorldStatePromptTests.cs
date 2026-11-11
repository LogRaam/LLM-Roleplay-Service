// Code written by Gabriel Mailhot, 11/11/2026.
// What these pin: that every character is told the realms of the world, the fiefs each holds and those that have fallen.
//
// Schmidt (Discord, 11/11/2026): "NPC's know about the end of their realm and others, as well as the number of fiefs in the
// realm, as right now everyone acts surprised when I tell them that Sturgia is no more, and their last fief taken." The
// world state named only the wars, so a fallen realm vanished from what characters were told. If these fail, the model
// speaks of a destroyed realm as standing, or is surprised to hear what every hall knows.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class RealmsWorldStatePromptTests
   {
      // STAKE: the report. Told that Sturgia has fallen, as common knowledge, a character is not surprised to hear it.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_a_fallen_realm_WHEN_the_prompt_is_built_THEN_every_character_knows_it_as_common_knowledge(LeanPromptLevel level)
      {
         string prompt = Build("Vlandia holds 6 towns and 10 castles. Fallen, holding nothing and no longer a power: Sturgia.", level);

         prompt.Should().Contain("Realms (common knowledge in every hall): Vlandia holds 6 towns and 10 castles. Fallen, holding nothing and no longer a power: Sturgia.");
      }

      // STAKE: a world with nothing to tell (a letter, a bare test) prints no empty "Realms:" line for the model to fill in.
      [Test]
      public void GIVEN_no_realms_to_tell_WHEN_the_prompt_is_built_THEN_no_realms_line_is_written()
      {
         Build(null, LeanPromptLevel.Full).Should().NotContain("Realms (common knowledge");
      }

      #region Helpers

      private static string Build(string realms, LeanPromptLevel level)
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"},
                                                  new WorldState {CurrentDay = 12, Realms = realms}, new EncounterContext {LeanLevel = level});

      #endregion
   }
}

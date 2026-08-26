// Code written by Gabriel Mailhot, 26/08/2026.
// Stolen Love pillar, Increment 2: the mod composes a "your marriage is fraying because of the player" directive
// and hands it to the prompt via EncounterContext.NpcMarriageFrayingNote. The SDK's only job is to place it
// verbatim and to render nothing when there is none. If the call were dropped from BuildSystemPrompt, an active
// estrangement would never reach the model and the fraying arc would be invisible; if it rendered when blank, an
// ordinary conversation would carry a stray heading.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class NpcMarriageFrayingNoteTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test", Name = "Test Hero", Faction = "Vlandia", Clan = "dey Meroc",
         Romantic = new RomanticProfile {IsFemale = true, Orientation = SexualOrientation.Heterosexual}
      };

      private static string Prompt(string frayingNote)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcMarriageFrayingNote = frayingNote});

      // The note must reach the model verbatim, or an active estrangement the mod detected is silently dropped and
      // the NPC never voices the rift the pillar is built on.
      [Test]
      public void GIVEN_a_fraying_note_WHEN_building_the_prompt_THEN_it_is_rendered_verbatim()
      {
         Prompt("YOUR MARRIAGE IS FRAYING BECAUSE OF THE PLAYER: a distinctive test line.")
            .Should().Contain("YOUR MARRIAGE IS FRAYING BECAUSE OF THE PLAYER: a distinctive test line.");
      }

      // The overwhelmingly common case (no affair, no estrangement) must carry no trace of the note, or every
      // ordinary conversation would sprout a stray fraying-marriage heading.
      [Test]
      public void GIVEN_no_fraying_note_WHEN_building_the_prompt_THEN_nothing_is_rendered()
      {
         Prompt(null).Should().NotContain("MARRIAGE IS FRAYING");
      }
   }
}

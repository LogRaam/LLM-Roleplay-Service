// Code written by Gabriel Mailhot, 10/07/2026.
// Geography grounding: EncounterContext.GeographyNote is host-composed from the engine's own map data
// and rendered verbatim by PromptBuilder under a "THE LAY OF THE LAND" header, so NPCs stop inventing
// where places sit. Null by default, so the header is absent unless the resolver supplied something.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GeographyNotePromptTests
   {
      private const string Header = "THE LAY OF THE LAND";
      private const string Note = "Pravend is a Vlandian town on the western coast, north of Sargot; GEOGRAPHY_TEST_MARKER.";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The note is built game-side from the engine's own map data (encyclopedia blurb + bearing), never a
      // hand-authored lorebook (ROADMAP: a hand lorebook would drift from the real map). If the header or the
      // note itself fails to reach the prompt, the NPC has nothing grounding it and reverts to inventing
      // where places sit, the exact bug this pillar exists to fix.
      [Test]
      public void GIVEN_a_geography_note_WHEN_building_the_prompt_THEN_it_is_injected_under_the_lay_of_the_land_header()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, GeographyNote = Note};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Header);
         prompt.Should().Contain(Note);
      }

      // Blank guard: with nothing concrete to ground (unknown settlement, no place named), the header must
      // not appear empty, which would read as "treat the following as fact" over nothing at all.
      [Test]
      public void GIVEN_no_geography_note_WHEN_building_the_prompt_THEN_the_lay_of_the_land_header_is_absent()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, GeographyNote = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Header);
         promptBlank.Should().NotContain(Header);
      }
   }
}

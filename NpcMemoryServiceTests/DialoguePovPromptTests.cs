// Code written by Gabriel Mailhot, 16/08/2026.
// The [DIALOGUE] block weaves the NPC's own *gestures* into the reply. A model wrote the NPC's OWN actions in the
// SECOND person - "*Your eyes narrow*", "*you step back from your horse*" - which reads as the PLAYER doing them
// (player report). The response format must state that the NPC's own gestures are FIRST person and that "you" is
// the player only.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class DialoguePovPromptTests
   {
      // The exact bug: the NPC narrated its own eyes/voice/movements as "your ...". The full response format must
      // teach that the NPC's own gestures are first person and "you"/"your" is the player, never the NPC itself.
      [Test]
      public void GIVEN_the_full_response_format_WHEN_built_THEN_the_npcs_own_gestures_are_first_person_and_you_is_the_player()
      {
         var npc = new NpcProfile {Id = "npc_test", Name = "Leontia", Faction = "Empire", Clan = "Osticos"};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full};

         string prompt = new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("refer ONLY to the PLAYER");
         prompt.Should().Contain("I narrow my eyes");
      }
   }
}

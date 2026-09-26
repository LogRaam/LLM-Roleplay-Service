// Code written by Gabriel Mailhot, 26/09/2026.
// The heir note says how the predecessor left. Since 2.5.24 the mod tells a retirement from a death (Wallace1067's
// character switch, and a real retirement, both disable the old hero rather than kill him), yet the prompt still told
// every NPC the predecessor "has died". A retired predecessor is alive and can be met: calling him dead is a
// falsehood the player can check, and reads as the model making things up.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class InheritedNoteTests
   {
      private static string PromptFor(bool retired)
      {
         var npc = new NpcProfile {
            Id = "n", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc",
            InheritedFromName = "Arwa", InheritedKinship = "mother", InheritedPredecessorRetired = retired
         };

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10});
      }

      // A death stays a death, and every profile reframed before this fix reads as one, as it always did.
      [Test]
      public void GIVEN_a_predecessor_who_died_WHEN_the_heir_is_met_THEN_the_note_says_so()
      {
         PromptFor(retired: false).Should().Contain("HEIR of Arwa, their mother, who has died.");
      }

      // The fix: a retired predecessor is alive, and the note says it.
      [Test]
      public void GIVEN_a_predecessor_who_retired_WHEN_the_heir_is_met_THEN_the_note_says_they_still_live()
      {
         string prompt = PromptFor(retired: true);

         prompt.Should().Contain("HEIR of Arwa, their mother, who has stepped down and still lives.");
         prompt.Should().NotContain("who has died");
      }
   }
}

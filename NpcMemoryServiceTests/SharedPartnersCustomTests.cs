// Code written by Gabriel Mailhot, 12/09/2026.
// What these pin: that a character whose culture accepts shared partners is TOLD so, and told what their
// grievance actually is.
//
// Player report, 2026-09-12: "Even if an NPC from the Empire accepts polyamory, their affinity continues to
// drop. Since there is no specific information regarding jealousy, it is possible that a recent update -
// intended to align views on romance with the culture - has caused an internal conflict or inconsistency."
//
// There WAS an inconsistency, and this is it. The romantic profile has been telling such a character they are
// "comfortable with multiple committed partners" while the jealousy ledger quietly docked them for exactly
// that. The custom that reconciles the two - by their people's practice the grievance is RANK, not betrayal -
// existed in the code (JealousyCulture.ToleratesSharedPartners) and fed only the selector that picks who is
// most aggrieved. It never reached the prompt. So the character could not say what the number was doing, and
// the player had a falling bar with no explanation anywhere.
//
// It is said BESIDE the preference it qualifies rather than as a pack of its own: two sources for one fact is
// how two sources come to disagree, which is the rule that closed the "what the player is carrying" candidate
// without a line being written.
//
// If these fail, a character is back to being contradicted by their own ledger.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;
using System.Collections.Generic;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class SharedPartnersCustomTests
   {
      private static string Build(bool customary)
         => new PromptBuilder {
               // The romantic section is gated on adult content being above Off, so a builder left at its
               // default would measure a prompt in which none of this exists - the "guard that measures its
               // own construction" trap.
               AdultLevel = AdultContentLevel.Mature
            }
            .BuildSystemPrompt(
            new NpcProfile {
               Id = "n", Name = "Alympia", Faction = "Empire", Clan = "Leonipardes", Age = 40,
               Romantic = new RomanticProfile {
                  IsFemale = true,
                  Preferences = new List<RomanticPreference> {RomanticPreference.Polyamorous}
               }
            },
            new WorldState {CurrentDay = 10},
            new EncounterContext {SharedPartnersAreCustomary = customary});

      // THE REPORT. The character was already being told they are comfortable with several partners; now they
      // are also told why that does not make the player's next partner a betrayal.
      [Test]
      public void GIVEN_a_culture_that_accepts_it_WHEN_the_prompt_is_built_THEN_the_character_knows_it_is_no_betrayal()
      {
         string said = Build(true);

         said.Should().Contain("Comfortable with multiple committed partners.");
         said.Should().Contain("NOT a betrayal");
      }

      // And the half that makes the remaining regard loss make sense rather than look like a bug: what they
      // mind is being set beneath somebody, which is exactly what the ledger has been charging for.
      [Test]
      public void GIVEN_the_same_culture_WHEN_told_THEN_the_grievance_named_is_rank_and_not_the_arrangement()
      {
         string said = Build(true);

         said.Should().Contain("STANDING among them");
         said.Should().Contain("Jealousy of the arrangement itself is not in your nature");
      }

      // The line must not appear for everybody, or an exclusive partner would be told to shrug at infidelity -
      // a far worse bug than the one being fixed.
      [Test]
      public void GIVEN_a_culture_that_does_not_accept_it_WHEN_built_THEN_nothing_excuses_a_betrayal()
      {
         string said = Build(false);

         said.Should().NotContain("NOT a betrayal");
         said.Should().NotContain("Jealousy of the arrangement itself");
      }

      // It is said where the preference is said. Asserted so nobody later "tidies" it into a pack of its own
      // and leaves two places describing one custom.
      [Test]
      public void GIVEN_the_custom_WHEN_rendered_THEN_it_sits_beside_the_preference_it_qualifies()
      {
         string said = Build(true);

         said.IndexOf("NOT a betrayal")
             .Should().BeGreaterThan(said.IndexOf("Comfortable with multiple committed partners."));
      }
   }
}

// Code written by Gabriel Mailhot, 28/10/2026.
// A lord's letter about an aim of his own that another mod registered for him through CrAims (tashmetu, 14/10/2026:
// "nobody takes the first step with the player"; Gabriel, 28/10/2026: "fait 2 et 1"). The letter is written from the
// Context the mod's aim gives and from nothing else. If this regresses, the lord promises a reward, a deadline or a name
// the mod never gave, or an old save reads its letters under the wrong reason.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AimLetterPromptTests
   {
      private static NpcProfile Lord() => new() {Id = "herra", Name = "Herra", Faction = "Empire", Clan = "Osticos"};

      // STAKE: the lord writes of his want and the player's part as the mod gave them, inventing nothing, never lying.
      [Test]
      public void GIVEN_a_lords_aim_WHEN_his_letter_is_written_THEN_he_writes_only_what_the_context_gives_and_never_a_falsehood()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Lord(), LetterReason.StandingAim, "What you want, in your own terms: Lakonon.", "Fercread");

         prompt.Should().Contain("Write ONLY of that: invent no promise, no reward, no deadline and no name the Context does not give");
         prompt.Should().Contain("never a falsehood");
         prompt.Should().Contain("Lakonon.");
      }

      // STAKE: a LetterReason is saved as its ordinal; the new one must follow the last before it.
      [Test]
      public void GIVEN_the_standing_aim_reason_WHEN_its_ordinal_is_read_THEN_it_follows_the_letter_from_another_mod()
      {
         ((int) LetterReason.StandingAim).Should().Be((int) LetterReason.FromAnotherMod + 1);
      }
   }
}

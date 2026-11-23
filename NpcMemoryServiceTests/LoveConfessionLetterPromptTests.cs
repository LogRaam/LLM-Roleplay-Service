// Code written by Gabriel Mailhot, 23/11/2026.
// The love letter: a character who has come to love the player, and never said so, writes to confess it (Gabriel,
// 23/11/2026: "Une lettre d'aveux ou d'amour serait tres bien"). The occasion tells them to confess and to ask nothing.
// If this regresses, the letter reads as a demand or a bargain, or an old save reads its letters under the wrong reason.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LoveConfessionLetterPromptTests
   {
      private static NpcProfile Lady() => new() {Id = "lady", Name = "Lady Test", Faction = "Vlandia", Clan = "dey Meroc"};

      // STAKE: the letter confesses and leaves the answer to the player; it never asks or bargains.
      [Test]
      public void GIVEN_a_love_confession_WHEN_the_letter_is_written_THEN_it_confesses_and_asks_nothing()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Lady(), LetterReason.LoveConfession, "You share their culture.", "Fercread");

         prompt.Should().Contain("You have come to love the player, and you have never told them.");
         prompt.Should().Contain("It is a confession, not a demand: ask nothing of them");
         prompt.Should().Contain("You share their culture.");
      }

      // STAKE: a LetterReason is saved as its ordinal; the new one must follow the last before it.
      [Test]
      public void GIVEN_the_love_confession_reason_WHEN_its_ordinal_is_read_THEN_it_follows_the_villages_spared()
      {
         ((int) LetterReason.LoveConfession).Should().Be((int) LetterReason.VillagesSpared + 1);
      }
   }
}

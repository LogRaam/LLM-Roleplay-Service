// Code written by Gabriel Mailhot, 18/10/2026.
// A letter another mod wrote in full for one of its heroes (CrLetters.Deliver; tashmetu, Regard and Standing,
// 18/10/2026: a lord's tournament invitation shown in the mod's courier window). Its body was never generated here, so
// when the player answers it, the sender is shown what he wrote; otherwise he replies as if he had invited no one.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LetterFromAnotherModPromptTests
   {
      private static NpcProfile Lord() => new() {Id = "derthert", Name = "Derthert", Faction = "Vlandia", Clan = "dey Valant"};

      // STAKE: the player answers an invitation another mod wrote; the lord knows what he wrote and keeps to it.
      [Test]
      public void GIVEN_a_reply_to_a_letter_another_mod_wrote_WHEN_built_THEN_the_sender_is_shown_what_he_wrote()
      {
         string prompt = LetterPromptBuilder.BuildReplyLetterMessage(Lord(), "I will come.", LetterReason.FromAnotherMod, "Fercread",
                                                                     yourLetter: "Come to the lists at Pravend in five days.");

         prompt.Should().Contain("Come to the lists at Pravend in five days.");
         prompt.Should().Contain("add no new matter");
      }

      // STAKE: CR's own letters keep their replies as they were: nothing is quoted when no letter is given.
      [Test]
      public void GIVEN_a_reply_to_one_of_CRs_own_letters_WHEN_built_THEN_no_letter_is_quoted()
      {
         LetterPromptBuilder.BuildReplyLetterMessage(Lord(), "Thank you.", LetterReason.StanceFavor, "Fercread")
                            .Should().NotContain("Your letter, which they answer");
      }
   }
}

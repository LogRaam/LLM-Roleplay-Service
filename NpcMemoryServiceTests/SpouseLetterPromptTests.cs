// Code written by Gabriel Mailhot, 12/11/2026.
// Aporo (Discord, 12/11/2026): "can i reduce somehow amount of letters i get from my wife? she writes to me about someone
// who won a lot of tournaments or that some random castle was taken". Gabriel: the player's wife should speak more of
// family, love and personal things. A spouse's letter was told only to carry "news", so the writer filled it with the
// world's. If this line is lost, a wife's letters read like a herald's bulletin again.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SpouseLetterPromptTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_test", Name = "Ira", Faction = "Battania", Clan = "Fen Duran"};

      // STAKE: a wife writes of tournaments and far castles instead of home and the two of them.
      [Test]
      public void GIVEN_a_spouse_letter_WHEN_built_THEN_it_writes_of_home_and_keeps_world_news_to_what_touches_the_family()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Npc(), LetterReason.SpouseCorrespondence, "", "Gabriel");

         prompt.Should().Contain(LetterPromptBuilder.SpouseLetterLine);
         LetterPromptBuilder.SpouseLetterLine.Should().Contain("Write of home and of the two of you")
            .And.Contain("ONLY when it touches your family, your house or the player directly");
      }

      // STAKE: a strained marriage must still be allowed its worry or reproach; only the subject changed, not the feeling.
      [Test]
      public void GIVEN_a_spouse_letter_WHEN_built_THEN_the_tone_still_follows_the_current_feeling()
      {
         LetterPromptBuilder.SpouseLetterLine.Should().Contain("CURRENT feeling").And.Contain("worry or reproach");
      }
   }
}

// Code written by Gabriel Mailhot, 28/09/2026.
// Lordfadooboo (Nexus, 28/09/2026): a dozen letters exchanged and the correspondent's regard had not moved, because a
// letter may emit no [ACTION] and change_relation never ran. The reader now judges each letter the player sends, on a
// [LETTER_TONE] line in the SAME call that already writes the reply, and the mod decides what that is worth. Both
// paths where the reader answers the player's words must ask for it, or letters on that path stay worthless; and the
// line must be asked for even when the reader declines to answer, since an insult is the letter most worth judging.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LetterTonePromptTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_test", Name = "Ira", Faction = "Battania", Clan = "Fen Duran"};

      // A letter the player wrote unprompted: the reader says how it left them, reply or not.
      [Test]
      public void GIVEN_a_player_letter_WHEN_the_reader_decides_on_a_reply_THEN_they_are_asked_how_it_left_them()
      {
         string prompt = LetterPromptBuilder.BuildPlayerLetterReplyDecisionMessage(Npc(), "A word with you.", "Gabriel");

         prompt.Should().Contain("[LETTER_TONE] warmer | same | cooler");
         prompt.Should().Contain("Even when you PASS");
      }

      // The player's answer to the reader's own letter: judged the same way.
      [Test]
      public void GIVEN_the_players_answer_to_a_letter_WHEN_the_reader_replies_THEN_they_are_asked_how_it_left_them()
      {
         LetterPromptBuilder.BuildReplyLetterMessage(Npc(), "Well met.", LetterReason.RomanticCorrespondence, "Gabriel")
                            .Should().Contain("[LETTER_TONE] warmer | same | cooler");
      }

      // A letter the reader writes FIRST judges nothing of the player's: asking there would invent a judgement.
      [Test]
      public void GIVEN_a_letter_the_reader_writes_first_WHEN_built_THEN_no_judgement_is_asked()
      {
         LetterPromptBuilder.BuildInitialLetterMessage(Npc(), LetterReason.RomanticCorrespondence, "", "Gabriel")
                            .Should().NotContain("[LETTER_TONE]");
      }
   }
}

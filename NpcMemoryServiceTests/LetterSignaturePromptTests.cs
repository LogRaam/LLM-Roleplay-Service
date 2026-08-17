// Code written by Gabriel Mailhot, 17/08/2026.
// Player report: delivered letters never named their sender, so the player could not tell who to answer (worse
// with a helmeted portrait, or when the letter challenges them to a duel or names a meeting place). The window
// heading shows "A LETTER FROM X", but the letter body read anonymously. Every letter builder must now tell the
// writer to sign with their own name; these pin that instruction across all three letter paths so none regresses
// back to an anonymous letter.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LetterSignaturePromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Ira",
         Faction = "Battania",
         Clan = "Fen Duran"
      };

      // An NPC-initiated letter (a lord writing first): it must be signed so the player knows who reached out.
      [Test]
      public void GIVEN_an_initial_letter_WHEN_built_THEN_the_writer_is_told_to_sign_with_their_name()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(
            Npc(), LetterReason.RomanticCorrespondence, "", "Gabriel");

         prompt.Should().Contain("Sign the letter");
         prompt.Should().Contain("Ira");
      }

      // A reply the NPC sends back to a player letter: same requirement, so a thread does not go anonymous halfway.
      [Test]
      public void GIVEN_a_reply_letter_WHEN_built_THEN_the_writer_is_told_to_sign_with_their_name()
      {
         string prompt = LetterPromptBuilder.BuildReplyLetterMessage(
            Npc(), "Well met.", LetterReason.RomanticCorrespondence, "Gabriel");

         prompt.Should().Contain("Sign the letter");
         prompt.Should().Contain("Ira");
      }

      // The player-initiated reply-decision path (the NPC decides whether and how to answer): it too must sign.
      [Test]
      public void GIVEN_a_player_letter_reply_decision_WHEN_built_THEN_the_writer_is_told_to_sign_with_their_name()
      {
         string prompt = LetterPromptBuilder.BuildPlayerLetterReplyDecisionMessage(
            Npc(), "A word with you.", "Gabriel");

         prompt.Should().Contain("Sign the letter");
         prompt.Should().Contain("Ira");
      }
   }
}

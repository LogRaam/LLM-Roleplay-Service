// Code written by Gabriel Mailhot, 12/11/2026.
// bdionysis80 (Nexus, 12/11/2026): "current date Summer 9 1084 while receiving letters with timestamp summer 1085". A
// letter was written knowing only the absolute day count, so the writer worked the year out for itself and landed one
// year ahead (the game counts its years from 0). The host now puts the calendar date in CURRENT WORLD STATE, and every
// letter path tells the writer that date is the only one it may write. If one path loses the line, its letters go back
// to inventing dates.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LetterDatePromptTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_test", Name = "Ira", Faction = "Battania", Clan = "Fen Duran"};

      // STAKE: a lord writing first dates his letter a year ahead of the player's calendar.
      [Test]
      public void GIVEN_an_initial_letter_WHEN_built_THEN_the_writer_dates_it_only_with_the_given_date()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Npc(), LetterReason.RomanticCorrespondence, "", "Gabriel");

         prompt.Should().Contain(LetterPromptBuilder.DateLine);
      }

      // STAKE: an answer to the player's own letter dates itself in the wrong year.
      [Test]
      public void GIVEN_a_reply_to_a_player_letter_WHEN_built_THEN_the_writer_dates_it_only_with_the_given_date()
      {
         string prompt = LetterPromptBuilder.BuildPlayerLetterReplyDecisionMessage(Npc(), "Greetings.", "Gabriel");

         prompt.Should().Contain(LetterPromptBuilder.DateLine);
      }

      // STAKE: the second letter of a thread jumps a year ahead of the first.
      [Test]
      public void GIVEN_a_reply_letter_WHEN_built_THEN_the_writer_dates_it_only_with_the_given_date()
      {
         string prompt = LetterPromptBuilder.BuildReplyLetterMessage(Npc(), "Thank you.", LetterReason.RomanticCorrespondence, "Gabriel");

         prompt.Should().Contain(LetterPromptBuilder.DateLine);
      }

      // STAKE: a dictated letter (the household writing for a great lord) is dated by the clerk's guess.
      [Test]
      public void GIVEN_a_dictated_letter_WHEN_built_THEN_the_date_line_is_still_there()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Npc(), LetterReason.RomanticCorrespondence, "", "Gabriel", writtenBy: "her steward");

         prompt.Should().Contain(LetterPromptBuilder.DateLine);
      }
   }
}

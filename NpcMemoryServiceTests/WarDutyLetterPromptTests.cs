// Code written by Gabriel Mailhot, 15/10/2026.
// The two letters of the vassal war duty (Maifailun, Nexus 15/10/2026; Gabriel's ladder the same day): the liege's
// SUMMONS to a vassal who sat out the war, to come and answer for it in person, and, when he never came, the LEVY
// demanded by letter. Gating before declaration and NPCs never lie: a letter names only the consequence the game
// will carry out (the Context states it), so the writer is told to threaten nothing else. And both reasons are
// appended to the enum, because a LetterReason is saved as its ordinal.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class WarDutyLetterPromptTests
   {
      private static NpcProfile Liege() => new() {Id = "lucon", Name = "Lucon", Faction = "Northern Empire", Clan = "Lucon"};

      // STAKE: Gabriel: "La lettre devrait etre une convocation ou le joueur doit aller rencontrer le Lord pour rendre
      // des comptes." A summons that read as a friendly note would not tell the player to go.
      [Test]
      public void GIVEN_a_war_summons_WHEN_the_letter_is_written_THEN_the_liege_summons_the_vassal_in_person()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Liege(), LetterReason.WarSummons, "The war with Battania.", "Gabriel");

         prompt.Should().Contain("summon them to come before you in person and answer for their absence");
         prompt.Should().Contain("Context: The war with Battania.");
      }

      // STAKE: a letter threatening a fief the game would never take reads as the model failing (NPCs never lie).
      [Test]
      public void GIVEN_either_war_duty_letter_WHEN_written_THEN_only_the_consequence_the_context_gives_is_named()
      {
         LetterPromptBuilder.BuildInitialLetterMessage(Liege(), LetterReason.WarSummons, "", "Gabriel")
                            .Should().Contain("Name only the consequence the Context gives; threaten nothing it does not.");
         LetterPromptBuilder.BuildInitialLetterMessage(Liege(), LetterReason.WarLevyDemand, "", "Gabriel")
                            .Should().Contain("Name only the consequence the Context gives; threaten nothing it does not.");
      }

      // STAKE: the levy letter must carry the exact sum the game will hold the player to.
      [Test]
      public void GIVEN_a_levy_demand_WHEN_written_THEN_the_sum_is_given_exactly_and_goods_serve_as_well()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Liege(), LetterReason.WarLevyDemand, "3000 denars.", "Gabriel");

         prompt.Should().Contain("with the sum exactly as the Context gives it");
         prompt.Should().Contain("what else may serve in its place");
      }

      // STAKE: a LetterReason is saved as its ordinal; inserting the new ones anywhere but the end would turn every
      // saved letter after them into another kind of letter.
      [Test]
      public void GIVEN_the_war_duty_reasons_WHEN_their_ordinals_are_read_THEN_they_follow_the_last_reason_before_them()
      {
         ((int) LetterReason.WarSummons).Should().Be((int) LetterReason.TournamentResult + 1);
         ((int) LetterReason.WarLevyDemand).Should().Be((int) LetterReason.TournamentResult + 2);
      }

      // STAKE: the writer remembers what the letter was for in plain words, not "in correspondence".
      [Test]
      public void GIVEN_a_summons_sent_WHEN_the_writer_remembers_it_THEN_the_memory_says_what_it_was_for()
      {
         LetterMemoryPolicy.BaseMemory(LetterReason.WarSummons, "Lucon", "Gabriel", true).Should().Contain("answer for sitting out the war");
      }
   }
}

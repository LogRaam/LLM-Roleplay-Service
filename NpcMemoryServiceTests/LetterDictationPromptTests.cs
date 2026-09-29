// Code written by Gabriel Mailhot, 28/09/2026.
// Gabriel (28/09/2026), landless at the start of a campaign: the Empress Rhagaea wrote to him in her own hand. His ruling:
// a greater lord has the letter written for them, under a title with no invented name. The host decides the rank; these
// pin what the letter prompt then says on every path a lord writes to the player, and that a letter in one's own hand is
// unchanged. If the dictation line goes missing, the empress signs her own name again; if it leaks into a personal
// letter, a lover's note arrives from a chancellery.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LetterDictationPromptTests
   {
      private const string Chancellery = "the chancellery of Her Majesty Rhagaea";

      private static NpcProfile Npc() => new() {Id = "npc_test", Name = "Rhagaea", Faction = "Southern Empire", Clan = "Pethros"};

      // All three paths a lord writes on: first, in answer to the player's letter, and in answer to the player's reply.
      [Test]
      public void GIVEN_a_letter_written_on_a_rulers_behalf_WHEN_built_on_any_path_THEN_it_is_dictated_and_signed_with_the_title()
      {
         string[] prompts = {
            LetterPromptBuilder.BuildInitialLetterMessage(Npc(), LetterReason.QuestUpdate, "", "Arwa", writtenBy: Chancellery),
            LetterPromptBuilder.BuildPlayerLetterReplyDecisionMessage(Npc(), "A word.", "Arwa", writtenBy: Chancellery),
            LetterPromptBuilder.BuildReplyLetterMessage(Npc(), "Well met.", LetterReason.QuestUpdate, "Arwa", writtenBy: Chancellery)
         };

         foreach (string prompt in prompts)
         {
            prompt.Should().Contain("You do not write this letter in your own hand");
            prompt.Should().Contain("Sign it: \"The chancellery of Her Majesty Rhagaea\". Name no clerk.");
            prompt.Should().NotContain("Sign the letter at the end with your own name");
         }
      }

      // A letter in one's own hand is exactly as it was: signed with one's own name, no dictation.
      [Test]
      public void GIVEN_a_letter_in_the_senders_own_hand_WHEN_built_THEN_it_is_signed_with_their_own_name()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Npc(), LetterReason.RomanticCorrespondence, "", "Arwa");

         prompt.Should().Contain("Sign the letter at the end with your own name, Rhagaea");
         prompt.Should().NotContain("You do not write this letter in your own hand");
      }
   }
}

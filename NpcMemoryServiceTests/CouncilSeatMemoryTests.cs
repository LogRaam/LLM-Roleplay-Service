// Code written by Gabriel Mailhot, 16/09/2026.
// What these pin: that a seat at the council remembers the player, and that what it remembers is safe to
// say in front of the others.
//
// Until 16/09/2026 the council carried no memory at all. A lord who had fought beside the player, been
// refused by them, or married into their house sat at the table with none of it: every seat met the player
// fresh, every sitting, however long the campaign had run.
//
// THE DANGER THIS OPENS, and the reason these tests exist. A council is ONE prompt covering the WHOLE
// table, so a seat's recall is read by the voice writing every other seat. That is the same exposure as a
// witness's recall — and the witness path's own leak guard carried a comment saying it was "the ONE place
// it is needed", which stopped being true the moment a council seat began carrying memory. The host
// therefore calls that same builder rather than reading events itself, so the guard is inherited rather
// than re-reasoned, and the comment now says so.
//
// If these fail, a confidence the player asked a lord to keep is about to be rendered into a room.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class CouncilSeatMemoryTests
   {
      private const string Recall = "I fought beside them at Pendraic; I refused their offer of gold.";

      // THE POINT OF THE INCREMENT: what a seat remembers reaches the table, attributed to that seat.
      [Test]
      public void GIVEN_a_seat_that_remembers_the_player_WHEN_the_council_prompt_is_built_THEN_the_recall_is_there()
      {
         string prompt = Build(Seat("Ajin the Hawk", Recall));

         prompt.Should().Contain("Ajin the Hawk");
         prompt.Should().Contain(Recall);
      }

      // Attached to its OWN seat. A table describes several people in a row, and a recall folded into the
      // attribute line would read as one more attribute of whoever happened to be listed last.
      [Test]
      public void GIVEN_several_seats_WHEN_the_council_prompt_is_built_THEN_each_recall_sits_under_its_own_name()
      {
         // Scoped to the ROSTER. The builder's own instructions name example speakers ("Sophia startles at his
         // words"), so a whole-prompt search finds a name the table never seated - which is how the first draft
         // of this test failed for a reason that had nothing to do with the code.
         string prompt = Build(
            Seat("Vangvayag the Fatherless", "I fought beside them at Pendraic."),
            Seat("Hophtalamos", "They freed me from a Sturgian cell."));

         string roster = prompt.Substring(prompt.IndexOf("THE COUNCIL PRESENT"));

         int first = roster.IndexOf("Vangvayag the Fatherless");
         int pendraic = roster.IndexOf("Pendraic");
         int second = roster.IndexOf("Hophtalamos");

         first.Should().BeGreaterThanOrEqualTo(0);
         second.Should().BeGreaterThan(first, "the roster lists them in the order they were seated");
         pendraic.Should().BeGreaterThan(first);
         pendraic.Should().BeLessThan(second, "the first seat's recall belongs to him, not to whoever is listed next");
      }

      // A seat that has never dealt with the player says nothing. Common, and not a gap — the prompt must not
      // manufacture an empty "remembers of you:" line for a stranger to read as significant.
      [Test]
      public void GIVEN_a_seat_with_no_history_WHEN_the_council_prompt_is_built_THEN_nothing_is_claimed_for_it()
      {
         Build(Seat("Ajin the Hawk", null)).Should().NotContain("remembers of you");
         Build(Seat("Ajin the Hawk", "   ")).Should().NotContain("remembers of you");
      }

      // The station increment must survive beside the recall: both describe the same seat and neither may
      // displace the other.
      [Test]
      public void GIVEN_a_seat_with_both_a_station_and_a_recall_WHEN_built_THEN_both_reach_the_table()
      {
         var seat = new CouncilMemberInput {
            Name = "Ajin the Hawk", StationLine = "governs Sargot", Memory = Recall
         };

         string prompt = Build(seat);

         prompt.Should().Contain("governs Sargot");
         prompt.Should().Contain(Recall);
      }

      // AUDIT #9, INCREMENT 3: what a seat KNOWS reaches the table. Until 16/09/2026 the council rendered no
      // knowledge pack at all, so a lord who could name his house's fiefs and its wars in a private word could
      // name nothing at his own council.
      [Test]
      public void GIVEN_a_seat_that_knows_things_WHEN_the_council_prompt_is_built_THEN_its_knowledge_reaches_the_table()
      {
         var seat = new CouncilMemberInput {
            Name = "Ajin the Hawk",
            Knowledge = "Your house holds Sargot and Charas." + "\n" + "You are at war with Sturgia."
         };

         string prompt = Build(seat);

         prompt.Should().Contain("Your house holds Sargot and Charas.");
         prompt.Should().Contain("You are at war with Sturgia.");
      }

      // FLATTENED onto one line. The packs render in paragraphs, which is right for a private word and is a
      // wall of text in a roster of six. Nothing is dropped by the flattening - what a seat can afford was
      // already decided by the Compact budget, which is the packs' own rule.
      [Test]
      public void GIVEN_a_seat_whose_knowledge_runs_to_paragraphs_WHEN_built_THEN_it_is_rendered_as_one_line()
      {
         string prompt = Build(new CouncilMemberInput {
            Name = "Ajin the Hawk",
            Knowledge = "Your house holds Sargot." + "\n\n" + "You are at war with Sturgia."
         });

         prompt.Should().Contain("knows: Your house holds Sargot. You are at war with Sturgia.");
      }

      // A seat that knows nothing worth stating says nothing, exactly as with the recall.
      [Test]
      public void GIVEN_a_seat_that_knows_nothing_WHEN_built_THEN_no_empty_line_is_manufactured()
      {
         Build(new CouncilMemberInput {Name = "Ajin the Hawk", Knowledge = null}).Should().NotContain("knows:");
         Build(new CouncilMemberInput {Name = "Ajin the Hawk", Knowledge = "  "}).Should().NotContain("knows:");
      }

      #region private

      private static CouncilMemberInput Seat(string name, string? memory)
         => new() {Name = name, Memory = memory};

      private static string Build(params CouncilMemberInput[] seats)
         => CouncilPromptBuilder.Build(new CouncilPromptInput {
            Roster = new List<CouncilMemberInput>(seats),
            PlayerLine = "What is to be done about the Sturgians?",
            PlayerName = "Arwa"
         });

      #endregion
   }
}

// Code written by Gabriel Mailhot, 29/10/2026.
// Who each seat at the council table is to the others and to the player. fearente (Nexus, 29/10/2026): a lady governor
// spoke to her father, the head of her clan, "like he was a servant". The whole-table call writes every seat in one
// prompt, so the kinship between seats must stand on each seat's own line, beside its rank; without it a daughter and
// her father argue across the table as strangers.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class CouncilSeatKinTests
   {
      // The report, at the table: her father's seat says he is her father, beside the rank he holds.
      [Test]
      public void GIVEN_a_father_and_his_daughter_seated_WHEN_the_table_is_written_THEN_his_line_says_whose_father_he_is()
      {
         string prompt = Build(
            new CouncilMemberInput {Name = "Lord Pethros", StationLine = "heads the Pethros clan", KinAtTable = "father of Lady Ira"},
            new CouncilMemberInput {Name = "Lady Ira", IsFemale = true, StationLine = "governs Lycaron", KinAtTable = "daughter of Lord Pethros"});

         prompt.Should().Contain("heads the Pethros clan; father of Lady Ira");
         prompt.Should().Contain("governs Lycaron; daughter of Lord Pethros");
      }

      // A seat kin to no one present keeps the line it always had.
      [Test]
      public void GIVEN_a_seat_kin_to_no_one_WHEN_written_THEN_no_kin_clause_is_made_up()
      {
         Build(new CouncilMemberInput {Name = "Lord Olek", StationLine = "heads the Olek clan"})
            .Should().Contain("heads the Olek clan; current regard");
      }

      private static string Build(params CouncilMemberInput[] seats)
         => CouncilPromptBuilder.Build(new CouncilPromptInput {
            Roster = new List<CouncilMemberInput>(seats), PlayerLine = "What is to be done?", PlayerName = "Arwa"
         });
   }
}

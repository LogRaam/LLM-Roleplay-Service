// Code written by Gabriel Mailhot, 17/10/2026.
// The patrolling companion's letters (Gabriel, 16/10/2026: "Il patrouille jusqu'a ce que le joueur lui dise d'arreter,
// soit en chat, soit avec une lettre. A chaque mois, le compagnon peut ecrire pour donner des nouvelles au joueur et
// lui demander s'il doit continuer la patrouille."). The monthly report tells only the patrol's real record, and a
// letter from the player can order the patrol home through a control line, since a letter may emit no [ACTION].

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PatrolLetterPromptTests
   {
      private static NpcProfile Companion() => new() {Id = "garmund", Name = "Garmund", Faction = "Battania", Clan = "Player Clan"};

      // STAKE: Gabriel's monthly letter: the real record, and the question whether to keep on; an invented fight
      // reads as the model lying about what the player's own men did.
      [Test]
      public void GIVEN_a_monthly_patrol_report_WHEN_written_THEN_it_tells_only_the_record_and_asks_whether_to_keep_on()
      {
         string prompt = LetterPromptBuilder.BuildInitialLetterMessage(Companion(), LetterReason.PatrolReport, "30 days around Ebereth.", "Fercread");

         prompt.Should().Contain("Report ONLY what the Context gives; invent no fight");
         prompt.Should().Contain("ask plainly whether you should keep patrolling or come back");
      }

      // STAKE: a letter that says "come back" must reach the game as an order, both when the player writes first and
      // when they answer the monthly letter; no line means keep patrolling (Gabriel: no answer, no change).
      [Test]
      public void GIVEN_a_letter_to_a_patrolling_companion_WHEN_its_answer_is_built_THEN_the_stop_line_is_taught_on_both_paths()
      {
         LetterPromptBuilder.BuildPlayerLetterReplyDecisionMessage(Companion(), "Come back.", "Fercread", onPatrol: true)
                            .Should().Contain("[PATROL] stop");
         LetterPromptBuilder.BuildReplyLetterMessage(Companion(), "Come back.", LetterReason.PatrolReport, "Fercread", onPatrol: true)
                            .Should().Contain("[PATROL] stop");
      }

      // STAKE: everyone else's letters stay byte-for-byte what they were.
      [Test]
      public void GIVEN_a_letter_to_anyone_not_on_patrol_WHEN_its_answer_is_built_THEN_no_patrol_line_is_taught()
      {
         LetterPromptBuilder.BuildPlayerLetterReplyDecisionMessage(Companion(), "Hello.", "Fercread").Should().NotContain("[PATROL]");
         LetterPromptBuilder.BuildReplyLetterMessage(Companion(), "Hello.", LetterReason.RealmTidings, "Fercread").Should().NotContain("[PATROL]");
      }

      // STAKE: a LetterReason is saved as its ordinal; the new one must follow the last before it.
      [Test]
      public void GIVEN_the_patrol_report_reason_WHEN_its_ordinal_is_read_THEN_it_follows_the_levy_demand()
      {
         ((int) LetterReason.PatrolReport).Should().Be((int) LetterReason.WarLevyDemand + 1);
      }
   }
}

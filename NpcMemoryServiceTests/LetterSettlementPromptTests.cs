// Code written by Gabriel Mailhot, 22/07/2026.
// Same audit, same family of bug as PrisonerExecutionPromptTests: the fiction can reach an outcome the
// machinery has no channel for in THIS mode. Every letter builder forbids [ACTION] entirely, so no letter can
// ever move coin. Yet the Reply button appears on every delivered letter, including a blackmail demand and a
// child-support request, whose whole point is the player paying. The payment only becomes real through the
// LIVE chat: pay_blackmail for the blackmail (PromptBuilder teaches it in the bastard-mother "mercenary"
// tone) and take_gold for the pension (which stamps the ProvideGold quest via StampProvideGoldIfOutstanding).
// Left unguarded the reply letter answered "your silence is bought", and the player, believing the debt paid,
// then watched BastardBlackmailPolicy expose the secret at the deadline anyway, or the quest lapse. These
// tests pin the redirect and, just as importantly, its narrowness: an ordinary letter must not be polluted
// with debt language it has no reason to carry.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LetterSettlementPromptTests
   {
      private const string SettlementHeading = "NOTHING IS SETTLED BY LETTER.";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Ira",
         Faction = "Battania",
         Clan = "Fen Duran"
      };

      private static string ReplyTo(LetterReason reason) =>
         LetterPromptBuilder.BuildReplyLetterMessage(
            Npc(), "You shall have your denars. Consider the matter closed.", reason, "Gabriel");

      // The reported shape: the player answers the blackmail with a promise of coin, and the model closes the
      // account in prose. The secret then breaks at the deadline regardless, which reads to the player as the
      // mod losing their payment.
      [Test]
      public void GIVEN_a_reply_to_a_blackmail_letter_WHEN_built_THEN_the_debt_is_declared_unsettled()
      {
         ReplyTo(LetterReason.Blackmail).Should().Contain(SettlementHeading);
      }

      // The blackmail's twin, and the reason this fix is not a one-off: a child-support letter asks for coin
      // through exactly the same channel and is satisfied by exactly the same live-chat action, so fixing only
      // the blackmail would have left an identical hole one enum value away.
      [Test]
      public void GIVEN_a_reply_to_a_child_support_letter_WHEN_built_THEN_the_debt_is_declared_unsettled()
      {
         ReplyTo(LetterReason.ChildSupportRequest).Should().Contain(SettlementHeading);
      }

      // The narrowness that keeps the fix honest. A tournament congratulation or a love letter has no debt to
      // leave open, and telling its writer that "no coin has reached you" would be noise in the prompt and
      // could surface as a bizarre non-sequitur in the letter itself.
      [TestCase(LetterReason.RomanticCorrespondence)]
      [TestCase(LetterReason.TournamentVictory)]
      [TestCase(LetterReason.SpouseCorrespondence)]
      public void GIVEN_a_reply_to_a_letter_that_asks_for_nothing_WHEN_built_THEN_no_debt_language_appears(LetterReason reason)
      {
         ReplyTo(reason).Should().NotContain(SettlementHeading);
      }

      // The unknown-reason path: a letter the PLAYER chose to write, where the host cannot know what is being
      // answered and so cannot name the debt. It gets the weaker, always-true form of the same invariant,
      // because it is the only guard available there.
      [Test]
      public void GIVEN_a_player_initiated_letter_WHEN_the_npc_decides_on_a_reply_THEN_it_is_told_coin_does_not_travel()
      {
         string prompt = LetterPromptBuilder.BuildPlayerLetterReplyDecisionMessage(
            Npc(), "Here are the denars you asked for.", "Gabriel");

         prompt.Should().Contain("Coin and goods do not travel with a letter.");
      }
   }
}

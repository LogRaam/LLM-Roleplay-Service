// Code written by Gabriel Mailhot, 13/10/2026.
// What this pins: CR, not the server, decides what a request to a local model leaves out when it would not fit.
//
// fkasad (Nexus, 13/10/2026), on LM Studio: "a long discussion ending with a 'Give something' is not working. The
// context lost the rules needed for a 'give'." A request bigger than the server's window is cut by the server's own
// overflow policy, and "truncate middle" cuts the middle of CR's prompt, where the action rules are. Gabriel
// (13/10/2026): CR trims it itself, the oldest lines of the conversation first, and never the action rules. If these
// fail, the server goes back to cutting blind, or a conversation loses its recap and its latest lines first.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ContextFitPolicyTests
   {
      // A conversation, oldest first, each line 700 characters (200 tokens at 3.5): player, character, player...
      private static List<ContextFitMessage> Conversation(int count)
         => Enumerable.Range(0, count).Select(i => new ContextFitMessage {IsUser = i % 2 == 0, Chars = 700}).ToList();

      // A request that fits is sent exactly as built: nothing the player wrote is lost for nothing.
      [Test]
      public void GIVEN_a_request_inside_the_window_WHEN_fitted_THEN_nothing_is_left_out()
      {
         ContextFit fit = ContextFitPolicy.Fit(new ContextFitFacts {
            WindowTokens = 32000, PromptChars = 35000, ReplyTokens = 3000, Messages = Conversation(9)
         });

         fit.Drop.Should().Be(0);
         fit.Fits.Should().BeTrue();
      }

      // THE REPORTED CASE: too big for the window, so the oldest raw lines go, and the recap (pinned) stays.
      [Test]
      public void GIVEN_a_request_too_big_WHEN_fitted_THEN_the_oldest_lines_after_the_recap_are_left_out_until_it_fits()
      {
         var messages = new List<ContextFitMessage> {new() {IsUser = true, Chars = 1400}}; // the running recap
         messages.AddRange(Conversation(13));

         ContextFit fit = ContextFitPolicy.Fit(new ContextFitFacts {
            WindowTokens = 16000, PromptChars = 30000, ReplyTokens = 3000, Messages = messages, PinnedLeading = 1,
            KeepAtLeast = ContextFitPolicy.KeepWithFullPrompt
         });

         fit.Fits.Should().BeTrue();
         fit.Drop.Should().BeGreaterThan(0);
         fit.EstimatedTokens.Should().BeLessThanOrEqualTo(fit.UsableTokens);
      }

      // The conversation must open on a player's line once lines are left out: several local chat templates refuse a
      // conversation that starts on the character's turn, and the player would get no reply at all.
      [Test]
      public void GIVEN_lines_left_out_WHEN_the_cut_would_open_on_the_characters_turn_THEN_it_opens_on_the_players()
      {
         ContextFit fit = ContextFitPolicy.Fit(new ContextFitFacts {
            WindowTokens = 16000, PromptChars = 40000, ReplyTokens = 3000, Messages = Conversation(13)
         });

         fit.Drop.Should().BeGreaterThan(0);
         (fit.Drop % 2).Should().Be(0, "message {0} after the cut is the character's", fit.Drop);
      }

      // The latest lines are what the reply answers: when even they do not fit, the fit says so (and the caller turns
      // to the compact prompt) rather than cutting the player's own words.
      [Test]
      public void GIVEN_a_prompt_too_big_for_the_window_WHEN_fitted_THEN_the_latest_lines_stay_and_it_reports_no_fit()
      {
         ContextFit fit = ContextFitPolicy.Fit(new ContextFitFacts {
            WindowTokens = 8192, PromptChars = 70000, ReplyTokens = 3000, Messages = Conversation(9),
            KeepAtLeast = ContextFitPolicy.KeepWithFullPrompt
         });

         fit.Drop.Should().BeLessThanOrEqualTo(9 - ContextFitPolicy.KeepWithFullPrompt + 1);
         fit.Fits.Should().BeFalse();
      }

      // The estimate leans to more tokens: a margin is kept, so an estimate a little short still lands inside.
      [Test]
      public void GIVEN_a_window_WHEN_fitted_THEN_CR_plans_to_fill_only_part_of_it()
      {
         ContextFitPolicy.Fit(new ContextFitFacts {WindowTokens = 10000}).UsableTokens.Should().Be(9000);
      }

      // The tokens the server reports are the truth: the next estimate uses them, within what text can be.
      [Test]
      public void GIVEN_a_reply_the_server_counted_WHEN_measured_THEN_the_next_estimate_uses_its_ratio_within_bounds()
      {
         ContextFitPolicy.Measured(30000, 10000).Should().Be(3.0);
         ContextFitPolicy.Measured(30000, 1000).Should().Be(6.0);
         ContextFitPolicy.Measured(30000, 0).Should().BeNull();
      }

      // The client's retry of a cut reply doubles the reply budget: it is only tried when that still fits the window.
      [Test]
      public void GIVEN_a_request_near_the_edge_WHEN_a_cut_reply_would_be_retried_THEN_the_retry_only_runs_if_it_fits()
      {
         var roomy = new ContextFit {EstimatedTokens = 5000, UsableTokens = 9000};
         var tight = new ContextFit {EstimatedTokens = 8000, UsableTokens = 9000};

         ContextFitPolicy.RetryFits(roomy, 3000).Should().BeTrue();
         ContextFitPolicy.RetryFits(tight, 3000).Should().BeFalse();
      }
   }
}

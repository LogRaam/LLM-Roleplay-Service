// Code written by Gabriel Mailhot, 13/10/2026.
// What this pins: the request CR actually sends to a local model with a declared context window, built the way the
// game builds it (the real prompt builder, the whole action vocabulary, the narrative payloads: LeanRealPromptBudgetTests).
//
// fkasad (Nexus, 13/10/2026): "a long discussion ending with a 'Give something' is not working. The context lost the
// rules needed for a 'give'." LM Studio cut CR's oversized request in its middle, where the action rules are. Gabriel
// (13/10/2026): CR fits the request itself. If these fail, a local player's request is too big again and the server
// cuts it blind, or CR cuts what it must keep: the action rules, the recap, the player's new line.

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ContextFitRequestTests
   {
      private const string NewLine = "And what will you give me for the sword?";

      private static string Line(int chars) => new StringBuilder().Insert(0, "We spoke of the road and the price of grain. ", chars / 45 + 1).ToString().Substring(0, chars);

      // A long conversation: the running recap, then twenty raw lines, player first.
      private static ChatSession LongConversation()
      {
         var session = new ChatSession();
         session.AddRunningSummary("Earlier in this conversation", Line(1200));
         for (var i = 0; i < 20; i++)
            if (i % 2 == 0) session.AddPlayerMessage(Line(700));
            else session.AddNpcMessage(Line(700));

         return session;
      }

      private static async Task<LlmRequest> Send(int window, ChatSession session, LeanPromptLevel level = LeanPromptLevel.Full)
      {
         var client = new CapturingClient();
         var service = new NpcChatService(client, new SectionResponseParser(), LeanRealPromptBudgetTests.RealBuilder()) {
            ContextWindowProvider = () => window
         };
         await service.ChatAsync(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, session, NewLine, LeanRealPromptBudgetTests.Encounter(level));

         return client.Last;
      }

      private static int Estimate(LlmRequest r)
         => (int) System.Math.Ceiling((r.SystemPrompt.Length + r.Messages.Sum(m => m.Content.Length)) / ContextFitPolicy.DefaultCharsPerToken
                                      + r.Messages.Count * ContextFitPolicy.PerMessageTokens + r.Parameters.MaxTokens);

      // THE REPORTED CASE, on a common local window: what goes out fits, still teaches the actions, keeps the recap and
      // the player's new line, and opens the raw history on a player's turn.
      [Test]
      public async Task GIVEN_a_long_conversation_and_a_16k_local_window_WHEN_sent_THEN_the_request_fits_and_keeps_what_it_must()
      {
         LlmRequest r = await Send(16384, LongConversation());

         Estimate(r).Should().BeLessThanOrEqualTo((int) (16384 * ContextFitPolicy.UsableShare));
         r.SystemPrompt.Should().Contain("GAME ACTIONS:");
         r.Messages.First().Content.Should().Contain("Earlier in this conversation");
         r.Messages[1].Role.Should().Be(MessageRole.User);
         r.Messages.Last().Content.Should().Be(NewLine);
      }

      // The cheapest loss first: when leaving out old lines is enough, the full prompt goes, unchanged.
      [Test]
      public async Task GIVEN_a_window_old_lines_alone_can_satisfy_WHEN_sent_THEN_the_full_prompt_goes_with_fewer_lines()
      {
         string full = LeanRealPromptBudgetTests.RealBuilder().BuildSystemPrompt(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, LeanRealPromptBudgetTests.Encounter(LeanPromptLevel.Full));
         // Room for the full prompt, its reply, the recap and the six latest lines, and not for all twenty-two.
         double tokens = full.Length / ContextFitPolicy.DefaultCharsPerToken + 3000 + 8 * (900 / ContextFitPolicy.DefaultCharsPerToken + ContextFitPolicy.PerMessageTokens);
         var window = (int) (tokens / ContextFitPolicy.UsableShare);

         ChatSession session = LongConversation();
         LlmRequest r = await Send(window, session);

         r.SystemPrompt.Should().Be(full);
         r.Messages.Count.Should().BeLessThan(session.Messages.Count);
      }

      // A window too small for the full prompt with its latest lines: the compact prompt and its smaller reply go, still
      // teaching the actions.
      [Test]
      public async Task GIVEN_an_8k_local_window_WHEN_sent_THEN_the_compact_prompt_goes_and_still_teaches_the_actions()
      {
         LlmRequest r = await Send(8192, LongConversation());

         r.Parameters.MaxTokens.Should().BeLessThanOrEqualTo(LeanReplyBudget.Tokens);
         r.SystemPrompt.Should().Contain("GAME ACTIONS:");
         r.Messages.Last().Content.Should().Be(NewLine);
      }

      // No window declared (a cloud model): the request goes exactly as built, as it always has.
      [Test]
      public async Task GIVEN_no_window_declared_WHEN_sent_THEN_the_whole_conversation_goes_as_built()
      {
         ChatSession session = LongConversation();
         LlmRequest r = await Send(0, session);

         r.Messages.Count.Should().Be(session.Messages.Count);
         r.Parameters.MaxTokens.Should().Be(3000);
      }

      // The host is told what CR did, so it can log it and tell the player once.
      [Test]
      public async Task GIVEN_a_fitted_request_WHEN_sent_THEN_the_host_is_told_what_was_done()
      {
         var reports = new List<ContextFitReport>();
         void Record(ContextFitReport report) => reports.Add(report);
         NpcChatService.ContextFitted += Record;
         try { await Send(8192, LongConversation()); }
         finally { NpcChatService.ContextFitted -= Record; }

         reports.Should().ContainSingle(x => x.WindowTokens == 8192 && x.SwitchedToCompact);
      }

      #region stubs

      private sealed class CapturingClient : ILlmClient
      {
         public LlmRequest Last { get; private set; } = null!;

         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
         {
            Last = request;

            return Task.FromResult(new LlmResponse {Content = "[DIALOGUE]\nAye.", IsSuccess = true});
         }
      }

      #endregion
   }
}

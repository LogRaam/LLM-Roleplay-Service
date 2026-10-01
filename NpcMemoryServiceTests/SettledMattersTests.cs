// Code written by Gabriel Mailhot, 01/10/2026.
// Lordfadooboo (Nexus, 30/09/2026, on 2.6.9): "sometimes even when a particular topic in conversation was brought up
// and settled, they bring it up again as if it wasnt settled. (a character wanted me to call them by a specific name and
// they kept requesting to be called that name even when i hadnt said anything concerning their name.)"
//
// Two holes, both traced on 01/10/2026. In a long conversation the older lines are folded into a recap by
// ConversationHistoryCompressor, and the turn where a matter was settled leaves the model's verbatim view; the recap
// was told to keep who asked what, but never to say what was SETTLED, so "she asked to be called Mira" survived and
// "he agreed" did not. And the prompt's only anti-repetition rule was about wording ("never begin two replies the same
// way"), never about a matter already asked and answered. If these rules are wrong, a character keeps re-asking for
// what was granted, re-opening what was closed, however often the player answers.

#region

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.Compression;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class SettledMattersTests
   {
      // The recap is all the model has of the folded turns: it must carry what was settled, as settled.
      [Test]
      public async Task GIVEN_older_lines_folded_into_a_recap_WHEN_the_recap_is_asked_for_THEN_it_must_say_what_was_settled()
      {
         var client = new CapturingLlmClient();

         await new ConversationHistoryCompressor(client).CompressAsync("", "Mira: Call me Mira.\nPlayer: Mira it is.");

         client.LastRequest!.SystemPrompt.Should().Contain("Settled:");
         client.LastRequest.SystemPrompt.Should().Contain("not raised again");
      }

      // The character is told, in every conversation and at every prompt size, that a matter asked and answered stays
      // closed unless the player reopens it. Lean included: the smaller the model, the likelier it is to circle back.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_any_conversation_WHEN_the_prompt_is_built_THEN_a_settled_matter_is_not_raised_again(LeanPromptLevel level)
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = level});

         prompt.Should().Contain("What was asked and answered in this conversation stays settled");
      }

      private static NpcProfile Npc() => new NpcProfile {Id = "lord_mira", Name = "Mira", Clan = "Fen", Faction = "Battania"};

      private sealed class CapturingLlmClient : ILlmClient
      {
         public LlmRequest? LastRequest { get; private set; }

         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
         {
            LastRequest = request;

            return Task.FromResult(new LlmResponse {Content = "Mira asked to be called Mira. Settled: the player agreed.", IsSuccess = true});
         }
      }
   }
}

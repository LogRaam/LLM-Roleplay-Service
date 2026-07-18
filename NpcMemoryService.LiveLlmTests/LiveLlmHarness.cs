// Code written by Gabriel Mailhot, 17/07/2026.
// Etage C harness: the shared plumbing for the FIREWALLED live-LLM behaviour tests. These verify what the
// deterministic prompt/policy tests cannot: that the model, given a correctly-built prompt, actually BEHAVES (uses
// a memory, presses a demand, hides a secret). They call a real LLM, so [SetUp] refuses to run any of them unless
// CR_RUN_LIVE_LLM=1 is set: no opt-in means Assert.Ignore before a single token is spent, whatever launched them.
// A small LLM-as-judge turns a fuzzy behaviour question into a YES/NO the test can assert.

#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   /// <summary>
   ///   Base class for live-LLM behaviour tests. Every derived test self-skips unless the opt-in env var is set,
   ///   so the suite is inert under nCrunch / a default <c>dotnet test</c> and can never quietly burn tokens.
   /// </summary>
   [Category("LiveLlm")]
   public abstract class LiveLlmHarness
   {
      private HttpClient? _http;

      /// <summary>The raw client, for the LLM-as-judge and any direct completions.</summary>
      protected ILlmClient Client { get; private set; } = null!;

      // Runs before EVERY test. The single, config-proof firewall: without the explicit opt-in there is no client
      // and no test body runs, so no token is ever spent by accident.
      [SetUp]
      public void RequireOptIn()
      {
         if (Environment.GetEnvironmentVariable("CR_RUN_LIVE_LLM") != "1")
            Assert.Ignore("Live-LLM tests are opt-in: set CR_RUN_LIVE_LLM=1 to run them (they call a real LLM and spend tokens).");

         string? key = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
         if (string.IsNullOrWhiteSpace(key))
            Assert.Ignore("OPENROUTER_API_KEY is not set; cannot reach the LLM.");

         var config = new OpenRouterConfig {
            ApiKey = key!,
            Model = Environment.GetEnvironmentVariable("CR_LIVE_LLM_MODEL") ?? "x-ai/grok-4.20"
         };
         _http = new HttpClient();
         Client = new OpenRouterClient(_http, config);
      }

      [TearDown]
      public void DisposeHttp() => _http?.Dispose();

      /// <summary>
      ///   Runs one NPC chat turn end to end (real LLM) at the given content level, returning the parsed reply.
      ///   A fresh <see cref="ChatSession" /> per call keeps each test independent.
      /// </summary>
      protected async Task<NpcChatResult> ChatOnce(NpcProfile npc,
                                                   string playerMessage,
                                                   EncounterContext? context = null,
                                                   AdultContentLevel adultLevel = AdultContentLevel.Off,
                                                   int currentDay = 10)
      {
         var service = new NpcChatService(Client, new SectionResponseParser(), new PromptBuilder {AdultLevel = adultLevel});

         return await service.ChatAsync(npc, new WorldState {CurrentDay = currentDay}, new ChatSession(), playerMessage, context);
      }

      /// <summary>
      ///   LLM-as-judge: asks the model, strictly, whether <paramref name="transcript" /> satisfies
      ///   <paramref name="criterion" />, and returns the YES/NO verdict. Deliberately literal and low-variance so
      ///   a genuine behaviour is scored, not the judge's mood.
      /// </summary>
      protected async Task<bool> JudgeYes(string criterion, string transcript)
      {
         var request = new LlmRequest {
            SystemPrompt = "You are a strict, literal test judge. You answer with a single word only: YES or NO.",
            Messages = new List<LlmMessage> {
               new(MessageRole.User,
                  $"CRITERION: {criterion}\n\nTRANSCRIPT:\n{transcript}\n\nDoes the transcript satisfy the criterion? Answer YES or NO only.")
            }
         };

         LlmResponse response = await Client.CompleteAsync(request);

         return response.IsSuccess && response.Content.TrimStart().StartsWith("YES", StringComparison.OrdinalIgnoreCase);
      }

      /// <summary>A minimal valid NPC to speak with.</summary>
      protected static NpcProfile Npc() => new() {
         Id = "npc_live_test",
         Name = "Raganvad",
         Faction = "Sturgia",
         Clan = "Vagiroving"
      };
   }
}

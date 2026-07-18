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
   ///   <para>
   ///     EVERY derived fixture must ALSO carry <c>[Explicit]</c>. The <c>[SetUp]</c> guard below is a RUNTIME
   ///     firewall: it fires only once a test has been built, instrumented and started, which costs no tokens
   ///     but does cost nCrunch a cycle on every file save now that this project sits in the solution.
   ///     <c>[Explicit]</c> stops the test being selected by an automatic sweep in the first place. The
   ///     attribute is repeated per fixture rather than inherited from here on purpose: NUnit's inheritance
   ///     rules for it are not something to leave to assumption when the cost of being wrong is a silent
   ///     auto-run against a paid API.
   ///   </para>
   /// </summary>
   [Category("LiveLlm")]
   public abstract class LiveLlmHarness
   {
      private string? _apiKey;
      private HttpClient? _http;
      private HttpClient? _judgeHttp;

      /// <summary>The raw client, for the LLM-as-judge and any direct completions.</summary>
      protected ILlmClient Client { get; private set; } = null!;

      // Runs before EVERY test. The single, config-proof firewall: without the explicit opt-in there is no client
      // and no test body runs, so no token is ever spent by accident.
      /// <summary>The loaded local settings, so a test can report or vary what it was run with.</summary>
      protected LiveLlmSettings Settings { get; private set; } = new();

      [SetUp]
      public void RequireOptIn()
      {
         Settings = LiveLlmSettings.Load(out string source);

         // Two ways in, and BOTH are deliberate. The file is for a developer at a keyboard: it takes effect at
         // once, with no restart, which the environment variable cannot do because a process captures its
         // environment at start-up. The environment variable is for CI and for anywhere no file should exist.
         bool allowed = Settings.Enabled || Environment.GetEnvironmentVariable("CR_RUN_LIVE_LLM") == "1";

         if (!allowed)
            Assert.Ignore($"Live-LLM tests are OFF (they call a real LLM and spend tokens). "
                        + $"To run them: copy {LiveLlmSettings.ExampleFileName} to {LiveLlmSettings.FileName} in the "
                        + $"test project and set \"enabled\": true. It applies immediately, no restart needed. "
                        + $"(CI alternative: CR_RUN_LIVE_LLM=1, which must be set BEFORE the runner starts.) "
                        + $"Settings read from: {source}");

         // Settings file first, environment second. The file wins because it is the one a developer can change
         // and see take effect immediately; the variable stays for CI and for anyone who prefers it.
         string? key = NullIfBlank(Settings.ApiKey) ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

         if (string.IsNullOrWhiteSpace(key))
            Assert.Ignore($"No API key. Put one in \"apiKey\" in {LiveLlmSettings.FileName} (git-ignored), or set "
                        + "OPENROUTER_API_KEY before starting your runner.");

         string model = !string.IsNullOrWhiteSpace(Settings.Model)
            ? Settings.Model
            : "x-ai/grok-4.20";

         _apiKey = key;

         var config = new OpenRouterConfig {
            ApiKey = key!,
            Model = model,
            ReasoningProvider = () => NullIfBlank(Settings.ReasoningEffort),
            ProviderSlugsProvider = () => NullIfBlank(Settings.ProviderPin),
            AllowProviderFallbacksProvider = () => Settings.AllowProviderFallbacks
         };

         _http = new HttpClient();
         Client = new OpenRouterClient(_http, config);

         // Which key, and from where, WITHOUT ever printing it. A deleted key in the environment variable while
         // a valid one sat in the game's own options is precisely how an afternoon gets lost to a 401.
         string keySource = NullIfBlank(Settings.ApiKey) != null
            ? LiveLlmSettings.FileName
            : "OPENROUTER_API_KEY";

         TestContext.Out.WriteLine($"[LIVE LLM] key={Fingerprint(key!)} from {keySource}");
         TestContext.Out.WriteLine($"[LIVE LLM] model={model}, judge={JudgeModelId}, "
                                 + $"reasoning={NullIfBlank(Settings.ReasoningEffort) ?? "(default)"}, "
                                 + $"providerPin={NullIfBlank(Settings.ProviderPin) ?? "(none)"}, "
                                 + $"maxTokens={Settings.MaxTokens}, temperature={Settings.Temperature}, "
                                 + $"presencePenalty={Settings.PresencePenalty} | settings: {source}");
      }

      /// <summary>
      ///   The client the JUDGE speaks through. Reuses the main client unless a separate judge model is
      ///   configured, in which case the verdict must not be scored by the model on trial's own settings.
      /// </summary>
      private ILlmClient JudgeClient()
      {
         if (string.IsNullOrWhiteSpace(Settings.JudgeModel) || Settings.JudgeModel == Settings.Model) return Client;

         _judgeHttp ??= new HttpClient();

         return new OpenRouterClient(_judgeHttp, new OpenRouterConfig {
            ApiKey = _apiKey!,
            Model = Settings.JudgeModel!
         });
      }

      /// <summary>The judge's model: its own if configured, otherwise the model under test.</summary>
      private string JudgeModelId => !string.IsNullOrWhiteSpace(Settings.JudgeModel)
         ? Settings.JudgeModel!
         : Settings.Model;

      private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

      /// <summary>
      ///   Enough of a key to tell WHICH one is in play, never enough to use it. Prints the same
      ///   head-and-tail shape OpenRouter's own dashboard shows, so a key can be matched against the account
      ///   page at a glance: that comparison is what identified a deleted key here.
      /// </summary>
      private static string Fingerprint(string key)
         => key.Length <= 16
            ? "(too short to be a key)"
            : $"{key.Substring(0, 12)}...{key.Substring(key.Length - 3)}";

      [TearDown]
      public void DisposeHttp()
      {
         _http?.Dispose();
         _judgeHttp?.Dispose();
         _http = null;
         _judgeHttp = null;
      }

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
         // The generation settings come from the local file, so what this lane measures is the shape the game
         // actually ships (or whatever variation is being swept), not a second set of defaults living here.
         var service = new NpcChatService(Client, new SectionResponseParser(), new PromptBuilder {AdultLevel = adultLevel}) {
            ChatParameters = new LlmParameters {
               MaxTokens = Settings.MaxTokens,
               Creativity = Settings.Temperature,
               PresencePenalty = Settings.PresencePenalty
            }
         };

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
            },
            // A verdict wants to be literal and repeatable, never creative: the judge runs cold and short
            // whatever the model under test is set to.
            Parameters = new LlmParameters {MaxTokens = 8, Creativity = 0f}
         };

         LlmResponse response = await JudgeClient().CompleteAsync(request);

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

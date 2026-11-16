// Code written by Gabriel Mailhot, 16/11/2026.
// Firewalled run of the Jev action bench (JevActionBench): the action bench's cases read by Jev, TypeSafe AI's decision
// model, through AIML API. Dutch (CHIM, Discord 16/11/2026) proposed it for the Enhanced composition's second call;
// Gabriel (16/11/2026): "prepare le banc Jev". A bench, not a gate: it asserts the run reached Jev for every case and
// prints the scoreboard; a second test runs the current interpreter on the SAME cases, so both print side by side.
//
// Opt-in as the rest of the lane (livellm.settings.json "enabled": true, or CR_RUN_LIVE_LLM=1), plus an AIML API key
// ("aimlApiKey" in the git-ignored settings copy, or AIMLAPI_KEY). "jevCases": "subset" (5 cases) or "all" (the catalog).

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NUnit.Framework;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   [TestFixture]
   [Explicit("Calls Jev (AIML API) and spends credits. Run deliberately: enabled in livellm.settings.json plus an AIML API key.")]
   public sealed class JevActionBenchLiveLlmTests
   {
      // The interpreter smoke run's own subset (ActionBenchLiveLlmTests), so the two compare on the same few cases.
      private static readonly HashSet<string> SubsetIds = new() {
         "give_gold", "release_prisoner", "swear_oath", "give_gold_narrated_only", "give_gold_conditional_promise"
      };

      private LiveLlmSettings _settings = new();
      private string _aimlKey = "";
      private readonly HttpClient _http = new() {Timeout = TimeSpan.FromSeconds(30)};

      [OneTimeTearDown]
      public void Dispose() => _http.Dispose();

      [SetUp]
      public void RequireOptIn()
      {
         _settings = LiveLlmSettings.Load(out string source);
         if (!_settings.Enabled && Environment.GetEnvironmentVariable("CR_RUN_LIVE_LLM") != "1")
            Assert.Ignore($"Live-LLM tests are OFF. Copy {LiveLlmSettings.ExampleFileName} to {LiveLlmSettings.FileName} and set \"enabled\": true. Settings read from: {source}");

         _aimlKey = string.IsNullOrWhiteSpace(_settings.AimlApiKey) ? Environment.GetEnvironmentVariable("AIMLAPI_KEY") ?? "" : _settings.AimlApiKey!;
         if (string.IsNullOrWhiteSpace(_aimlKey))
            Assert.Ignore($"No AIML API key. Put one in \"aimlApiKey\" in {LiveLlmSettings.FileName} (git-ignored), or set AIMLAPI_KEY before starting your runner.");

         TestContext.Out.WriteLine($"[JEV] model={_settings.JevModel}, cases={_settings.JevCases}, key=...{_aimlKey.Substring(Math.Max(0, _aimlKey.Length - 4))} | settings: {source}");
      }

      [Test]
      public async Task GIVEN_the_action_bench_cases_WHEN_jev_reads_them_THEN_its_scoreboard_prints()
      {
         List<ActionBenchCase> cases = Cases();

         IReadOnlyList<JevBenchResult> results = await JevActionBench.RunAsync(_http, _aimlKey, _settings.JevModel, cases);

         TestContext.Out.WriteLine(JevActionBench.Format(results, _settings.JevModel));
         results.Count.Should().Be(cases.Count);
         results.Count(r => !r.CallSucceeded).Should().Be(0, "a run past the firewall should reach Jev for every case");
      }

      [Test]
      public async Task GIVEN_the_same_cases_WHEN_jev_and_the_interpreter_both_read_them_THEN_both_scoreboards_print()
      {
         string? openRouterKey = string.IsNullOrWhiteSpace(_settings.ApiKey) ? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") : _settings.ApiKey;
         if (string.IsNullOrWhiteSpace(openRouterKey)) Assert.Ignore("No OpenRouter key for the interpreter side (\"apiKey\" or OPENROUTER_API_KEY).");

         List<ActionBenchCase> cases = Cases();

         IReadOnlyList<JevBenchResult> jev = await JevActionBench.RunAsync(_http, _aimlKey, _settings.JevModel, cases);
         // The interpreter reads the same reasoning and provider settings as the rest of the live lane, so a run
         // can match the in-game Interpret setting (reasoning left blank = the provider's default, often thinking).
         string? reasoning = string.IsNullOrWhiteSpace(_settings.ReasoningEffort) ? null : _settings.ReasoningEffort;
         string? providerPin = string.IsNullOrWhiteSpace(_settings.ProviderPin) ? null : _settings.ProviderPin;
         var interpreter = new OpenRouterClient(_http, new OpenRouterConfig {
            ApiKey = openRouterKey!,
            Model = _settings.Model,
            ReasoningProvider = () => reasoning,
            ProviderSlugsProvider = () => providerPin,
            AllowProviderFallbacksProvider = () => _settings.AllowProviderFallbacks
         });
         var clock = System.Diagnostics.Stopwatch.StartNew();
         ActionBenchReport report = await ActionBenchRunner.RunAsync(interpreter, cases);
         clock.Stop();

         TestContext.Out.WriteLine(JevActionBench.Format(jev, _settings.JevModel));
         TestContext.Out.WriteLine($"INTERPRETER ({_settings.Model}, reasoning={reasoning ?? "(default)"}, providerPin={providerPin ?? "(none)"}), same cases, types AND parameters, "
                                   + $"{clock.Elapsed.TotalSeconds / Math.Max(1, cases.Count):0.0} s per case on average:");
         TestContext.Out.WriteLine(report.Format());
         jev.Count(r => !r.CallSucceeded).Should().Be(0);
         report.CallFailures.Should().Be(0);
      }

      private List<ActionBenchCase> Cases()
         => string.Equals(_settings.JevCases, "all", StringComparison.OrdinalIgnoreCase)
            ? ActionBenchCatalog.All.ToList()
            : ActionBenchCatalog.All.Where(c => SubsetIds.Contains(c.Id)).ToList();
   }
}

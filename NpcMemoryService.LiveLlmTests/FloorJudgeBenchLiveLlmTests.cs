// Code written by Gabriel Mailhot, 26/11/2026.
// Firewalled run of the floor judge bench (FloorJudgeBench). Gabriel (26/11/2026): "go" for the judge bench, after the
// TurnArbiter; DeepSeek V4.1 Flash first ("tres rapide, et peu couteux"), others by changing "floorJudgeModel". A bench,
// not a gate: it asserts every call came back and prints the scoreboard. The second test judges the same listener after
// each line of a growing scene and prints how much of each call the cache served (Gabriel: the history "devrait s'annuler
// en cout dans le CACHE").
//
// Opt-in as the rest of the lane: livellm.settings.json "enabled": true (or CR_RUN_LIVE_LLM=1) and an OpenRouter key.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   [TestFixture]
   [Explicit("Calls a real model through OpenRouter and spends tokens (a few cents). Run deliberately.")]
   public sealed class FloorJudgeBenchLiveLlmTests
   {
      private LiveLlmSettings _settings = new();
      private OpenRouterClient _client = null!;
      private readonly HttpClient _http = new() {Timeout = TimeSpan.FromSeconds(60)};

      [OneTimeTearDown]
      public void Dispose() => _http.Dispose();

      [SetUp]
      public void RequireOptIn()
      {
         _settings = LiveLlmSettings.Load(out string source);
         if (!_settings.Enabled && Environment.GetEnvironmentVariable("CR_RUN_LIVE_LLM") != "1")
            Assert.Ignore($"Live-LLM tests are OFF. Set \"enabled\": true in {LiveLlmSettings.FileName}. Settings read from: {source}");

         string? key = string.IsNullOrWhiteSpace(_settings.ApiKey) ? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") : _settings.ApiKey;
         if (string.IsNullOrWhiteSpace(key)) Assert.Ignore("No OpenRouter key (\"apiKey\" or OPENROUTER_API_KEY).");

         string reasoning = _settings.FloorJudgeReasoning;
         string? pin = string.IsNullOrWhiteSpace(_settings.ProviderPin) ? null : _settings.ProviderPin;
         _client = new OpenRouterClient(_http, new OpenRouterConfig {
            ApiKey = key!, Model = _settings.FloorJudgeModel, ReasoningProvider = () => reasoning, ProviderSlugsProvider = () => pin,
            AllowProviderFallbacksProvider = () => _settings.AllowProviderFallbacks
         });
         TestContext.Out.WriteLine($"[FLOOR JUDGE] model={_settings.FloorJudgeModel}, reasoning={reasoning}, providerPin={pin ?? "(none)"}");
      }

      [Test]
      public async Task GIVEN_the_validated_scenes_WHEN_the_judge_reads_each_listener_THEN_the_scoreboard_prints()
      {
         List<FloorJudgeCase> cases = FloorJudgeBench.Cases();
         // The listeners of one line are judged in parallel in play; here every case at once, as many as the bench holds.
         FloorJudgeResult[] results = await Task.WhenAll(cases.Select(c => FloorJudgeBench.JudgeAsync(_client, c)));

         TestContext.Out.WriteLine(FloorJudgeBench.Scoreboard(_settings.FloorJudgeModel, results));
         results.Count(r => r.Error != null).Should().Be(0, "every judge call must come back for the scoreboard to mean anything");
      }

      [Test]
      public async Task GIVEN_a_scene_growing_line_by_line_WHEN_one_listener_is_judged_after_each_THEN_the_cache_share_prints()
      {
         List<FloorJudgeLine> full = FloorJudgeBench.GrowingScene();
         var lines = new List<string> {$"CACHE ON A GROWING SCENE: {_settings.FloorJudgeModel}, listener Lucon"};
         for (var i = 1; i <= full.Count; i++)
         {
            FloorJudgeResult r = await FloorJudgeBench.JudgeAsync(_client, new FloorJudgeCase {
               Id = $"line_{i}", Listener = FloorJudgeBench.Cases().First(c => c.Listener.Name == "Lucon").Listener, Scene = full.Take(i).ToList()
            });
            int prompt = r.Usage?.PromptTokens ?? 0;
            int cached = r.Usage?.CachedPromptTokens ?? 0;
            lines.Add($"  after line {i}: prompt {prompt} tok, cached {cached} ({(prompt == 0 ? 0 : 100.0 * cached / prompt):0}%), {r.Millis} ms, ${r.Usage?.Cost ?? 0:0.000000}, intent={r.Verdict.Intent}{(r.Error != null ? " ERROR " + r.Error : "")}");
         }

         TestContext.Out.WriteLine(string.Join(Environment.NewLine, lines));
      }
   }
}

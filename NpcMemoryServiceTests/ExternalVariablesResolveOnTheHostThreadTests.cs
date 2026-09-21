// Code written by Gabriel Mailhot, 21/09/2026.
// What these pin: that building a prompt never calls a third-party variable provider, once the host has
// resolved them itself.
//
// tashmetu (Nexus, 21/09/2026), who could not check it from outside: "do CrPrompt.RegisterVariable callbacks
// always run on the main thread, synchronously, before the prompt is sent off? If there's any scenario where
// they run elsewhere, I'd rather have our one live-reading variable snapshot its data on the main thread first."
//
// They did not. The mod calls ChatAsync inside a Task.Run and the prompt, variables included, is built at the top
// of it, so a provider reading Hero.MainHero or Campaign.Current live was touching campaign objects from a
// thread-pool thread while the simulation ran. Bannerlord's campaign objects are not safe to read that way.
//
// The host now resolves them on its own thread as it builds the encounter context and hands the values over. The
// provider that throws in these tests stands in for a provider doing anything at all: if the builder still
// called it, the test would fail, and so would the thread guarantee.

#region

using System;
using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Extension;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class ExternalVariablesResolveOnTheHostThreadTests
   {
      [SetUp]
      public void SetUp() => PromptVariableRegistry.Clear();

      [TearDown]
      public void TearDown() => PromptVariableRegistry.Clear();

      // THE GUARANTEE: values supplied by the host are used, and no provider is invoked while the prompt builds.
      [Test]
      public void GIVEN_the_host_resolved_the_variables_WHEN_the_prompt_is_built_THEN_no_provider_is_ever_called()
      {
         PromptVariableRegistry.Register("bc_player", _ => throw new InvalidOperationException(
            "a provider must not run during prompt building once the host has resolved them"));

         string prompt = Build(new Dictionary<string, string> {["bc_player"] = "Marshal of the North"},
            station: "YOUR OFFICE: {{bc_player}}");

         prompt.Should().Contain("YOUR OFFICE: Marshal of the North");
      }

      // BACK-COMPAT: a host that has not moved its resolution earlier still gets its providers called, exactly as
      // before. Removing that would break every consumer of the API the day this shipped.
      [Test]
      public void GIVEN_no_host_resolved_values_WHEN_the_prompt_is_built_THEN_the_registry_is_used_as_before()
      {
         PromptVariableRegistry.Register("bc_player", _ => "Strategos of the eastern themes");

         Build(null, station: "YOUR OFFICE: {{bc_player}}")
            .Should().Contain("YOUR OFFICE: Strategos of the eastern themes");
      }

      // A bridge that registers nothing, and a host that resolved nothing, must still build a prompt.
      [Test]
      public void GIVEN_an_empty_resolved_map_WHEN_the_prompt_is_built_THEN_it_builds_and_the_token_stays_literal()
      {
         string prompt = Build(new Dictionary<string, string>(), station: "YOUR OFFICE: {{bc_player}}");

         prompt.Should().Contain("{{bc_player}}", "an unknown token stays literal, as it always has");
      }

      // THE COLLISION RULE SURVIVES THE MOVE: a third-party name can never shadow a core token, whichever side
      // resolved it.
      [Test]
      public void GIVEN_a_resolved_variable_named_like_a_core_token_WHEN_the_prompt_is_built_THEN_the_core_one_wins()
      {
         string prompt = Build(new Dictionary<string, string> {["user"] = "Impostor"}, station: "PLAYER: {{user}}");

         prompt.Should().Contain("PLAYER: Arthur").And.NotContain("PLAYER: Impostor");
      }

      #region private

      private static string Build(IReadOnlyDictionary<string, string>? resolved, string station)
         => new PromptBuilder {PlayerName = "Arthur"}.BuildSystemPrompt(
            new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"},
            new WorldState {CurrentDay = 12},
            new EncounterContext {
               LeanLevel = LeanPromptLevel.Full, Scene = SceneType.Keep,
               StationGuidelinesOverride = station, ExternalPromptVariables = resolved
            });

      #endregion
   }
}

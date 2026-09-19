// Code written by Gabriel Mailhot, 19/09/2026.
// What these pin: that a {{variable}} a modder writes into the behaviour guidelines reaches the model as its
// VALUE, the way it already did in every other prose file.
//
// tashmetu (Nexus, 19/09/2026) built a Bellum Civile bridge on CrPrompt.RegisterVariable and shipped two prose
// contributions through cr_patch. The one in post_history_instructions worked. The one in behavior_guidelines
// put "YOUR OFFICE AND YOUR CROWN: {{bc_office}} {{bc_crown}}" in front of the model as literal braces, because
// the style, the player description, the world and the post-history instructions were all expanded and the
// guidelines were not. Nothing about the API said so, and the only way to find out was to read a prompt dump.

#region

using FluentAssertions;
using NpcMemoryService.Core.Extension;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class GuidelineVariablesAreExpandedTests
   {
      [SetUp]
      public void SetUp() => PromptVariableRegistry.Clear();

      [TearDown]
      public void TearDown() => PromptVariableRegistry.Clear();

      // THE REPORTED CASE: a lord's own station text, which is where a per-character contribution lands.
      [Test]
      public void GIVEN_a_variable_in_a_characters_guidelines_WHEN_the_prompt_is_built_THEN_the_model_reads_its_value()
      {
         PromptVariableRegistry.Register("bc_office", _ => "You hold the Logothete's seat on the privy council.");

         string prompt = Build(LeanPromptLevel.Full, station: "YOUR OFFICE: {{bc_office}}");

         prompt.Should().Contain("YOUR OFFICE: You hold the Logothete's seat on the privy council.");
         prompt.Should().NotContain("{{bc_office}}", "a raw token is noise to the model and a bug to the modder");
      }

      // The session-wide guidelines take the same path when no character-specific text exists.
      [Test]
      public void GIVEN_a_variable_in_the_session_guidelines_WHEN_the_prompt_is_built_THEN_it_is_expanded_too()
      {
         PromptVariableRegistry.Register("bc_crown", _ => "The crown is barely obeyed.");

         string prompt = new PromptBuilder {BehaviorGuidelinesOverride = "CROWN: {{bc_crown}}"}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 12},
               new EncounterContext {LeanLevel = LeanPromptLevel.Full, Scene = SceneType.Keep});

         prompt.Should().Contain("CROWN: The crown is barely obeyed.");
      }

      // A SMALL MODEL IS NOT THE ONE THAT GETS THE RAW TOKEN. Lean has its own guideline path, and a fix to the
      // full one alone would leave exactly the players on local models reading braces.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_the_guidelines_carry_a_variable_THEN_it_is_expanded_there_as_well()
      {
         PromptVariableRegistry.Register("bc_office", _ => "Strategos of the eastern themes.");

         Build(LeanPromptLevel.Lean, station: "OFFICE: {{bc_office}}")
            .Should().Contain("OFFICE: Strategos of the eastern themes.").And.NotContain("{{bc_office}}");
      }

      #region private

      private static NpcProfile Npc() => new() {Id = "lord_x", Name = "Test Lord", Faction = "Empire", Clan = "Vizartos"};

      private static string Build(LeanPromptLevel lean, string station)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 12},
            new EncounterContext {LeanLevel = lean, Scene = SceneType.Keep, StationGuidelinesOverride = station});

      #endregion
   }
}

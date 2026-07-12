// Code written by Gabriel Mailhot, 11/07/2026.
// Regression guard (player report, v1.30.6): authored backstories stopped landing. The only code change
// in the whole backstory chain since v1.30.1 was the v1.30.4 "invent 2-3 SUBTLE quirks of your own"
// clause, which diluted a strong named persona (e.g. "Kratos style") into faint generic tics ("no hint of
// personality"). These tests lock the intended behaviour: a non-empty AuthoredBackstory reaches the system
// prompt in Full mode, the section commits to it forcefully, a named figure's FULL manner is adopted (only
// its world kept out), the quirk-invention is a strict fallback, and the section is dropped in Lean mode.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AuthoredBackstoryPromptTests
   {
      private const string Header = "BACKSTORY AND VOICE";
      private const string Backstory = "Speak like a battle-hardened warrior in the style of Kratos; BACKSTORY_TEST_MARKER.";

      private static NpcProfile Npc(string? backstory) => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         AuthoredBackstory = backstory
      };

      private static string BuildFull(string? backstory)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(backstory), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full});

      [Test]
      public void GIVEN_an_authored_backstory_WHEN_building_the_full_prompt_THEN_its_text_reaches_the_prompt()
      {
         string prompt = BuildFull(Backstory);

         prompt.Should().Contain(Header);
         prompt.Should().Contain(Backstory);
      }

      [Test]
      public void GIVEN_an_authored_backstory_WHEN_building_the_full_prompt_THEN_the_section_commits_to_it_forcefully()
      {
         string prompt = BuildFull(Backstory);

         // The anti-dilution guarantee: the persona must be unmistakable in every reply, not a faint flavour.
         prompt.Should().Contain("COMMIT to it");
         prompt.Should().Contain("every reply");
         prompt.Should().Contain("instantly recognisable");
      }

      [Test]
      public void GIVEN_a_backstory_naming_a_figure_WHEN_building_the_full_prompt_THEN_its_full_manner_is_adopted_but_its_world_is_kept_out()
      {
         string prompt = BuildFull(Backstory);

         prompt.Should().Contain("FULL manner");
         prompt.Should().Contain("import no names, places");
      }

      [Test]
      public void GIVEN_an_authored_backstory_WHEN_building_the_full_prompt_THEN_inventing_quirks_is_a_strict_fallback_not_a_dilution()
      {
         string prompt = BuildFull(Backstory);

         // Quirk invention only kicks in when the backstory itself spells out none.
         prompt.Should().Contain("ONLY if, it spells out no speech quirks");
         // The v1.30.4 regressive framing that watered strong personas down must not return.
         prompt.Should().NotContain("two or three subtle");
      }

      [Test]
      public void GIVEN_a_blank_or_missing_backstory_WHEN_building_the_prompt_THEN_the_section_is_absent()
      {
         BuildFull(null).Should().NotContain(Header);
         BuildFull("   ").Should().NotContain(Header);
      }

      [Test]
      public void GIVEN_lean_mode_WHEN_building_the_prompt_THEN_the_backstory_section_is_dropped()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(Backstory), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Lean});

         prompt.Should().NotContain(Header);
         prompt.Should().NotContain(Backstory);
      }
   }
}

// Code written by Gabriel Mailhot, 11/07/2026.
// Regression guard (player report, v1.30.6): authored backstories stopped landing. The only code change
// in the whole backstory chain since v1.30.1 was the v1.30.4 "invent 2-3 SUBTLE quirks of your own"
// clause, which diluted a strong named persona (e.g. "Kratos style") into faint generic tics ("no hint of
// personality"). These tests lock the intended behaviour: a non-empty AuthoredBackstory reaches the system
// prompt in Full mode, the section commits to it forcefully, a named figure's FULL manner is adopted (only
// its world kept out), no tic is invented and a written one surfaces only now and then (VonLemberg, 15/11/2026),
// and the section is dropped in Lean mode.

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

      // The baseline every other test in this file assumes: if the authored text itself stops reaching the
      // prompt, no amount of correct wording around it matters, and the player's written persona vanishes
      // outright rather than merely diluting.
      [Test]
      public void GIVEN_an_authored_backstory_WHEN_building_the_full_prompt_THEN_its_text_reaches_the_prompt()
      {
         string prompt = BuildFull(Backstory);

         prompt.Should().Contain(Header);
         prompt.Should().Contain(Backstory);
      }

      // Pins the exact reworded wording that fixed the v1.30.6 report: the section must COMMIT to the
      // persona, not merely mention it, or a strong named voice again reads as flat and generic.
      [Test]
      public void GIVEN_an_authored_backstory_WHEN_building_the_full_prompt_THEN_the_section_commits_to_it_forcefully()
      {
         string prompt = BuildFull(Backstory);

         // The anti-dilution guarantee: the persona must be unmistakable in every reply, not a faint flavour.
         prompt.Should().Contain("COMMIT to it");
         prompt.Should().Contain("unmistakably in every reply");
         // ...and it is carried by how the character thinks and answers, not by a signature phrase (VonLemberg, 15/11/2026).
         prompt.Should().Contain("in how you think, judge, react, jest and");
      }

      // STAKE: VonLemberg (Discord, 15/11/2026): "a character repeatedly says 'Hmm...', uses the same metaphor, addresses
      // the player with the same pet name ... the player notices the pattern rather than the personality." A quirk the
      // player wrote stays, but as a tendency that surfaces now and then, never a mark demanded in every line.
      [Test]
      public void GIVEN_a_backstory_with_a_speech_quirk_WHEN_building_the_full_prompt_THEN_the_quirk_is_an_occasional_tendency_not_a_mark_in_every_line()
      {
         string prompt = BuildFull(Backstory);

         prompt.Should().Contain("a tendency that surfaces now and");
         prompt.Should().Contain("never a mark to show in every line");
         prompt.Should().NotContain("instantly recognisable");
         prompt.Should().NotContain("use them consistently");
      }

      // STAKE: the same audit: "Preserve explicitly authored speech characteristics." A player who wrote that the
      // character ends every line with 'aye' asked for exactly that; the occasional rule must not overrule them.
      [Test]
      public void GIVEN_a_backstory_saying_a_habit_comes_every_time_WHEN_building_the_full_prompt_THEN_the_player_s_words_still_win()
      {
         BuildFull(Backstory).Should().Contain("(unless it says, in so many");
      }

      // A named figure (e.g. "Kratos") must lend the NPC their FULL manner and force of personality, not a
      // watered-down echo, while Calradia stays Calradia: no imported names, places or powers from the
      // figure's own world.
      [Test]
      public void GIVEN_a_backstory_naming_a_figure_WHEN_building_the_full_prompt_THEN_its_full_manner_is_adopted_but_its_world_is_kept_out()
      {
         string prompt = BuildFull(Backstory);

         prompt.Should().Contain("FULL manner");
         prompt.Should().Contain("import no names, places");
      }

      // The actual root cause of the v1.30.6 regression: the old "invent 2-3 subtle quirks of your own"
      // clause fired even when a strong persona was already authored, diluting it. Quirk invention must now
      // trigger ONLY when the backstory itself names none, and the regressive phrasing must never return.
      [Test]
      public void GIVEN_an_authored_backstory_WHEN_building_the_full_prompt_THEN_inventing_quirks_is_a_strict_fallback_not_a_dilution()
      {
         string prompt = BuildFull(Backstory);

         // With no quirk of its own, the backstory borrows only the habit under YOUR VOICE.
         prompt.Should().Contain("ONLY if, it spells out no speech quirks");
         // The v1.30.4 regressive framing that watered strong personas down must not return.
         prompt.Should().NotContain("two or three subtle");
         // STAKE: and no tic is invented at all (VonLemberg, 15/11/2026: "Remove the instruction to invent two or three
         // quirks when none are supplied"): an invented tic is exactly the groove WornPhrasePolicy then has to fight.
         prompt.Should().NotContain("invent two or three");
         prompt.Should().Contain("invent no tics of your own");
      }

      // A whitespace-only backstory must not render an empty, pointless section header; the section only
      // exists to carry real authored text.
      [Test]
      public void GIVEN_a_blank_or_missing_backstory_WHEN_building_the_prompt_THEN_the_section_is_absent()
      {
         BuildFull(null).Should().NotContain(Header);
         BuildFull("   ").Should().NotContain(Header);
      }

      // AuthoredBackstory is one of the heavy sections LeanPromptPolicy drops for small/local models; a long
      // authored backstory has no room in that short context budget.
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

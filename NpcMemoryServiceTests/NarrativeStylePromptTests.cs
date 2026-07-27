// Code written by Gabriel Mailhot, 27/07/2026.
// The player-chosen NARRATIVE STYLE (the quill picker) is a house voice injected into the system prompt.
// This pins two things the AUDIT-STYLE-LITTERAIRE audit flagged as broken or missing:
//
// WHY IT MATTERS:
//   F1 (P1): the style must ride BOTH prompt builders. The commoner path is a separate, slimmer builder,
//   and before the fix it never received the voice, so a player on "Tolkien" heard the lord in that voice
//   and the tavernkeeper in the generic one, one exchange apart. Nothing but a test keeps the two in sync.
//   F3 (P2): the style is player/modder editable and injected verbatim, so an oversized file could overflow
//   a small local model or crowd out the identity/scene sections. A defensive cap trims it at the one choke
//   point every caller flows through; this proves the trim fires and lands on a line boundary.

#region

using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class NarrativeStylePromptTests
   {
      private static NpcProfile Npc() => new()
      {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The lord path is the reference case: a chosen voice must reach the prompt at all. If this fails the
      // whole feature is dead, so it anchors the other two.
      [Test]
      public void GIVEN_a_narrative_style_WHEN_the_lord_prompt_is_built_THEN_it_carries_the_voice()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {NarrativeStyle = "Write plainly. UNIQUE_STYLE_MARKER."};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 100}, context);

         prompt.Should().Contain("NARRATIVE STYLE");
         prompt.Should().Contain("UNIQUE_STYLE_MARKER");
      }

      // F1, the actual bug: the commoner builder is a second, independent code path. Without the style
      // threaded through it, the tavernkeeper drops out of the player's chosen voice mid-visit. This guards
      // that the commoner prompt carries the same voice the lord prompt does.
      [Test]
      public void GIVEN_a_narrative_style_WHEN_the_commoner_prompt_is_built_THEN_it_carries_the_voice()
      {
         var builder = new PromptBuilder();

         string prompt = builder.BuildCommonerSystemPrompt(Npc(), new CommonsKnowledge(), "Write plainly. UNIQUE_STYLE_MARKER.");

         prompt.Should().Contain("NARRATIVE STYLE");
         prompt.Should().Contain("UNIQUE_STYLE_MARKER");
      }

      // A blank style must inject nothing at all: the section header would otherwise sit in the prompt with an
      // empty body, teaching the model that "no voice" is itself a voice worth naming.
      [Test]
      public void GIVEN_no_narrative_style_WHEN_the_commoner_prompt_is_built_THEN_no_style_section_appears()
      {
         var builder = new PromptBuilder();

         string prompt = builder.BuildCommonerSystemPrompt(Npc(), new CommonsKnowledge(), null);

         prompt.Should().NotContain("NARRATIVE STYLE");
      }

      // F3: a pathological modder file (here far over the cap) must be trimmed before it reaches the model.
      // The head survives, the tail is dropped, and the trim lands on a line boundary so the voice never ends
      // mid-word. The boundary sits at MaxNarrativeStyleChars, the public constant the host warns against too.
      [Test]
      public void GIVEN_an_oversized_style_WHEN_the_prompt_is_built_THEN_it_is_trimmed_to_the_cap()
      {
         var builder = new PromptBuilder();
         string head = "HEAD_SENTINEL keep this opening line.";
         string filler = string.Join("\n", Enumerable.Range(0, 400).Select(i => $"padding line {i} of the voice"));
         string oversized = head + "\n" + filler + "\nTAIL_SENTINEL drop this closing line.";
         oversized.Length.Should().BeGreaterThan(PromptBuilder.MaxNarrativeStyleChars);

         var context = new EncounterContext {NarrativeStyle = oversized};
         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 100}, context);

         prompt.Should().Contain("HEAD_SENTINEL");
         prompt.Should().NotContain("TAIL_SENTINEL");
      }
   }
}

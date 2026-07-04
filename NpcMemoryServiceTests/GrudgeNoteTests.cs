// Code written by Gabriel Mailhot, 03/07/2026. Tone arbiter coverage added 04/07/2026.
// Grudges pillar: EncounterContext.GrudgeNote is host-composed (the consumer's GrudgeNarrator) and
// rendered verbatim by PromptBuilder, mirroring the StanceConsequenceHint placement discipline exactly,
// unconditional (not Lean-gated), since a live grudge should colour even a small model's replies.
// RegardShadowNote is the tone arbiter's companion field: a short clause placed right after the CURRENT
// STANCE regard line, pointing at the grudge note below before the LLM even reaches it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GrudgeNoteTests
   {
      private const string Marker = "UNIQUE_GRUDGE_TEST_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      [Test]
      public void GIVEN_a_full_prompt_WHEN_a_grudge_note_is_supplied_THEN_it_is_rendered_verbatim()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, GrudgeNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      [Test]
      public void GIVEN_a_lean_prompt_WHEN_a_grudge_note_is_supplied_THEN_it_is_still_rendered()
      {
         // Unlike ExtraActionTeachings, a live grudge is not dropped for a small model — it is short,
         // per-NPC stable colour, not an extended verb set.
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, GrudgeNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      [Test]
      public void GIVEN_no_grudge_note_WHEN_building_a_prompt_THEN_nothing_extra_is_added()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, GrudgeNote = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Marker);
         promptBlank.Should().NotContain(Marker);
      }

      // ── RegardShadowNote: the tone arbiter's clause on the CURRENT STANCE regard line ───────────

      [Test]
      public void GIVEN_a_regard_shadow_note_WHEN_building_a_prompt_THEN_it_is_rendered_verbatim_after_current_stance()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, RegardShadowNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
         prompt.IndexOf("CURRENT STANCE", System.StringComparison.Ordinal)
            .Should().BeLessThan(prompt.IndexOf(Marker, System.StringComparison.Ordinal));
      }

      [Test]
      public void GIVEN_no_regard_shadow_note_WHEN_building_a_prompt_THEN_nothing_extra_is_added()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, RegardShadowNote = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Marker);
         promptBlank.Should().NotContain(Marker);
      }
   }
}

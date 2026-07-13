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

      // The host (GrudgeNarrator) already composed the knownness mirror (named openly vs concealed) and the
      // "no numbers" narrative framing; AppendGrudgeNote's job is only to PLACE that text. If it reformats or
      // drops any of it, a concealed grudge could leak its true cause, or the mechanic could bleed into the
      // NPC's own voice as a bare number instead of a story.
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

      // Whitespace must count as "no grudge", not as "a blank grudge to render": otherwise a stray "A
      // GRIEVANCE YOU NURSE" header with nothing under it would reach the LLM, reading as a non-sequitur
      // the model would have to invent a cause for.
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

      // Ordering is the whole point of the tone arbiter: it must land right after CURRENT STANCE so the LLM
      // is primed on the live grudge BEFORE it reaches the regard number, per the production doc "before the
      // LLM even reaches it, so a strong grievance is never read as an afterthought to stated warmth". If the
      // clause drifted before the stance line or vanished, a good regard score could read as uncomplicated
      // warmth even while a grudge should be muting it.
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

      // Same blank guard as the grudge note itself: no live grudge shadowing the regard means the CURRENT
      // STANCE line must render exactly as it always did, with no dangling tone clause.
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

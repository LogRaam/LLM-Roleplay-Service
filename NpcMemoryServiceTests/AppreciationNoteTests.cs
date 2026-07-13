// Code written by Gabriel Mailhot, 04/07/2026.
// Appreciations pillar: EncounterContext.AppreciationNote is host-composed (the consumer's
// AppreciationNarrator, tarnished by the holder's own dominant live grudge) and rendered verbatim by
// PromptBuilder, mirroring GrudgeNoteTests exactly, unconditional (not Lean-gated) since a lingering
// kindness should colour even a small model's replies. Placed right after AppendGrudgeNote.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AppreciationNoteTests
   {
      private const string Marker = "UNIQUE_APPRECIATION_TEST_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The host has already applied the TARNISH (a fresh live grudge souring a standing kindness, see
      // AppreciationPolicy.TarnishedMagnitude) before composing this note, so PromptBuilder's only job is to
      // place the text unchanged. Rewriting or dropping it here would show a kindness at full untarnished
      // warmth even when the NPC's own grudge should have soured it.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_an_appreciation_note_is_supplied_THEN_it_is_rendered_verbatim()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, AppreciationNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      // Mirrors GrudgeNoteTests' lean case: a lingering kindness is short, per-NPC stable colour, not an
      // extended verb set, so it is never dropped for a small model the way ExtraActionTeachings is.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_an_appreciation_note_is_supplied_THEN_it_is_still_rendered()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, AppreciationNote = Marker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Marker);
      }

      // Whitespace must count as "no kindness worth voicing", not a blank line under "A KINDNESS YOU
      // REMEMBER" that the model would have to fill in on its own.
      [Test]
      public void GIVEN_no_appreciation_note_WHEN_building_a_prompt_THEN_nothing_extra_is_added()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, AppreciationNote = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(Marker);
         promptBlank.Should().NotContain(Marker);
      }

      // Fixed order matters: the grievance colours the NPC first, the kindness second, so the "likes you but
      // resents you" texture (ROADMAP: grudge DOMINATES tone, warmth is the nuance, not the co-equal) reads
      // in the intended weight rather than the model latching onto whichever note it saw first.
      [Test]
      public void GIVEN_both_a_grudge_note_and_an_appreciation_note_WHEN_building_a_prompt_THEN_the_appreciation_renders_after_the_grudge()
      {
         const string grudgeMarker = "UNIQUE_GRUDGE_MARKER_FOR_ORDERING";
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full, GrudgeNote = grudgeMarker, AppreciationNote = Marker
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.IndexOf(grudgeMarker, System.StringComparison.Ordinal)
            .Should().BeLessThan(prompt.IndexOf(Marker, System.StringComparison.Ordinal));
      }
   }
}

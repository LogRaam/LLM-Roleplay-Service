// Code written by Gabriel Mailhot, 26/07/2026.
// STAKE (player report, 2026-07-26): a character's stored memories alternated between first and third
// person ("Nethor granted retirement..." vs "I told..."). Cause: the conversation RECAP summarizer is
// explicitly first-person, but the LLM-emitted [EVENT] block's summary: line carried no voice instruction,
// so the model free-chose per turn. Fix: both the Lean and Full [EVENT] teachings now lock the summary to
// the first person, matching the recap's voice.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class EventSummaryVoicePromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The Full-mode [EVENT] teaching is what a capable model actually reads on an ordinary (non-captive)
      // conversation. Without this line pinned first person, the model drifts to third person on some
      // turns (its own name, "he"/"she") while the recap stays first person, so the same NPC's stored
      // memories read as if narrated by two different voices.
      [Test]
      public void GIVEN_a_full_ordinary_conversation_WHEN_building_the_prompt_THEN_the_event_summary_teaching_locks_first_person()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("in the FIRST PERSON and past tense");
      }

      // The Lean teaching is what a small/local model reads; it must carry the same voice rule, or the
      // very models most prone to id drift (weaker instruction-following) are the ones left unconstrained.
      [Test]
      public void GIVEN_a_lean_ordinary_conversation_WHEN_building_the_prompt_THEN_the_event_summary_teaching_locks_first_person()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("in the first person (\"I ...\"), never your own name");
      }
   }
}

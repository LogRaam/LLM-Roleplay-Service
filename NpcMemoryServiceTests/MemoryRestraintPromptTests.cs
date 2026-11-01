// Code written by Gabriel Mailhot, 01/11/2026.
// What these pin: that the character's prompt asks for restraint with the past and with small things.
//
// kurushimi00 (Nexus, 01/11/2026): "It has a tendency to restate, almost in checklist fashion, things captured in
// memories. The same character, in every meeting almost, will marvel that I 'cleared the road of bandits'", and "It has a
// tendency to find something it can latch onto -- some symbolic gesture or item traded -- and elevate it beyond all
// proportion." The history block ended on "Reference them when relevant" with nothing against it, the [EVENT] rule named
// any "gift" as a memory, and the summary asked "why it mattered". Gabriel approved restraint for both prompt levels
// (01/11/2026, "go"). If these fail, the same instruction that produced the report is back, or a small model lost it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class MemoryRestraintPromptTests
   {
      // STAKE: the report. Told to reference a memory with nothing against it, a model recites the list at every meeting.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_a_character_with_memories_WHEN_the_prompt_is_built_THEN_they_are_told_the_past_is_not_a_list_to_go_through(LeanPromptLevel level)
      {
         string prompt = Build(level);

         prompt.Should().Contain("not a list to");
         prompt.Should().NotContain("Reference them when relevant");
      }

      // STAKE: restraint must not silence the mod's promise, that characters remember the player: a memory that serves the
      // moment stays welcome, and its date stays the given one.
      [Test]
      public void GIVEN_a_memory_that_serves_the_moment_WHEN_the_prompt_is_built_THEN_the_character_may_still_speak_of_it()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("Bring one up only when it serves this moment");
         prompt.Should().Contain("use the elapsed times given above");
      }

      // STAKE: the second half of the report, for a full prompt: a traded item made a symbol.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_it_is_built_THEN_a_small_gesture_stays_small()
      {
         Build(LeanPromptLevel.Full).Should().Contain("Keep PROPORTION");
      }

      // STAKE: Gabriel asked for both levels; a small model has the least room and the same habit.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_it_is_built_THEN_it_keeps_the_restraint_in_its_one_craft_line()
      {
         Build(LeanPromptLevel.Lean).Should().Contain("keep small things small");
      }

      // STAKE: a banal gift recorded as a memory is read back at every meeting for the rest of the campaign, which feeds
      // both habits the player reported.
      [Test]
      public void GIVEN_the_memory_rules_WHEN_the_prompt_is_built_THEN_a_small_gift_is_not_a_memory()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("a gift or payment of real significance");
         prompt.Should().Contain("A small gift, a");
         prompt.Should().NotContain("what happened and why it mattered");
      }

      // STAKE: in Enhanced the interpreter writes the memory, so the rule has to reach it or that mode keeps the old habit.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_the_interpreter_writes_the_memory_WHEN_its_prompt_is_built_THEN_a_small_gift_is_not_a_memory(LeanPromptLevel level)
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefixFor(level);

         prefix.Should().Contain("A small gift, courtesy or trade is no memory");
         prefix.Should().NotContain("why it mattered");
      }

      #region private

      private static string Build(LeanPromptLevel level)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};
         npc.Events.Add(new NotableEvent(10, NotableEventType.Collaboration, "I saw Arwa clear the road of bandits."));

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 12}, new EncounterContext {LeanLevel = level});
      }

      #endregion
   }
}

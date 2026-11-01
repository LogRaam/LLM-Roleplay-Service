// Code written by Gabriel Mailhot, 01/11/2026.
// What these pin: that a character can say which memory it spoke of, and reads it as already discussed from the next
// conversation on.
//
// kurushimi00 (Nexus, 01/11/2026): "The same character, in every meeting almost, will marvel that I 'cleared the road of
// bandits' like I was asked". Gabriel, same day: an NPC relating a memory marks it with a tag, the memory then reads as
// already discussed with the player, and each record has a short UID. If these fail, the character goes back to not knowing
// what it has said, a code reaches the player's screen, or the cached prompt breaks on the turn after every tag.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Memory;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NpcMemoryService.Core.Storage;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class MemoryRecallTests
   {
      private const string Road = "I saw Arwa clear the road of bandits.";
      private const string Ford = "I sheltered with Arwa at the ford.";

      // STAKE: every save already written is full of memories with no code; they are the ones a character keeps reciting.
      [Test]
      public void GIVEN_memories_from_an_old_save_WHEN_a_conversation_opens_THEN_each_gets_its_own_code()
      {
         NpcProfile npc = Npc(Road, Ford);

         MemoryRecall.AssignShortUids(npc);

         npc.Events.Select(e => e.ShortUid).Should().Equal("m1", "m2");
      }

      // STAKE: the code sits in the cached part of the prompt, and a mark waits on it: a code that moved would break the
      // cache and put a mark on the wrong memory.
      [Test]
      public void GIVEN_memories_with_codes_WHEN_a_new_memory_arrives_THEN_the_old_codes_do_not_move()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);
         npc.Events.Add(new NotableEvent(11, NotableEventType.Other, Ford));

         MemoryRecall.AssignShortUids(npc);

         npc.Events.Select(e => e.ShortUid).Should().Equal("m1", "m2");
      }

      // STAKE: compression drops memories from the middle of the list; a count would hand a survivor's number to a newcomer.
      [Test]
      public void GIVEN_a_memory_dropped_by_compression_WHEN_a_new_memory_arrives_THEN_it_takes_no_survivors_code()
      {
         NpcProfile npc = Npc(Road, Ford, "A third thing.");
         MemoryRecall.AssignShortUids(npc);
         npc.Events.RemoveAt(0);
         npc.Events.Add(new NotableEvent(12, NotableEventType.Other, "A fourth thing."));

         MemoryRecall.AssignShortUids(npc);

         npc.Events.Select(e => e.ShortUid).Should().OnlyHaveUniqueItems();
      }

      // STAKE: Gabriel's rule: the tag is what says a memory was spoken of.
      [Test]
      public void GIVEN_a_reply_with_the_tag_WHEN_it_is_applied_THEN_the_memory_is_marked_and_the_other_is_not()
      {
         NpcProfile npc = Npc(Road, Ford);
         MemoryRecall.AssignShortUids(npc);
         ParsedResponse reply = new SectionResponseParser().Parse("[DIALOGUE]The road is clear thanks to you.[/DIALOGUE]\n[DISCUSSED]\nmemories: m1\n[/DISCUSSED]");

         ProfileMutator.Apply(npc, reply, 20);

         npc.Events[0].DiscussedDay.Should().Be(20);
         npc.Events[1].DiscussedDay.Should().BeNull();
      }

      // STAKE: Gabriel's ruling on the adjustment: the mark is a field. A summary reaches witnesses, the Memories panel and
      // the compressor, and enrichment replaces it, so text added there would travel or vanish.
      [Test]
      public void GIVEN_a_marked_memory_WHEN_it_is_read_THEN_its_summary_is_unchanged()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);

         MemoryRecall.MarkDiscussed(npc, new[] {"m1"}, 20);

         npc.Events[0].summary.Should().Be(Road);
      }

      // STAKE: a model invents; an unknown code must neither fail the turn nor mark a memory.
      [Test]
      public void GIVEN_a_code_nobody_holds_WHEN_it_is_applied_THEN_nothing_is_marked()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);

         MemoryRecall.MarkDiscussed(npc, new[] {"m99"}, 20);

         npc.Events[0].DiscussedDay.Should().BeNull();
      }

      // STAKE: a player must never read a bookkeeping code in a character's mouth or in the narration.
      [TestCase("The road [m1] is clear.")]
      [TestCase("The road (m1) is clear.")]
      [TestCase("The road m1 is clear.")]
      public void GIVEN_a_code_written_in_the_reply_WHEN_it_is_shown_THEN_the_player_never_reads_it(string line)
      {
         ParsedResponse reply = new SectionResponseParser().Parse($"[DIALOGUE]{line}[/DIALOGUE]\n[NARRATION]{line}[/NARRATION]");

         reply.Dialogue.Should().Be("The road is clear.");
         reply.Narration.Should().Be("The road is clear.");
      }

      // STAKE: the tag must not leak to the screen either, and must be read however a model spaces it.
      [TestCase("memories: m1, m3")]
      [TestCase("m1 m3")]
      [TestCase("memories: M1,M3")]
      public void GIVEN_the_tag_WHEN_it_is_parsed_THEN_its_codes_are_read_and_it_stays_off_the_screen(string body)
      {
         ParsedResponse reply = new SectionResponseParser().Parse($"[DIALOGUE]Aye.[/DIALOGUE]\n[DISCUSSED]\n{body}\n[/DISCUSSED]");

         reply.Dialogue.Should().Be("Aye.");
         reply.DiscussedMemories.Should().Equal("m1", "m3");
      }

      // STAKE: without its code on the line the model has nothing to name.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_a_conversation_WHEN_the_prompt_is_built_THEN_the_memory_carries_its_code_and_the_tag_is_taught(LeanPromptLevel level)
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);

         string prompt = Build(npc, level, MemoryRecall.DiscussedSoFar(npc));

         prompt.Should().Contain(") m1: " + Road);
         prompt.Should().Contain("[DISCUSSED]");
      }

      // STAKE: a small model pays for every character (DuskSymphony's 8,192-token server), and the budget guards build no
      // conversation, so they cannot see this. Six memories is the compact window (LeanPromptPolicy).
      [Test]
      public void GIVEN_a_compact_conversation_WHEN_the_prompt_is_built_THEN_the_tag_costs_under_150_characters()
      {
         NpcProfile npc = Npc(Road, Ford, "A.", "B.", "C.", "D.");
         MemoryRecall.AssignShortUids(npc);

         int cost = Build(npc, LeanPromptLevel.Lean, MemoryRecall.DiscussedSoFar(npc)).Length - Build(npc, LeanPromptLevel.Lean, null).Length;

         cost.Should().BeLessThan(150);
      }

      // STAKE: the one-line form a compact model is taught must read like the block a full model writes.
      [Test]
      public void GIVEN_the_tag_written_on_one_line_WHEN_it_is_parsed_THEN_its_codes_are_read_and_it_stays_off_the_screen()
      {
         ParsedResponse reply = new SectionResponseParser().Parse("[DIALOGUE]Aye.[/DIALOGUE] [DISCUSSED]memories: m2[/DISCUSSED]");

         reply.Dialogue.Should().Be("Aye.");
         reply.DiscussedMemories.Should().Equal("m2");
      }

      // STAKE: the marks live in the save. If they do not come back from it, the character forgets at every reload what it
      // has said, and the report returns after the player's next session.
      [Test]
      public void GIVEN_a_marked_memory_WHEN_the_save_is_written_and_read_THEN_the_code_and_the_mark_survive()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);
         MemoryRecall.MarkDiscussed(npc, new[] {"m1"}, 20);

         string saved = ResilientProfileBundle.Serialize(new Dictionary<string, NpcProfile> {["n"] = npc});
         NotableEvent back = ResilientProfileBundle.Deserialize(saved)["n"].Events[0];

         back.ShortUid.Should().Be("m1");
         back.DiscussedDay.Should().Be(20);
      }

      // STAKE: Enhanced rests on this. The writer sees the codes and the interpreter never does, so the tag must be taught
      // to the writer whichever reader takes the actions, or every Enhanced player keeps the checklist.
      [TestCase(true)]
      [TestCase(false)]
      public void GIVEN_either_reader_of_the_actions_WHEN_the_prompt_is_built_THEN_the_writer_is_taught_the_tag(bool proseReadsItsOwnActions)
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);

         string prompt = new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 30}, new EncounterContext {
            LeanLevel = LeanPromptLevel.Full, ProseReadsItsOwnActions = proseReadsItsOwnActions, MemoriesDiscussedAtOpening = MemoryRecall.DiscussedSoFar(npc)
         });

         prompt.Should().Contain("[DISCUSSED]");
      }

      // STAKE: a letter is not a conversation and nothing reads a tag there; it must not be asked for one.
      [Test]
      public void GIVEN_a_prompt_outside_a_conversation_WHEN_it_is_built_THEN_no_code_and_no_tag_appear()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);

         string prompt = Build(npc, LeanPromptLevel.Full, null);

         prompt.Should().Contain("): " + Road);
         prompt.Should().NotContain("[DISCUSSED]");
      }

      // STAKE: the report. From the next meeting on, the character knows this one is said.
      [Test]
      public void GIVEN_a_memory_discussed_in_an_earlier_conversation_WHEN_the_prompt_is_built_THEN_it_reads_as_already_discussed()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);
         MemoryRecall.MarkDiscussed(npc, new[] {"m1"}, 20);

         string prompt = Build(npc, LeanPromptLevel.Full, MemoryRecall.DiscussedSoFar(npc));

         prompt.Should().Contain(Road + " [already discussed with Arwa]");
         prompt.Should().Contain("unless the player raises it");
      }

      // STAKE: a memory never spoken of must not read as settled, or the character falls silent on its own past, which is
      // the promise of the mod.
      [Test]
      public void GIVEN_a_memory_never_discussed_WHEN_the_prompt_is_built_THEN_it_reads_as_fresh()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);

         Build(npc, LeanPromptLevel.Full, MemoryRecall.DiscussedSoFar(npc)).Should().NotContain("[already discussed");
      }

      // STAKE: the cached prefix must survive the tag. A line that changed mid-conversation would discard the prefix on the
      // turn after every tag (fkasad, 15/09/2026, the same failure on another line), and the very next reply would be told
      // to leave alone what it is in the middle of discussing.
      [Test]
      public void GIVEN_a_memory_tagged_during_a_conversation_WHEN_the_next_turn_is_built_THEN_the_cached_prefix_is_unchanged()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);
         IReadOnlyCollection<string> atOpening = MemoryRecall.DiscussedSoFar(npc);
         string? before = PromptCacheSplit.StablePrefixOf(Build(npc, LeanPromptLevel.Full, atOpening));

         MemoryRecall.MarkDiscussed(npc, new[] {"m1"}, 20);

         PromptCacheSplit.StablePrefixOf(Build(npc, LeanPromptLevel.Full, atOpening)).Should().Be(before);
      }

      // STAKE: two conversations on one day are ordinary (the trap NotableEvent.Id records), so the day cannot decide this.
      [Test]
      public void GIVEN_a_second_conversation_on_the_same_day_WHEN_the_prompt_is_built_THEN_the_memory_reads_as_already_discussed()
      {
         NpcProfile npc = Npc(Road);
         MemoryRecall.AssignShortUids(npc);
         MemoryRecall.MarkDiscussed(npc, new[] {"m1"}, 30);

         Build(npc, LeanPromptLevel.Full, MemoryRecall.DiscussedSoFar(npc)).Should().Contain("[already discussed with Arwa]");
      }

      #region private

      private static NpcProfile Npc(params string[] summaries)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};
         foreach (string summary in summaries)
            npc.Events.Add(new NotableEvent(10, NotableEventType.Other, summary));

         return npc;
      }

      private static string Build(NpcProfile npc, LeanPromptLevel level, IReadOnlyCollection<string>? atOpening)
         => new PromptBuilder {PlayerName = "Arwa"}.BuildSystemPrompt(npc, new WorldState {CurrentDay = 30},
            new EncounterContext {LeanLevel = level, MemoriesDiscussedAtOpening = atOpening});

      #endregion
   }
}

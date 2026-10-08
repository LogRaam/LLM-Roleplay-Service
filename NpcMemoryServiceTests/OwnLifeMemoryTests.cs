// Code written by Gabriel Mailhot, 08/10/2026.
// A character's own life, apart from the player. tashmetu (Nexus, 08/10/2026) writes Regard and Standing, whose
// memories are almost all about other nobles: attachments, grudges, feuds, affairs ("I have learned that my wife...").
// Filed as Other, each one was shown under "YOUR HISTORY WITH THIS PLAYER", took one of the six places of the Compact
// prompt, and on a player succession sat under "Everything recorded below was your history with X". Gabriel
// (08/10/2026) ratified an OwnLife kind, kept apart like Hearsay, and an heir note that frames only the lived history.
// If this breaks, a lord's affair is read as something he lived with the player, or handed to the heir as their parent's.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class OwnLifeMemoryTests
   {
      private const string Wedding = "We were wed at Pravend, before the whole court.";
      private const string Affair = "I have learned that my wife has taken Grunwalder to her bed. I will not forgive it.";

      // tashmetu's Unthery: his wife's affair is his own life, shown apart from what he lived with the player.
      [Test]
      public void GIVEN_a_memory_of_the_characters_own_life_WHEN_the_prompt_is_built_THEN_it_is_apart_from_their_history_with_the_player()
      {
         string prompt = Build(LeanPromptLevel.Full, new NotableEvent(10, NotableEventType.Agreement, Wedding), new NotableEvent(11, NotableEventType.OwnLife, Affair));

         int own = prompt.IndexOf(HistoryWindowPolicy.OwnLifeHeader);
         own.Should().BeGreaterThan(prompt.IndexOf(Wedding));
         prompt.IndexOf(Affair).Should().BeGreaterThan(own);
      }

      // Compact keeps its lived memories however much of the character's own life was written after them.
      [Test]
      public void GIVEN_much_of_their_own_life_written_after_a_lived_memory_WHEN_the_compact_prompt_is_built_THEN_the_lived_memory_stays()
      {
         var events = new List<NotableEvent> {new(10, NotableEventType.Agreement, Wedding)};
         for (int i = 1; i <= 8; i++) events.Add(new NotableEvent(10 + i, NotableEventType.OwnLife, $"A matter of my own house, number {i}."));

         string prompt = Build(LeanPromptLevel.Lean, events.ToArray());

         prompt.Should().Contain(Wedding).And.Contain("number 8.").And.NotContain("number 6.");
      }

      // Compression shields the latest lived memories, never the character's own life for being recent.
      [Test]
      public void GIVEN_recent_memories_of_their_own_life_WHEN_compression_picks_what_to_keep_THEN_only_lived_ones_are_shielded()
      {
         var events = new List<NotableEvent> {new(10, NotableEventType.Collaboration, "Fought beside them.")};
         for (int i = 1; i <= 6; i++) events.Add(new NotableEvent(10 + i, NotableEventType.OwnLife, $"Own matter {i}."));

         HistoryWindowPolicy.LatestLived(events, 5).Should().Equal(0);
      }

      // The heir is told only the lived history was their parent's; what the character heard and lived apart is theirs.
      [Test]
      public void GIVEN_an_heir_WHEN_the_inherited_note_is_written_THEN_it_frames_only_the_history_with_the_predecessor()
      {
         var npc = new NpcProfile {Id = "n", Name = "Unthery", Faction = "Vlandia", Clan = "dey Meroc", InheritedFromName = "Old Can", InheritedKinship = "father"};
         npc.Events.Add(new NotableEvent(10, NotableEventType.Agreement, Wedding));
         npc.Events.Add(new NotableEvent(11, NotableEventType.OwnLife, Affair));

         string prompt = new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 20}, new EncounterContext {LeanLevel = LeanPromptLevel.Full});

         prompt.Should().NotContain("Everything recorded below was your history");
         prompt.Should().Contain("YOUR HISTORY WITH THIS PLAYER").And.Contain("Old Can");
         prompt.Should().Contain("your own, not that history");
      }

      // The kind is appended: every saved event keeps its meaning, and a mod reaches it by name.
      [Test]
      public void GIVEN_the_new_kind_WHEN_read_by_name_THEN_it_is_own_life()
      {
         System.Enum.TryParse("OwnLife", true, out NotableEventType kind).Should().BeTrue();
         kind.Should().Be(NotableEventType.OwnLife);
      }

      private static string Build(LeanPromptLevel level, params NotableEvent[] events)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};
         npc.Events.AddRange(events);

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 20}, new EncounterContext {LeanLevel = level});
      }
   }
}

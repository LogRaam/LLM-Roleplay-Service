// Code written by Gabriel Mailhot, 06/10/2026.
// What a character merely HEARD is not what they lived with the player. tashmetu (Nexus, 06/10/2026) is writing Regard
// and Standing, which gives a noble a memory each time a piece of news reaches them, about 25 a day across the world.
// Every memory sat in one list, and two things read that list by recency alone: the Compact prompt shows only the
// last six, and compression always keeps the last five. A noble who heard six pieces of news in a week would, on
// Compact, see none of what he lived with the player, and five fresh rumours would be kept while his marriage to the
// player was folded away. Gabriel (06/10/2026) ratified a Hearsay kind: it never takes the place of a lived memory,
// it is told apart as word that reached them, and it is the first thing compression may fold.

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
   public sealed class HearsayMemoryTests
   {
      private const string Wedding = "We were wed at Pravend, before the whole court.";

      // tashmetu's case on Compact: six rumours heard after the wedding. The wedding is still in the six, and only
      // the latest rumours are shown, apart, as word that reached them.
      [Test]
      public void GIVEN_many_rumours_heard_after_a_lived_memory_WHEN_the_compact_prompt_is_built_THEN_the_lived_memory_stays_in_view()
      {
         var events = new List<NotableEvent> {new(10, NotableEventType.Agreement, Wedding)};
         for (int i = 1; i <= 8; i++) events.Add(new NotableEvent(10 + i, NotableEventType.Hearsay, $"Heard rumour number {i}."));

         string prompt = Build(LeanPromptLevel.Lean, events);

         prompt.Should().Contain(Wedding);
         prompt.Should().Contain("Heard rumour number 8.").And.Contain("Heard rumour number 7.");
         prompt.Should().NotContain("Heard rumour number 6.", "Compact shows only the latest word heard, so its cost stays fixed");
      }

      // Told apart: a rumour is never offered to the model as something lived through with the player.
      [Test]
      public void GIVEN_a_rumour_heard_WHEN_the_prompt_is_built_THEN_it_sits_under_what_they_heard_not_their_history()
      {
         var events = new List<NotableEvent> {
            new(10, NotableEventType.Agreement, Wedding), new(11, NotableEventType.Hearsay, "Heard that Derthert lost Pravend.")
         };

         string prompt = Build(LeanPromptLevel.Full, events);

         int heard = prompt.IndexOf(HistoryWindowPolicy.HeardHeader);
         heard.Should().BeGreaterThan(prompt.IndexOf(Wedding));
         prompt.IndexOf("Heard that Derthert lost Pravend.").Should().BeGreaterThan(heard);
      }

      // A noble who has only heard of the player has still never met them: hearsay is no shared past.
      [Test]
      public void GIVEN_a_character_who_has_only_heard_of_the_player_WHEN_the_prompt_is_built_THEN_it_is_still_a_first_meeting()
      {
         var events = new List<NotableEvent> {new(11, NotableEventType.Hearsay, "Heard that the player took Pravend.")};

         string prompt = Build(LeanPromptLevel.Full, events);

         prompt.Should().Contain("You have never met this player before.");
         prompt.Should().Contain("Heard that the player took Pravend.");
      }

      // Full shows every memory, as before: hearsay included.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_many_rumours_were_heard_THEN_all_of_them_are_shown()
      {
         var events = new List<NotableEvent>();
         for (int i = 1; i <= 8; i++) events.Add(new NotableEvent(10 + i, NotableEventType.Hearsay, $"Heard rumour number {i}."));

         string prompt = Build(LeanPromptLevel.Full, events);

         prompt.Should().Contain("Heard rumour number 1.").And.Contain("Heard rumour number 8.");
      }

      // Compression keeps the five latest LIVED memories, not the five latest of anything: fresh rumours never shield
      // themselves at the cost of the wedding.
      [Test]
      public void GIVEN_fresh_rumours_WHEN_compression_picks_what_it_must_keep_THEN_the_latest_lived_memories_are_kept_and_the_rumours_are_not()
      {
         var events = new List<NotableEvent> {new(10, NotableEventType.Collaboration, "Fought beside them at Ortysia.")};
         for (int i = 1; i <= 6; i++) events.Add(new NotableEvent(10 + i, NotableEventType.Hearsay, $"Heard rumour number {i}."));

         IReadOnlyList<int> kept = HistoryWindowPolicy.LatestLived(events, 5);

         kept.Should().Equal(0);
      }

      // The kind is reachable by name, so a mod's "Hearsay" is filed as such and not as Other.
      [Test]
      public void GIVEN_a_mod_naming_the_kind_WHEN_parsed_THEN_it_is_hearsay()
      {
         System.Enum.TryParse("Hearsay", true, out NotableEventType kind).Should().BeTrue();
         kind.Should().Be(NotableEventType.Hearsay);
      }

      #region private

      private static string Build(LeanPromptLevel level, IEnumerable<NotableEvent> events)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};
         npc.Events.AddRange(events.ToList());

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 20}, new EncounterContext {LeanLevel = level});
      }

      #endregion
   }
}

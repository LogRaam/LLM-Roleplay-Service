// Code written by Gabriel Mailhot, 08/11/2026.
// What these pin: the player's cap on how many memories a full prompt shows.
//
// avalanche24 (Nexus, 07/11/2026): "Can we have an option to limit the memories to a certain number ... when I checked the
// local model, I noticed it taking too much tokens just analyzing each memory entry ... (they were reaching more than 16k
// context)." Gabriel (08/11/2026): put the settings in MCM. EncounterContext.HistoryMemoryCap carries the cap; the
// latest memories are the ones kept. If these fail, the setting either does nothing or drops the newest memories, the
// ones the conversation most likely turns on.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class HistoryMemoryCapTests
   {
      private static readonly string[] Memories = {
         "We met at the ford of Ortysia.",
         "They paid my ransom at Zeonica.",
         "We hunted boar near Pravend.",
         "They sent me a horse from Sargot."
      };

      // STAKE (avalanche24): a cap of two on a full prompt shows the two latest memories and no older one.
      [Test]
      public void GIVEN_a_full_prompt_and_a_cap_of_two_WHEN_built_THEN_only_the_two_latest_memories_are_shown()
      {
         string prompt = Build(2);

         prompt.Should().Contain("Pravend").And.Contain("Sargot");
         prompt.Should().NotContain("Ortysia").And.NotContain("Zeonica");
      }

      // STAKE: the setting's default (no cap) keeps the whole history, exactly as before it existed.
      [Test]
      public void GIVEN_a_full_prompt_and_no_cap_WHEN_built_THEN_every_memory_is_shown()
      {
         string prompt = Build(null);

         foreach (string place in new[] {"Ortysia", "Zeonica", "Pravend", "Sargot"})
            prompt.Should().Contain(place);
      }

      #region private

      private static string Build(int? cap)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

         foreach (string summary in Memories)
            npc.Events.Add(new NotableEvent(10, NotableEventType.Other, summary));

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 12},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, HistoryMemoryCap = cap});
      }

      #endregion
   }
}

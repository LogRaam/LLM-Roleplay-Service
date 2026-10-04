// Code written by Gabriel Mailhot, 03/10/2026.
// Authored quests (docs/design/AUTHORED_QUESTS.md): a character who is part of a quest is told what they know at this
// stage. The block changes at each stage, sometimes as the conversation opens, so it must sit below the cache marker:
// above it, a stage change would rewrite the cached prefix, and a stale prefix would keep the old stage.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class AuthoredQuestBlockTests
   {
      // The block reaches the character, after the marker where per-turn facts belong.
      [Test]
      public void GIVEN_a_character_in_an_authored_quest_WHEN_the_prompt_is_built_THEN_the_block_is_below_the_cache_marker()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "lord_a", Name = "Aldric", Clan = "", Faction = ""},
            new WorldState {CurrentDay = 10}, new EncounterContext {AuthoredQuestBlock = "A MATTER YOU ARE PART OF (The Wolf of the Wood):"});

         prompt.IndexOf("A MATTER YOU ARE PART OF").Should().BeGreaterThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading));
      }
   }
}

// Code written by Gabriel Mailhot, 01/07/2026.
// The shared "prose craft" directive (specificity, varied rhythm, show-don't-tell, and the anti-cliché blocklist)
// must ride in both the lord and commoner system prompts, so replies read like a novelist rather than an AI.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ProseCraftPromptTests
   {
      private static NpcProfile Npc() => new()
      {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      [Test]
      public void GIVEN_the_lord_system_prompt_WHEN_built_THEN_it_carries_the_prose_craft_directive()
      {
         var builder = new PromptBuilder();

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 100});

         prompt.Should().Contain("WRITE LIKE A NOVELIST");
         prompt.Should().Contain("ministrations"); // the anti-cliché blocklist is present
      }

      [Test]
      public void GIVEN_the_commoner_system_prompt_WHEN_built_THEN_it_carries_the_prose_craft_directive()
      {
         var builder = new PromptBuilder();

         string prompt = builder.BuildCommonerSystemPrompt(Npc(), new CommonsKnowledge());

         prompt.Should().Contain("WRITE LIKE A NOVELIST");
      }
   }
}

// Code written by Gabriel Mailhot, 01/07/2026.
// The shared "prose craft" directive (specificity, varied rhythm, show-don't-tell, and the anti-cliché blocklist)
// must ride in both the lord and commoner system prompts, so replies read like a novelist rather than an AI.
//
// WHY IT MATTERS: per the production doc, this is called "the biggest single lift to prose quality", and it
// is universal (any model, adult or not) and stable enough to stay inside the cache-friendly prefix. Two
// separate prompt builders (lord vs commoner) exist, so nothing enforces they stay in sync except a test
// that checks both.

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

      // Both the directive itself AND the anti-cliché blocklist ("ministrations" is one of the listed tired
      // phrases) must be present, since a directive with no concrete blocklist would be easy to comply with
      // in letter while still producing AI-sounding stock phrases.
      [Test]
      public void GIVEN_the_lord_system_prompt_WHEN_built_THEN_it_carries_the_prose_craft_directive()
      {
         var builder = new PromptBuilder();

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 100});

         prompt.Should().Contain("WRITE LIKE A NOVELIST");
         prompt.Should().Contain("ministrations"); // the anti-cliché blocklist is present
      }

      // BuildCommonerSystemPrompt is a separate, slimmer code path (no identity/romantic/quest/witness
      // sections); this guards that the shared craft directive was not forgotten when that slim prompt was
      // assembled.
      [Test]
      public void GIVEN_the_commoner_system_prompt_WHEN_built_THEN_it_carries_the_prose_craft_directive()
      {
         var builder = new PromptBuilder();

         string prompt = builder.BuildCommonerSystemPrompt(Npc(), new CommonsKnowledge());

         prompt.Should().Contain("WRITE LIKE A NOVELIST");
      }
   }
}

// Code written by Gabriel Mailhot, 12/07/2026.
// At-sea grounding: EncounterContext.AtSea, set by the mod when the player party is sailing on open
// water, is rendered by PromptBuilder under an "AT SEA" header so NPCs stop talking of horses and
// roads while aboard a ship. False by default, so the header is absent unless the mod flags it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AtSeaPromptTests
   {
      private const string Header = "AT SEA";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // Without this header an NPC talking to a player mid-ocean would still default to land framing: riding
      // out to meet them, roads, inns, horses, none of which exist on open water. AppendAtSeaNote exists
      // specifically to stop that break in immersion.
      [Test]
      public void GIVEN_the_party_is_at_sea_WHEN_building_the_prompt_THEN_the_at_sea_header_is_injected()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, AtSea = true};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Header);
      }

      // False by default, so an ordinary land conversation is never told there are "no horses, no roads, no
      // inns" when the player is standing in a town.
      [Test]
      public void GIVEN_the_party_is_not_at_sea_WHEN_building_the_prompt_THEN_the_at_sea_header_is_absent()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, AtSea = false};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(Header);
      }
   }
}

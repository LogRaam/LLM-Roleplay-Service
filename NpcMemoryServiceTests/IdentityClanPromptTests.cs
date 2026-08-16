// Code written by Gabriel Mailhot, 15/08/2026.
// The identity line names the NPC's house ("of the X clan"). A clanless NPC (a hunted notable, a landless
// wanderer) has no house, and HeroProfileMapper used to fill that gap with the literal phrase "No clan", which the
// template turned into "of the No clan clan" — an NPC then introduced itself "of the No clan" (player report,
// 2026-08-15). These tests pin that a real clan still renders "of the X clan", and that BOTH an empty clan and the
// legacy "No clan" sentinel render the natural "of no noble clan" instead of naming a fake house.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class IdentityClanPromptTests
   {
      // The ordinary case must be untouched: a lord with a house is named "of the X clan", so the fix for the
      // clanless case cannot have quietly dropped the house from every other NPC's self-introduction.
      [Test]
      public void GIVEN_an_npc_with_a_real_clan_WHEN_building_the_prompt_THEN_the_house_is_named()
      {
         string prompt = Build("dey Meroc");

         prompt.Should().Contain("of the dey Meroc clan");
      }

      // The reported bug: a clanless hero mapped to an EMPTY clan must read "of no noble clan", never "of the  clan"
      // (an empty, double-spaced house) and never a fabricated one.
      [Test]
      public void GIVEN_an_npc_with_no_clan_WHEN_building_the_prompt_THEN_it_reads_of_no_noble_clan()
      {
         string prompt = Build("");

         prompt.Should().Contain("of no noble clan");
         prompt.Should().NotContain("of the  clan");
      }

      // Legacy data: profiles saved before the mapper fix still carry the literal "No clan" sentinel. The prompt must
      // recognise it as clanless too, or an existing save's clanless NPC keeps introducing itself "of the No clan clan".
      [Test]
      public void GIVEN_the_legacy_no_clan_sentinel_WHEN_building_the_prompt_THEN_it_is_treated_as_clanless()
      {
         string prompt = Build("No clan");

         prompt.Should().Contain("of no noble clan");
         prompt.Should().NotContain("No clan clan");
      }

      #region private

      private static string Build(string clan)
      {
         var npc = new NpcProfile {Id = "npc_test", Name = "Emira al Fahda", Faction = "Aserai", Clan = clan, Age = 52};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full};

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      #endregion
   }
}

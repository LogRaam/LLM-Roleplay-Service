// Code written by Gabriel Mailhot, 27/10/2026.
// Guards the family line in every prompt. DepaHN (Nexus, 26/10/2026): "relationships with children and grandchildren feel
// very cold, as do those with brothers, sisters, and relatives". Kin were told they are family only while they shared no
// memory with the player; the line now stays, Lean and Full, with memories or without. If this regresses, a grandchild
// with a shared past speaks to the player as to anyone else again.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class KinIdentityTests
   {
      private static string Prompt(EncounterContext context)
      {
         var npc = new NpcProfile {Id = "k", Name = "Elys", Faction = "Vlandia", Clan = "dey Meroc", Age = 19, IsFemale = true};
         npc.Events.Add(new NotableEvent(3, NotableEventType.Other, "Rode with the player to Pravend."));

         return new PromptBuilder {AdultLevel = AdultContentLevel.Mature}.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      // STAKE: the reported coldness: a granddaughter WITH memories must still be told she is family, Lean and Full.
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_a_granddaughter_with_memories_WHEN_building_the_prompt_THEN_she_is_told_she_is_family(LeanPromptLevel lean)
      {
         Prompt(new EncounterContext {LeanLevel = lean, KinshipToPlayer = "granddaughter"})
            .Should().Contain("You are the player's granddaughter: their blood and their family");
      }

      // STAKE: a stranger is never told they are kin.
      [Test]
      public void GIVEN_no_bond_of_blood_WHEN_building_the_prompt_THEN_no_family_line_appears()
      {
         Prompt(new EncounterContext()).Should().NotContain("their blood and their family");
      }

      // STAKE: a spouse already has her own line; a second, blood line would muddle what she is to the player.
      [Test]
      public void GIVEN_the_players_spouse_WHEN_building_the_prompt_THEN_no_blood_line_is_added()
      {
         Prompt(new EncounterContext {NpcSpouseIsPlayer = true, KinshipToPlayer = "cousin"}).Should().NotContain("their blood and their family");
      }
   }
}

// Code written by Gabriel Mailhot, 25/11/2026.
// attack_together: one army of a lord's party and the player's, raised against an enemy within reach. froggyluv (Nexus,
// 25/11/2026): a lord planned at length how they would attack an enemy party together, and "once the conversation ended,
// she took off the other direction": the model had no deed for it. Gabriel (25/11/2026): the lord who agrees raises an
// army of the two parties, the most powerful leads, and it attacks the right target. These pin the teaching: taught only
// when the game can raise it, with the targets the game listed and who leads, and never otherwise.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AttackTogetherTeachingTests
   {
      private const string Heading = "ATTACK TOGETHER, ONE ARMY";

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(new NpcProfile {Id = "npc_test", Name = "Test Lady", Faction = "Vlandia", Clan = "dey Meroc"}, new WorldState {CurrentDay = 10}, context);

      // STAKE: froggyluv's lady: able to raise the army, she must be taught the deed and the enemies she may name.
      [Test]
      public void GIVEN_the_game_can_raise_the_army_WHEN_building_THEN_the_deed_and_the_listed_targets_are_taught()
      {
         string prompt = Build(new EncounterContext {NpcCanAttackTogether = true, JointAttackTargets = new List<string> {"looters_7: Looters (a party of 30)"}});

         prompt.Should().Contain(Heading);
         prompt.Should().Contain("type: attack_together");
         prompt.Should().Contain("looters_7: Looters (a party of 30)");
      }

      // STAKE: a lord promising an army the game cannot raise (another realm, already in an army) breaks it at once.
      [Test]
      public void GIVEN_the_game_cannot_raise_the_army_WHEN_building_THEN_nothing_is_taught()
      {
         Build(new EncounterContext {NpcCanAttackTogether = false, JointAttackTargets = new List<string> {"looters_7: Looters"}}).Should().NotContain(Heading);
      }

      // STAKE: with no enemy listed, the model would have to invent the target.
      [Test]
      public void GIVEN_no_target_is_listed_WHEN_building_THEN_nothing_is_taught()
      {
         Build(new EncounterContext {NpcCanAttackTogether = true}).Should().NotContain(Heading);
      }

      // STAKE: the lord tells the player who commands; saying it wrong promises the player a command the game will not give.
      [Test]
      public void GIVEN_who_leads_WHEN_building_THEN_the_lord_is_told_which_of_them_commands()
      {
         var targets = new List<string> {"castle_V3: Rhotae Castle (a castle)"};

         Build(new EncounterContext {NpcCanAttackTogether = true, JointAttackTargets = targets, JointAttackPlayerLeads = true})
            .Should().Contain("The player outranks you here and leads it");
         Build(new EncounterContext {NpcCanAttackTogether = true, JointAttackTargets = targets, JointAttackPlayerLeads = false})
            .Should().Contain("You outrank the player here and lead it");
      }
   }
}

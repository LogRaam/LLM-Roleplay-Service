// Code written by Gabriel Mailhot, 29/10/2026.
// Who the people in the room are, and the speaker's own family in Compact. fearente (Nexus, 29/10/2026): a lady governor
// spoke to her father, the head of her clan, "like he was a servant"; a ruler was treated like an ordinary noble; a
// brother spoke of a son "Lorgand" when he has a daughter. The witness line carried only how the speaker felt about a
// person (and a companion nothing but "the player's companion"), and Compact dropped the family with the rest of
// RELATIONSHIPS. If these break, characters treat their own kin and their sovereign as strangers, and invent children.

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
   public class WitnessStandingPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Lady Ira",
         Faction = "the Southern Empire",
         Clan = "Pethros",
         Relationships = "Liege: Rhagaea, ruler of the Southern Empire.\nFriends: Lord Olek.\nFamily: Father: Lord Pethros (age 58); Children: none.",
         Family = "Family: Father: Lord Pethros (age 58); Children: none."
      };

      private static string Build(LeanPromptLevel level, params WitnessEntry[] witnesses)
      {
         var context = new EncounterContext {LeanLevel = level, Witnesses = new List<WitnessEntry>(witnesses)};

         return new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // The report: her father, travelling with the player, read only "the player's companion". Who he is to her must
      // stand on his line, beside his role.
      [Test]
      public void GIVEN_her_father_in_the_room_as_the_players_companion_WHEN_her_prompt_is_built_THEN_his_line_says_he_is_her_father_and_clan_head()
      {
         string prompt = Build(LeanPromptLevel.Full, new WitnessEntry {
            Name = "Lord Pethros", RelationToNpc = "the player's companion", IsPlayerCompanion = true, IsFemale = false,
            Standing = "your father, head of your clan"
         });

         prompt.Should().Contain("- Lord Pethros, a man (the player's companion; your father, head of your clan)");
      }

      // Small models run Compact, and the room matters as much there: the standing is on the line in both modes.
      [Test]
      public void GIVEN_compact_mode_WHEN_a_ruler_is_in_the_room_THEN_her_line_still_names_her_ruler()
      {
         string prompt = Build(LeanPromptLevel.Lean, new WitnessEntry {
            Name = "Rhagaea", RelationToNpc = "your liege", IsFemale = true, Standing = "ruler of the Southern Empire and your sovereign"
         });

         prompt.Should().Contain("- Rhagaea, a woman (your liege; ruler of the Southern Empire and your sovereign)");
      }

      // An ordinary witness, no kin and no rank, keeps the line it always had.
      [Test]
      public void GIVEN_a_witness_with_no_standing_WHEN_built_THEN_the_line_is_unchanged()
      {
         Build(LeanPromptLevel.Full, new WitnessEntry {Name = "Olek", RelationToNpc = "a lord you know", IsFemale = false})
            .Should().Contain("- Olek, a man (a lord you know)\n".Replace("\n", System.Environment.NewLine));
      }

      // Compact drops RELATIONSHIPS (friends, enemies) to fit a small window, but not the family: a lord who is never
      // told who his children are invents them.
      [Test]
      public void GIVEN_compact_mode_WHEN_built_THEN_the_family_is_kept_and_the_rest_of_relationships_is_not()
      {
         string prompt = Build(LeanPromptLevel.Lean);

         prompt.Should().Contain("YOUR FAMILY:");
         prompt.Should().Contain("Children: none");
         prompt.Should().NotContain("RELATIONSHIPS:");
         prompt.Should().NotContain("Friends: Lord Olek");
      }

      // Full already carries the family inside RELATIONSHIPS: it must not be said twice.
      [Test]
      public void GIVEN_full_mode_WHEN_built_THEN_the_family_is_said_once_inside_relationships()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("RELATIONSHIPS:");
         prompt.Should().NotContain("YOUR FAMILY:");
      }
   }
}

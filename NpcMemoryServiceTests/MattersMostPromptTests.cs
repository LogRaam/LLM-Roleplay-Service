// Code written by Gabriel Mailhot, 27/10/2026.
// What matters most to a character reaches the prompt at every adult level (Gabriel, 27/10/2026: "qu'est-ce qui compte
// le plus pour un NPC. Ses enfants? Sa richesse? Son amoureux?"), and the player learns it when the character says it
// in their own words: a [DISCOVERY] with the reserved key matters_most marks it known and is never stored as free text
// beside the game's own wording. MattersMost is saved as an integer, so its ordinals are frozen here. If this is wrong,
// the character never acts on what they cherish, the encyclopedia says it twice in two voices, the discovery is asked
// for again and again, or a save reads one anchor as another.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class MattersMostPromptTests
   {
      // STAKE: a mother who cherishes her children above all is told so, at every adult level, even with adult content off.
      [Test]
      public void GIVEN_a_character_who_cherishes_her_children_WHEN_the_prompt_is_built_THEN_it_says_so_and_how_to_reveal_it()
      {
         string prompt = Build(AdultContentLevel.Off, MattersMost.Children, known: false);

         prompt.Should().Contain("WHAT MATTERS MOST TO YOU, above everything else: your children.");
         prompt.Should().Contain("you never name a false one");
         prompt.Should().Contain("key: " + MattersMostDiscovery.Key);
      }

      // STAKE: once the player knows it, the character is not asked to reveal it again (a discovery every reply).
      [Test]
      public void GIVEN_the_player_already_knows_WHEN_the_prompt_is_built_THEN_it_is_not_asked_to_reveal_it_again()
      {
         string prompt = Build(AdultContentLevel.Off, MattersMost.Glory, known: true);

         prompt.Should().Contain("glory in battle");
         prompt.Should().NotContain("key: " + MattersMostDiscovery.Key);
      }

      // STAKE: a character not yet given one (an old save before it is read) gets no empty line.
      [Test]
      public void GIVEN_nothing_chosen_yet_WHEN_the_prompt_is_built_THEN_nothing_is_said()
      {
         Build(AdultContentLevel.Off, MattersMost.None, known: false).Should().NotContain("WHAT MATTERS MOST");
      }

      // STAKE: the reserved discovery marks it learned and is not stored as free text, so the encyclopedia names it once,
      // in the game's words.
      [Test]
      public void GIVEN_the_reserved_discovery_WHEN_applied_THEN_it_is_marked_known_and_not_stored_as_text()
      {
         var profile = new NpcProfile {Id = "npc", Name = "Lady", Faction = "Empire", Clan = "Pethros", MattersMost = MattersMost.Children};
         var response = new ParsedResponse {
            Dialogue = "My children are everything to me.",
            Discovery = new DiscoveredTrait {Key = "matters_most", Description = "Her children are everything to her."}
         };

         ProfileMutator.Apply(profile, response, gameDay: 3);

         profile.MattersMostKnown.Should().BeTrue();
         profile.DiscoveredTraits.Should().BeEmpty();
      }

      // STAKE: every anchor is told in words: one appended without them would reach the model as an empty line.
      [Test]
      public void GIVEN_every_anchor_WHEN_the_prompt_is_built_THEN_it_is_told_in_words()
      {
         foreach (MattersMost m in System.Enum.GetValues(typeof(MattersMost)))
         {
            if (m == MattersMost.None) continue;
            Build(AdultContentLevel.Off, m, known: true).Should().Contain("above everything else: ", m.ToString())
                                                         .And.NotContain("above everything else: .", m.ToString());
         }
      }

      // STAKE: MattersMost is saved as an integer: inserting or reordering a member would read one anchor as another.
      [TestCase(MattersMost.None, 0)]
      [TestCase(MattersMost.Children, 1)]
      [TestCase(MattersMost.Spouse, 2)]
      [TestCase(MattersMost.Lover, 3)]
      [TestCase(MattersMost.ThePlayer, 4)]
      [TestCase(MattersMost.Kin, 5)]
      [TestCase(MattersMost.HouseHonor, 6)]
      [TestCase(MattersMost.Wealth, 7)]
      [TestCase(MattersMost.Power, 8)]
      [TestCase(MattersMost.Glory, 9)]
      [TestCase(MattersMost.Lands, 10)]
      [TestCase(MattersMost.Freedom, 11)]
      [TestCase(MattersMost.Vengeance, 12)]
      public void GIVEN_a_saved_anchor_WHEN_cast_to_int_THEN_its_ordinal_is_frozen(MattersMost m, int ordinal)
      {
         ((int) m).Should().Be(ordinal);
      }

      // STAKE: a member appended without its frozen ordinal above is caught here.
      [Test]
      public void GIVEN_the_anchor_enum_WHEN_its_members_are_counted_THEN_each_has_its_frozen_ordinal()
      {
         System.Enum.GetValues(typeof(MattersMost)).Length.Should().Be(13);
      }

      private static string Build(AdultContentLevel level, MattersMost m, bool known)
      {
         var npc = new NpcProfile {
            Id = "npc_test", Name = "Test Lady", Faction = "Empire", Clan = "Pethros", ReputationWithPlayer = 30, MattersMost = m,
            MattersMostKnown = known
         };

         return new PromptBuilder {AdultLevel = level}.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, new EncounterContext());
      }
   }
}

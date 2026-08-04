// Code written by Gabriel Mailhot, 03/08/2026.
// Council bug c: CouncilRosterPolicy seats a member on availability alone, by design with no distance/travel
// test (a seated companion may not be in the player's party at all). Without a stated presence, nothing in the
// prompt contradicted such a member claiming, in dialogue or narration, to be already at the player's side on
// the road. WitnessEntry.PresenceStatus (populated by the mod's CouncilRosterResolver.DescribePresence) and the
// directive below are the fix: this file asserts both the per-seat status renders and the directive appears
// only on a council/round-table turn, matching the existing WITNESSES PRESENT / AT THIS TABLE machinery.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CouncilMemberPresencePromptTests
   {
      private const string DirectiveHeader = "SPEAK ONLY YOUR TRUE PRESENCE:";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static readonly WitnessEntry[] NotInPartyMember = {
         new() {
            Name = "Ira", HeroStringId = "hero_ira", RelationToNpc = "a companion in the player's service",
            PresenceStatus = "keeping to Pravend"
         }
      };

      // The core of the fix: a seated member who is not in the player's party must show up with their real
      // whereabouts bracketed right after their seat line, so the model has something true to read instead of
      // inferring co-travel from silence.
      [Test]
      public void GIVEN_a_seated_member_not_in_the_players_party_WHEN_building_the_prompt_THEN_their_presence_status_is_stated()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            Witnesses = NotInPartyMember
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("Ira");
         prompt.Should().Contain("[keeping to Pravend]");
      }

      // The directive that holds every member (and the reply's own speaker) to that stated truth must appear
      // alongside it on a council turn, or the bracketed status is just decoration the model is free to ignore.
      [Test]
      public void GIVEN_a_council_turn_WHEN_building_the_prompt_THEN_the_true_presence_directive_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            Witnesses = NotInPartyMember
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(DirectiveHeader);
         prompt.Should().Contain("share the road with the player this very moment");
      }

      // Absent a PresenceStatus (an ordinary, non-council witness), no bracket must render at all: the concept
      // does not exist outside a council, and an empty "[]" or stray bracket would be a visible regression.
      [Test]
      public void GIVEN_an_ordinary_witness_with_no_presence_status_WHEN_building_the_prompt_THEN_no_bracket_renders()
      {
         var ordinary = new[] {
            new WitnessEntry {Name = "Sley", HeroStringId = "hero_sley", RelationToNpc = "a rival lord"}
         };
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, Witnesses = ordinary};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(DirectiveHeader);
         prompt.Should().NotContain("[]");
      }

      // The directive is council-specific machinery; an ordinary two-person scene (no round table) must never
      // see it, matching every other council-only block in this same method.
      [Test]
      public void GIVEN_an_ordinary_turn_WHEN_building_the_prompt_THEN_the_true_presence_directive_is_absent()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, Witnesses = NotInPartyMember};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(DirectiveHeader);
      }
   }
}

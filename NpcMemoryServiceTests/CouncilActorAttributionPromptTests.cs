// Code written by Gabriel Mailhot, 21/07/2026.
// Council action attribution: the mod's action-dispatch loop only knows to route an [ACTION] to a non-anchor
// seated member when the model names them via "actor:". This teaches the model that vocabulary, and ONLY on a
// council/round-table turn (an ordinary two-person scene has exactly one NPC who could possibly be meant), so
// teaching "actor:" there would be pure noise and risks a model bolting it onto an unrelated action.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CouncilActorAttributionPromptTests
   {
      private const string Header = "ACTIONS AT THE TABLE:";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static readonly WitnessEntry[] OneWitness = {
         new() {Name = "Sley", HeroStringId = "hero_sley", RelationToNpc = "a companion in the player's service"}
      };

      // Without this, a seated member who agrees in their reaction to a gift or a task leaves the mod nothing
      // to attribute it to, and the action loop dispatches every [ACTION] to the anchor regardless of who
      // actually committed to it (the bug this whole feature exists to close).
      [Test]
      public void GIVEN_a_council_turn_WHEN_building_the_prompt_THEN_the_actor_attribution_block_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            Witnesses = OneWitness
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Header);
         prompt.Should().Contain("actor: <the member's name exactly as listed above>");
      }

      // An ordinary one-on-one conversation has only one possible actor (the NPC speaking), so this directive
      // must not appear there (printing it anyway would be noise at best, and at worst invites a model to
      // start emitting "actor:" on a plain two-person action where the mod never looks for one).
      [Test]
      public void GIVEN_an_ordinary_turn_WHEN_building_the_prompt_THEN_the_actor_attribution_block_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            Witnesses = OneWitness
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(Header);
      }
   }
}

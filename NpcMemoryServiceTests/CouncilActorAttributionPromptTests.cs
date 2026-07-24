// Code written by Gabriel Mailhot, 21/07/2026.
// Council action attribution: the mod's action-dispatch loop only knows to route an [ACTION] to a non-anchor
// seated member when the model names them via "actor:". This teaches the model that vocabulary, and ONLY on a
// council/round-table turn (an ordinary two-person scene has exactly one NPC who could possibly be meant), so
// teaching "actor:" there would be pure noise and risks a model bolting it onto an unrelated action.
// Ratified 2026-07-24 (council audit C3): the ONE action carried out live at a council is a shift in a
// member's REGARD (change_relation); deeds defer to [RESOLUTION]. The block was renamed and narrowed to that,
// so the actor field now routes a perception change to the right seat rather than an immediate gift of gold.

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
      private const string Header = "A SHIFT IN REGARD AT THE TABLE:";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static readonly WitnessEntry[] OneWitness = {
         new() {Name = "Sley", HeroStringId = "hero_sley", RelationToNpc = "a companion in the player's service"}
      };

      // Without this, a seated member whose regard shifts (a witness the player just angered) leaves the mod
      // nothing to attribute the change to, and the action loop dispatches the change_relation to the anchor
      // regardless of who actually reacted (the misattribution this whole feature exists to close). Narrowed to
      // change_relation because that is the only verb the bridge runs live at a council; deeds defer.
      [Test]
      public void GIVEN_a_council_turn_WHEN_building_the_prompt_THEN_the_regard_attribution_block_is_taught()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsRoundTableTurn = true,
            Witnesses = OneWitness
         };

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(Header);
         prompt.Should().Contain("type: change_relation");
         prompt.Should().Contain("actor: <the member's name exactly as listed above>");
         // And it must say plainly that a DEED is a resolution, not an immediate action, so the narrowing holds.
         prompt.Should().Contain("is a [RESOLUTION], never an [ACTION] here");
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

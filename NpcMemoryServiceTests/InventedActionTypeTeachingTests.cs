// Code written by Gabriel Mailhot, 12/08/2026.
// Pins the teaching added to STAY WITHIN WHAT YOU KNOW that forbids the model from inventing an [ACTION] type.
//
// The stake (player report): in a triumphant scene the model emitted an [ACTION] whose type was not a real
// mod verb (an invented "grant a fief" / "bless a marriage"). The bridge correctly refused it (a verb with no
// executor is inert), but the player saw an out-of-character refusal mid-narrative that read as contradicting
// the story. The wording fix on the mod side treats the symptom; THIS teaching treats the cause, telling the
// model to emit only the action types it was taught and to route a later reward through a [QUEST] reward
// instead of a made-up [ACTION]. If the teaching is dropped or reworded away from its distinctive opening,
// the cause returns, so this test holds the phrase in place.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class InventedActionTypeTeachingTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build()
      {
         var builder = new PromptBuilder();

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10});
      }

      // The core rule: emit only taught action types, never a fabricated one. This is the exact directive whose
      // absence produced the player-facing refusal mid-scene, so the distinctive opening phrase is asserted.
      [Test]
      public void GIVEN_the_stay_within_what_you_know_rule_WHEN_built_THEN_it_forbids_inventing_an_action_type()
      {
         string prompt = Build();

         prompt.Should().Contain("Never invent an action type");
         prompt.Should().Contain("Emit ONLY the [ACTION] types you were explicitly taught");
      }

      // The redirect half: when there is no action for the outcome, the model must describe it in words or, for
      // a reward delivered later (a fief, a title, a marriage), route it through a [QUEST] reward, never a
      // made-up [ACTION]. Without this, the model would still reach for an invented tag to "make it real".
      [Test]
      public void GIVEN_the_teaching_WHEN_built_THEN_it_redirects_a_later_reward_to_a_quest_reward()
      {
         string prompt = Build();

         prompt.Should().Contain("set it as a [QUEST] reward, never a made-up [ACTION]");
      }
   }
}

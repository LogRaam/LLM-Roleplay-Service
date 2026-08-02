// Code written by Gabriel Mailhot, 01/08/2026.
// Player report: a Sadistic/Hotheaded lord rejected a duel in character, then in the very same reply spoke
// of "protecting peasants" (a kindly, prosocial line no cruel/calculating lord would volunteer). The model
// (e.g. GLM) drifts toward agreeable, prosocial answers even when the identity block above states a cold or
// cruel nature. This directive tells the NPC to let its nature colour EVERY judgment, not only the ones the
// PERSONALITY/CHARACTER blocks explicitly cover, and to resist drifting into kindliness to please the
// listener. It is gated on a non-empty Personality so a minimal/lean profile (no derived personality, e.g.
// the lean-budget test's profile) renders nothing extra and the lean token budget is unaffected.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PersonaAdherencePromptTests
   {
      private static NpcProfile NpcWithPersonality() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         Personality = "Sadistic, Hotheaded",
         Trait = "Cruel and quick to anger, delights in others' suffering."
      };

      private static NpcProfile NpcWithoutPersonality() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(NpcProfile npc)
         => new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, new EncounterContext());

      // The root bug: a cruel/calculating lord answered a duel refusal in character, then pivoted to a
      // kindly, prosocial line about protecting peasants. The directive must be present to counter that
      // drift whenever the NPC has a derived personality to stay true to.
      [Test]
      public void GIVEN_an_npc_with_a_personality_WHEN_the_prompt_is_built_THEN_the_stay_true_directive_is_present()
      {
         string prompt = Build(NpcWithPersonality());

         prompt.Should().Contain("STAY TRUE TO THIS NATURE");
         prompt.Should().Contain("do not drift");
         prompt.Should().Contain("into kindly, agreeable, or prosocial answers");
      }

      // A minimal profile (no derived Personality, the shape used by the lean/minimal-profile path) must
      // not gain this block: it exists to reinforce a stated nature, not to add unconditional overhead to
      // every prompt regardless of whether the NPC has a personality to enforce.
      [Test]
      public void GIVEN_an_npc_without_a_personality_WHEN_the_prompt_is_built_THEN_the_stay_true_directive_is_absent()
      {
         string prompt = Build(NpcWithoutPersonality());

         prompt.Should().NotContain("STAY TRUE TO THIS NATURE");
      }
   }
}

// Code written by Gabriel Mailhot, 20/09/2026.
// What these pin: that a character never arranges a meeting the game cannot hold, and never blames the player for
// missing one.
//
// yozakura12 (Nexus, 19/09/2026): "During conversations, NPCs tell me when and where to meet them. However, when I
// go to the designated location at the appointed time, the NPC isn't there. Consequently, the NPC gets upset,
// claiming I didn't keep the promise. Does this game actually have a meet-up feature?"
//
// It does not. The character invented the appointment, its own [EVENT] memory recorded the promise, and the player
// was then punished for a meeting nobody could have kept: the one failure mode the mod must never have, since the
// whole pitch is that what is remembered is true. A real rendezvous is a pillar to be built. This is the guardrail
// that stands until then, and the second half of it, never reproaching a missed meeting, has to keep standing
// afterwards for every promise already written into a save.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class NoAppointmentsTests
   {
      // THE PROMISE ITSELF: the rule names the shape of what must not be said, because "do not make appointments"
      // alone reads to a model as advice about diaries.
      [Test]
      public void GIVEN_any_conversation_WHEN_the_prompt_is_built_THEN_the_character_is_told_it_cannot_fix_a_meeting()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("MEETINGS YOU CANNOT ARRANGE");
         prompt.Should().Contain("never fix a place and a time to");
      }

      // AND WHAT TO DO INSTEAD, or the character simply goes silent where it would have offered something.
      [Test]
      public void GIVEN_the_rule_WHEN_read_THEN_it_offers_what_the_game_can_actually_do()
      {
         Build(LeanPromptLevel.Full).Should().Contain("say you will write");
      }

      // THE HALF THAT OUTLIVES THE GUARDRAIL: saves are already full of promises made before this rule existed,
      // and the player must not keep paying for them.
      [Test]
      public void GIVEN_the_rule_WHEN_read_THEN_the_character_is_forbidden_to_reproach_a_missed_meeting()
      {
         Build(LeanPromptLevel.Full).Should().Contain("never reproach the player for missing a meeting");
      }

      // A local model on the compact prompt invents MORE, not less, so this is exactly where the rule must hold.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_it_is_built_THEN_the_rule_is_there_too()
      {
         Build(LeanPromptLevel.Lean).Should().Contain("MEETINGS YOU CANNOT ARRANGE");
      }

      private static string Build(LeanPromptLevel lean)
         => new PromptBuilder().BuildSystemPrompt(
            new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"},
            new WorldState {CurrentDay = 12},
            new EncounterContext {LeanLevel = lean, Scene = SceneType.Keep});
   }
}

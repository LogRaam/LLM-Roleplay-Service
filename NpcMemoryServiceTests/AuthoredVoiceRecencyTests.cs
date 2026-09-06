// Code written by Gabriel Mailhot, 06/09/2026.
// Player report 2026-09-06: an author gave two characters a per-character voice through the character_overrides
// "backstory" field ("speaks loudly in the voice of Homer", "speaks in verse in the voice of Baudelaire") and found
// it almost worked. It landed for the first exchange or two and then faded. His diagnosis was exactly right, and it
// is the SAME failure the narrative-style echo was added to fix a few days earlier: BACKSTORY AND VOICE sits far up
// in the cacheable prefix, and the long imperative tail (format contract, action checklist) drowns it out by the
// time the model generates. So the authored voice now gets the same recency anchor at the very end of the prompt.
//
// These tests pin the two things that make that anchor work: it exists at all when a character HAS an authored
// voice (and never otherwise, so an ordinary NPC's prompt is not padded), and it ties the voice to the PRESENT
// rather than restating it as biography, which is what the player asked for.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class AuthoredVoiceRecencyTests
   {
      private static NpcProfile Npc(string authoredBackstory = null) => new() {
         Id = "npc_test",
         Name = "Borcuff",
         Faction = "Empire",
         Clan = "dey Meroc",
         AuthoredBackstory = authoredBackstory
      };

      private static string Build(NpcProfile npc)
         => new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full});

      // An ordinary character must not gain the anchor: it would pad every prompt in the game with a reminder about
      // a voice nobody authored.
      [Test]
      public void GIVEN_no_authored_backstory_WHEN_built_THEN_no_voice_anchor_is_added()
      {
         Build(Npc()).Should().NotContain("BACKSTORY AND VOICE, taught above, is not background");
      }

      // THE fix: a character with an authored voice gets it restated in the highest-recency window, after the format
      // contract, which is what stops it fading a few exchanges in.
      [Test]
      public void GIVEN_an_authored_voice_WHEN_built_THEN_it_is_re_anchored_at_the_end()
      {
         string prompt = Build(Npc("A former imperial infantry captain who speaks loudly in the voice of Homer."));

         prompt.Should().Contain("BACKSTORY AND VOICE, taught above, is not background");

         int anchor = prompt.IndexOf("is not background", System.StringComparison.Ordinal);
         int contract = prompt.IndexOf("FORMAT REMINDER", System.StringComparison.Ordinal);
         anchor.Should().BeGreaterThan(contract, "the anchor only works if it comes AFTER the imperative tail that was drowning it");
      }

      // The player's own insight, and the reason the wording matters: a voice restated as biography reads as
      // something that WAS true. It has to be pinned to the present, or the model treats it as past colour again.
      [Test]
      public void GIVEN_an_authored_voice_WHEN_built_THEN_the_anchor_ties_it_to_the_present()
      {
         string prompt = Build(Npc("She is a shy Bretonnian Damsel who speaks in verse in the voice of Baudelaire."));

         prompt.Should().Contain("RIGHT NOW");
         prompt.Should().Contain("never fade");
      }

      // The anchor must not cost the Lean budget, which is pinned elsewhere by LeanPromptPolicyTests: a small model
      // has no headroom, so this rides in the Full prompt only.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_anchor_is_left_out()
      {
         string lean = new PromptBuilder().BuildSystemPrompt(
            Npc("A former imperial infantry captain who speaks in the voice of Homer."),
            new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Lean});

         lean.Should().NotContain("is not background");
      }
   }
}

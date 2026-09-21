// Code written by Gabriel Mailhot, 20/09/2026.
// What these pin: that the last thing a character reads before replying is who it is and who the other voice is.
//
// Fakade (Nexus, 20/09/2026) pasted his model's own reasoning from LM Studio, which shows the failure precisely.
// Turn one: "Goal: I sought this meeting. The matter is 'Arthur took Lady Debana captive'" -- exactly right.
// Turn two, after he confirms he holds her: "Analyze the Player's Speech/Action: ... He challenges Diasca's right
// to hold her captive" -- the character has quietly taken the player's side of the matter for its own.
//
// The identity contract was already stated, near the TOP of the prompt: "You are {name}" and "THE PLAYER: Name".
// On a 4B model with a short context that is thousands of characters back, and it can be truncated out of the
// middle entirely. So it is restated at the point of highest recency, in two sentences, with the one ownership
// fact that goes visibly wrong attached to it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class RoleAnchorTests
   {
      // BOTH NAMES, at the end, where the model reads last.
      [Test]
      public void GIVEN_any_conversation_WHEN_the_prompt_is_built_THEN_it_ends_by_naming_both_parties()
      {
         string prompt = Build();

         prompt.Should().Contain("WHO IS WHO: you are Chief Rolan. The other voice is Arthur, the player.");
         prompt.Should().Contain("never yours");
      }

      // RECENCY IS THE WHOLE POINT: an anchor in the middle of a long prompt is the anchor that was already there.
      [Test]
      public void GIVEN_the_prompt_WHEN_it_is_built_THEN_the_anchor_sits_in_its_last_stretch()
      {
         string prompt = Build();

         prompt.IndexOf("WHO IS WHO:", System.StringComparison.Ordinal)
               .Should().BeGreaterThan((int) (prompt.Length * 0.8), "it must be read last, not merely be present");
      }

      // The small model is the one that loses the thread, so Lean carries it too, shorter.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_it_is_built_THEN_the_anchor_is_there_in_brief()
      {
         Build(LeanPromptLevel.Lean).Should().Contain("WHO IS WHO: you are Chief Rolan; the other voice is Arthur.");
      }

      // THE OWNERSHIP FACT THAT KEEPS GOING WRONG, restated where it cannot be lost.
      [Test]
      public void GIVEN_the_player_holds_captives_WHEN_the_prompt_is_built_THEN_the_anchor_says_they_are_not_yours()
      {
         Build(captives: "- Lady Debana (Southern Empire)")
            .Should().Contain("Arthur holds the lord captives listed above, not you.");
      }

      // A CAPTOR WHO HAS NOT BEEN TOLD THE NAME must not be handed it here of all places: the withholding is the
      // whole point of the captive scene, and the point of highest recency is the worst place to leak it.
      [Test]
      public void GIVEN_a_prisoner_who_has_not_given_their_name_WHEN_the_prompt_is_built_THEN_the_anchor_withholds_it()
      {
         string prompt = new PromptBuilder {PlayerName = "Arthur"}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 12},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, Scene = SceneType.Dungeon, PlayerStatus = PlayerStatusVsNpc.Captive});

         prompt.Should().Contain("WHO IS WHO: you are Chief Rolan. The other voice is the other person here");
         prompt.Should().NotContain("Arthur");
      }

      #region private

      private static NpcProfile Npc() => new() {Id = "n", Name = "Chief Rolan", Faction = "Sturgia", Clan = "Rolan"};

      private static string Build(LeanPromptLevel lean = LeanPromptLevel.Full, string? captives = null)
         => new PromptBuilder {PlayerName = "Arthur"}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 12},
            new EncounterContext {
               LeanLevel = lean, Scene = SceneType.Keep, WarStatus = DiplomaticStatus.AtWar, HeldLordPrisoners = captives
            });

      #endregion
   }
}

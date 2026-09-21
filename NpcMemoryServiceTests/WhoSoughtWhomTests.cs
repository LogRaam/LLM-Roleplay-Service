// Code written by Gabriel Mailhot, 19/09/2026.
// What these pin: that a character who sought the player out is still told so on turn five, and that a compact
// prompt says whose captives are whose.
//
// Fakade (Nexus, 19/09/2026), on a local model through LM Studio:
//
//   "when I am approached by another, the model gets confused... it says that it knows they approached me, and the
//    dialogue begins normally. I respond, then the AI treats the conversation as if I approached them.
//    - AI approaches me, one of the reasons is the prisoners I have captured...
//    - I confirm I have the prisoners
//    - The next turn, the AI thinks THEY have the prisoners, and they say they took them as a matter of war."
//
// Two separate holes, both of them ours. Who sought whom was stated once, in a directive the host consumes on
// read, so from turn two nothing in the prompt said it and the only evidence left was the transcript, whose most
// recent voice is the player's. And the line naming the player as the captives' holder lives in the prisoner
// teaching, which the compact prompt drops entirely, so a small model had nothing to hold on to there either.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class WhoSoughtWhomTests
   {
      private const string Captives = "- Lord Ergeon (Northern Empire)";

      // TURN ONE: the opening directive and the standing line both speak, and they do not contradict each other.
      [Test]
      public void GIVEN_the_turn_the_npc_rode_out_WHEN_the_prompt_is_built_THEN_both_the_directive_and_the_standing_line_are_there()
      {
         string prompt = Build(sought: true, interception: "It is YOU who sought this meeting: you rode out to them.");

         prompt.Should().Contain("WHO SOUGHT WHOM: you approached the player");
         prompt.Should().Contain("you rode out to them");
      }

      // TURN TWO AND AFTER, which is the report: the directive is spent, and the standing fact must survive it.
      [Test]
      public void GIVEN_a_later_turn_of_the_same_conversation_WHEN_the_prompt_is_built_THEN_it_still_says_who_approached_whom()
      {
         string prompt = Build(sought: true);

         prompt.Should().Contain("WHO SOUGHT WHOM: you approached the player, not the other way round");
         prompt.Should().Contain("whole of this conversation");
      }

      // A SMALL MODEL IS THE ONE THAT LOSES THE THREAD, so the compact prompt is exactly where this must hold.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_the_npc_sought_the_player_THEN_the_standing_line_is_there_too()
      {
         Build(sought: true, lean: LeanPromptLevel.Lean).Should().Contain("WHO SOUGHT WHOM");
      }

      // An ordinary conversation, where the player walked up, must say nothing at all: a line claiming the
      // character sought a player who sought them would be worse than the silence it replaces.
      [Test]
      public void GIVEN_a_conversation_the_player_began_WHEN_the_prompt_is_built_THEN_nothing_claims_the_npc_sought_them()
      {
         Build(sought: false).Should().NotContain("WHO SOUGHT WHOM");
      }

      #region whose captives

      // THE SECOND HALF OF HIS EXAMPLE: on a compact prompt the whole prisoner-bargain section is dropped, so
      // nothing said whose captives they were and the character claimed them as its own spoils of war.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_the_player_holds_lord_captives_THEN_it_says_they_are_the_players()
      {
         string prompt = Build(sought: true, lean: LeanPromptLevel.Lean, captives: Captives);

         prompt.Should().Contain("THE PLAYER holds these lord captives, not you:");
         prompt.Should().Contain("Lord Ergeon");
      }

      // The full prompt names the holder inside the bargain teaching too, but that sits thousands of characters
      // up in the cached prefix, which is exactly the part a short-context model loses. Fakade saw the swap again
      // on 20/09/2026, after the Lean-only version of this line shipped, so the tail carries it in Full as well.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_the_player_holds_lord_captives_THEN_the_tail_says_so_too()
      {
         string prompt = Build(sought: true, captives: Captives);

         prompt.Should().Contain("The player currently holds these lord captives:");
         prompt.Should().Contain("THE PLAYER holds these lord captives, not you:");
      }

      // A compact prompt costs the player money and a small model its attention, so the line appears only when
      // there is something to say.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_the_player_holds_nobody_THEN_no_prisoner_line_is_spent()
      {
         Build(sought: true, lean: LeanPromptLevel.Lean).Should().NotContain("lord captives");
      }

      // A player who is the captive here holds nobody; the line would be absurd in a cell.
      [Test]
      public void GIVEN_the_player_is_this_characters_captive_WHEN_the_compact_prompt_is_built_THEN_no_prisoner_line_is_added()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 12},
            new EncounterContext {
               LeanLevel = LeanPromptLevel.Lean, Scene = SceneType.Dungeon, WarStatus = DiplomaticStatus.AtWar,
               HeldLordPrisoners = Captives, PlayerStatus = PlayerStatusVsNpc.Captive
            });

         prompt.Should().NotContain("THE PLAYER holds these lord captives");
      }

      #endregion

      #region private

      private static NpcProfile Npc() => new() {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

      private static string Build(
         bool sought, string? interception = null, LeanPromptLevel lean = LeanPromptLevel.Full, string? captives = null)
         => new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 12},
            new EncounterContext {
               LeanLevel = lean,
               Scene = SceneType.Keep,
               WarStatus = DiplomaticStatus.AtPeace,
               NpcSoughtThePlayer = sought,
               InterceptionReason = interception,
               HeldLordPrisoners = captives
            });

      #endregion
   }
}

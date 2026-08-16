// Code written by Gabriel Mailhot, 15/08/2026.
// A lord captor holding the player prisoner used to be handed the player's full identity (THE PLAYER: Name/Clan)
// straight off the prompt, and used it in the fiction even on a fresh capture - a captor naming "a Thais whelp"
// though the prisoner had never told her (player report, 2026-08-15). Only brigand captors were gated. Now a
// non-bandit captor is gated too: on a fresh capture they read rank from the captive's bearing but do NOT know the
// name or house until the prisoner gives it (CaptorKnowsPlayerName) or they already knew them (CaptorKnowsPlayer).
// These tests pin that the player's identity is withheld on a fresh capture, revealed once known, and that the
// captor's OWN house (AppendIdentity) is never affected.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptorPlayerPerceptionPromptTests
   {
      // The exact report: a fresh capture must NOT leak the player's name or clan. The captor took a stranger on the
      // field; naming "Aldric" or "Thais" is knowledge she does not have until he gives it.
      [Test]
      public void GIVEN_a_fresh_lord_capture_WHEN_building_the_prompt_THEN_the_players_name_and_clan_are_withheld()
      {
         string prompt = Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerStatus = PlayerStatusVsNpc.Captive});

         prompt.Should().NotContain("Aldric");
         prompt.Should().NotContain("Thais");
         prompt.Should().Contain("do NOT know their NAME");
      }

      // The captor's OWN house must survive untouched: the fix withholds the PLAYER's identity, never the NPC's. The
      // identity line ("of the Osticos clan") is a separate block and must still name her house.
      [Test]
      public void GIVEN_a_fresh_lord_capture_WHEN_building_the_prompt_THEN_the_captors_own_clan_is_still_named()
      {
         string prompt = Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerStatus = PlayerStatusVsNpc.Captive});

         prompt.Should().Contain("Osticos"); // the NPC's own house, from AppendIdentity
      }

      // Once the prisoner has given their name this scene (CaptorKnowsPlayerName), the captor may use the name and
      // house freely - the withholding is about knowledge not yet earned, not a permanent gag.
      [Test]
      public void GIVEN_the_prisoner_has_given_their_name_WHEN_building_the_prompt_THEN_the_name_and_clan_are_available()
      {
         string prompt = Build(new EncounterContext
         {
            LeanLevel = LeanPromptLevel.Full,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptorKnowsPlayerName = true
         });

         prompt.Should().Contain("Aldric");
         prompt.Should().Contain("Thais");
      }

      // A captor who genuinely knew the player beforehand (CaptorKnowsPlayer) knows the name too - a lord recognises
      // a noble they have dealt with. The gate is for STRANGERS, not for a captor with real prior acquaintance.
      [Test]
      public void GIVEN_a_captor_who_already_knew_the_player_WHEN_building_the_prompt_THEN_the_name_is_available()
      {
         string prompt = Build(new EncounterContext
         {
            LeanLevel = LeanPromptLevel.Full,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptorKnowsPlayer = true
         });

         prompt.Should().Contain("Aldric");
      }

      // The gate is specific to the captor-of-player case: an ordinary conversation (player free) must still get the
      // full THE PLAYER block with the name, or every normal NPC would suddenly forget who they are talking to.
      [Test]
      public void GIVEN_the_player_is_not_a_captive_WHEN_building_the_prompt_THEN_the_full_player_identity_is_given()
      {
         string prompt = Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerStatus = PlayerStatusVsNpc.Free});

         prompt.Should().Contain("Aldric");
         prompt.Should().Contain("THE PLAYER:");
      }

      #region private

      private static string Build(EncounterContext context)
      {
         var npc = new NpcProfile {Id = "npc_test", Name = "Susada", Faction = "Empire", Clan = "Osticos", Age = 40};
         var builder = new PromptBuilder {PlayerName = "Aldric", PlayerClanName = "Thais", PlayerIsFemale = false};

         return builder.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      #endregion
   }
}

// Code written by Gabriel Mailhot, 11/09/2026.
// What this pins: whether a character is told the POST HE HOLDS, in the identity section that opens every
// prompt, rather than only inside the offer that would appoint him to it.
//
// Why it exists. fkasad, 2026-09-11: he made a companion governor through the vanilla interface, came back
// about a year later, and found the man "totally oblivious that 1. I own the city. and 2. He's governor." He
// told him so in conversation, it worked, and the memory it left is the evidence:
//
//   "Amfildor REVEALED that he conquered Hargendorf through siege and battle, and he officially appointed me
//    as his governor."
//
// Nothing was revealed. The man had been administering that town for a year. The model reported honestly what
// we had done to it: this was the first time anyone had told it.
//
// The mod always KNEW. EncounterContextBuilder reads Hero.GovernorOf a few lines below where this fact is now
// set, to decide whether appoint_governor may be offered. The fact was used to GATE AN ACTION and never to
// STATE A TRUTH, so the one man in Calradia certain to know he governs Hargendorf was the only one never told.
// This is the 2026-09-08 king bug two rungs down, with the same cause and the same shape of fix.
//
// If these tests fail, a governor is about to congratulate the player on news he has been living inside.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class GovernorKnowsHisOwnPostTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_halswyn",
         Name = "Halswyn",
         Faction = "Vlandia",
         Clan = "Aelindrath"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // THE REPORT. A companion keeping the player's town must be told that he keeps it.
      [Test]
      public void GIVEN_a_governor_of_the_players_town_WHEN_the_prompt_is_built_THEN_he_is_told_he_governs_it()
      {
         string prompt = Build(new EncounterContext {
            GovernedSettlementName = "Hargendorf", GovernsForPlayerHouse = true
         });

         prompt.Should().Contain("HARGENDORF");
      }

      // The OTHER half of his report, and the half that produced the word "revealed": the man did not know who
      // owned the city either. From the governor's own side these are one fact - he keeps that town, for someone.
      [Test]
      public void GIVEN_the_town_belongs_to_the_player_WHEN_built_THEN_he_is_told_whose_house_he_keeps_it_for()
      {
         string prompt = Build(new EncounterContext {
            GovernedSettlementName = "Hargendorf", GovernsForPlayerHouse = true
         });

         prompt.Should().Contain("PLAYER'S HOUSE");
      }

      // And the instruction that answers "revealed" head on: this is an ordinary daily fact of his life, so he
      // must not receive it as news the moment the player happens to mention it.
      [Test]
      public void GIVEN_his_own_post_WHEN_built_THEN_he_is_told_not_to_hear_it_as_news()
      {
         string prompt = Build(new EncounterContext {
            GovernedSettlementName = "Hargendorf", GovernsForPlayerHouse = true
         });

         prompt.Should().Contain("not news");
      }

      // A lord governing his OWN house's town knows his job just as surely. What a man does all day is not
      // privileged information, so this is deliberately NOT gated on being the player's man.
      [Test]
      public void GIVEN_a_lord_governing_for_his_own_house_WHEN_built_THEN_he_is_told_so_too()
      {
         string prompt = Build(new EncounterContext {
            GovernedSettlementName = "Ortysia", GovernsForPlayerHouse = false
         });

         prompt.Should().Contain("Ortysia");
         prompt.Should().NotContain("PLAYER'S HOUSE");
      }

      // Most characters govern nothing, and must not be handed a post they do not hold. This is what keeps the
      // fix from inventing governors out of a blank field.
      [Test]
      public void GIVEN_a_character_who_governs_nothing_WHEN_built_THEN_no_post_is_invented_for_him()
      {
         Build(new EncounterContext()).Should().NotContain("YOU GOVERN");
         Build(new EncounterContext {GovernedSettlementName = "   "}).Should().NotContain("YOU GOVERN");
      }

      // A man may head his house AND keep a town, and both are true at once. The crown line above returns early
      // on purpose; this one must not be lost behind it, or a clan leader who governs is back where fkasad found
      // his companion.
      [Test]
      public void GIVEN_a_clan_head_who_also_governs_WHEN_built_THEN_both_are_said_because_both_are_true()
      {
         string prompt = Build(new EncounterContext {
            // The house NAME now travels on the context rather than on the profile: station became a
            // knowledge pack on 2026-09-12, and a pack reads the context. The host fills it from the
            // LIVE hero, so a context built by hand must do the same or it measures its own setup.
            LeadsOwnClan = true, SpeakerClanName = "Aelindrath",
            GovernedSettlementName = "Hargendorf", GovernsForPlayerHouse = true
         });

         prompt.Should().Contain("HEAD the Aelindrath clan");
         prompt.Should().Contain("HARGENDORF");
      }
   }
}

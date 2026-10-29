// Code written by Gabriel Mailhot, 28/10/2026.
// Guards what a commoner is taught when a fight with the player is on offer. Gabriel (28/10/2026): the duel is noble, the
// street brawl a settling of scores, and a companion only brawls. avalanche24 (Nexus, 28/10/2026): a companion hired hours
// before called him out for being told to follow orders, and a landowner with no skill at arms called out a master. If this
// regresses, a commoner hears "both of standing" and steel again, or is taught to start a fight they may not start.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class BrawlTeachingPromptTests
   {
      private static string Prompt(EncounterContext context)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
           .BuildSystemPrompt(new NpcProfile {Id = "c", Name = "Ymira", Faction = "Battania", Clan = "", Age = 28, IsFemale = true},
              new WorldState {CurrentDay = 30}, context);

      // STAKE: a brawler is never told she and the player are "both of standing", nor offered steel.
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_a_brawl_on_offer_WHEN_taught_THEN_it_is_fists_and_never_a_duel_of_standing(LeanPromptLevel lean)
      {
         string prompt = Prompt(new EncounterContext {LeanLevel = lean, DuelChallengeAvailable = true, DuelIsBrawl = true});

         prompt.Should().Contain("A SCORE MAY BE SETTLED WITH FISTS");
         prompt.Should().NotContain("both of standing").And.NotContain("A MATTER OF HONOUR MAY BE SETTLED WITH STEEL");
      }

      // STAKE: avalanche24's new hire: one who may not start a brawl is told so and only learns to answer the player's.
      [Test]
      public void GIVEN_a_commoner_who_may_not_call_the_player_out_WHEN_taught_THEN_only_the_players_challenge_is_offered()
      {
         string prompt = Prompt(new EncounterContext {DuelChallengeAvailable = true, DuelIsBrawl = true, NpcMayNotCallOut = true});

         prompt.Should().Contain("You will NOT start one yourself");
         prompt.Should().NotContain("challenger: you");
      }

      // STAKE: after a brawl, the beaten one speaks of blows traded, never of crossed blades.
      [Test]
      public void GIVEN_a_brawl_just_fought_WHEN_the_conversation_resumes_THEN_it_speaks_of_fists()
      {
         string prompt = Prompt(new EncounterContext {IsPostDuel = true, PostDuelPlayerWon = true, PostDuelWasBrawl = true});

         prompt.Should().Contain("THE BRAWL IS OVER").And.Contain("traded blows with the player, fists not steel,");
         prompt.Should().NotContain("crossed blades");
      }
   }
}

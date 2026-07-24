// Code written by Gabriel Mailhot, 23/07/2026.
// Player report (Nexus, 2026-07-23): a player courted Ira, was sent by her mother to capture a city, took it,
// came back for the family's blessing and received it, then went to Ira and went through the whole ceremony in
// dialogue. Nothing happened in law: she never joined his clan. (He runs DeepSeek v3.2, so the model also
// ignored the guard that told it not to declare a marriage done, but that is only half the story.)
// The machinery was correct throughout: the marry action is taught only when the game can honour it, and the
// bridge re-validates before sealing. What failed was that the gate he actually missed was INVISIBLE. He had
// built the FAMILY's goodwill through a quest, while the bride's own regard never moved (a quest's relation
// reward goes to its giver, not to their daughter), and the prompt's refusal said only that marriage is "a
// formal, family-gated matter" -- true, generic, and useless to a player who had just done the family part.
// These tests pin the named obstacle: exactly one reason, spoken by the NPC in her own words, so an invisible
// rule becomes a scene the player can act on. They also pin the two boundaries that keep it honest: silence
// when nothing is in the way, and no leak of the guidance into a conversation where marriage IS on offer.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class MarriageObstaclePromptTests
   {
      private const string ObstacleHeading = "WHAT STANDS IN THE WAY";
      private const string NotOnOfferHeading = "MARRIAGE IS NOT YET ON OFFER:";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Ira",
         Faction = "Battania",
         Clan = "Fen Duran"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The reported case exactly. This is the ONE obstacle a player can actually do something about, and the
      // one he was never told existed. It must also say what would change it, or naming it just moves the
      // frustration rather than resolving it.
      [Test]
      public void GIVEN_the_bride_does_not_hold_the_player_dear_enough_WHEN_built_THEN_she_is_told_to_say_so_herself()
      {
         string prompt = Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.RegardTooLow});

         prompt.Should().Contain(ObstacleHeading);
         prompt.Should().Contain("It is YOU.");
      }

      // The specific correction for what this player lived through: he had the family's blessing in hand and
      // reasonably read it as the last step. The NPC must not treat her family's consent as settling her own
      // heart, which is precisely the confusion that cost him a campaign's worth of effort.
      [Test]
      public void GIVEN_the_regard_obstacle_WHEN_built_THEN_a_family_blessing_is_not_allowed_to_stand_in_for_her_consent()
      {
         Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.RegardTooLow})
            .Should().Contain("Never treat their standing with your family");
      }

      // The war gate is a MAP FACTION test, not a personal one (BannerlordGameStateBridge.IsAtWarWithPlayer),
      // so an NPC who adores the player is still barred while their kingdoms fight. Without a named obstacle
      // that reads as the mod being broken; named, it is a story about two people caught in a war.
      [Test]
      public void GIVEN_their_peoples_are_at_war_WHEN_built_THEN_the_war_is_named_as_the_obstacle()
      {
         Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.AtWar})
            .Should().Contain("your peoples are at war");
      }

      // A vow taken in a cell is worth nothing, and vanilla blocks it at its own screen too. The captive case
      // needs its own words: "you do not hold me dear enough" would be a lie told to a prisoner.
      [Test]
      public void GIVEN_one_of_them_is_a_captive_WHEN_built_THEN_captivity_is_named_rather_than_regard()
      {
         string prompt = Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.Captive});

         prompt.Should().Contain("one of you is a captive");
         prompt.Should().NotContain("It is YOU.");
      }

      // Silence is the correct answer for the rules an NPC has no natural words for (age, station, an existing
      // marriage of her own). Inventing a line for those would put stilted legalese in her mouth, which is
      // worse than the generality already above it.
      [Test]
      public void GIVEN_a_rule_the_npc_could_not_naturally_speak_of_WHEN_built_THEN_no_obstacle_is_named()
      {
         string prompt = Build(new EncounterContext {MarriageBlockedBecause = MarriageBlockReason.NotMarriageable});

         prompt.Should().Contain(NotOnOfferHeading);
         prompt.Should().NotContain(ObstacleHeading);
      }

      // The boundary that keeps the whole thing from backfiring: when marriage IS on offer, none of this may
      // appear. An NPC who was eligible and still explained what stands in the way would be talking the player
      // out of the very thing the game had just opened to them.
      [Test]
      public void GIVEN_marriage_is_on_offer_WHEN_built_THEN_no_obstacle_guidance_appears_at_all()
      {
         string prompt = Build(new EncounterContext {
            LoveMatchEligible = true,
            LoveMatchBlessed = true,
            MarriageBlockedBecause = MarriageBlockReason.None
         });

         prompt.Should().NotContain(NotOnOfferHeading);
         prompt.Should().NotContain(ObstacleHeading);
      }
   }
}

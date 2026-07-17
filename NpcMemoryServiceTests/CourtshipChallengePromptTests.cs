// Code written by Gabriel Mailhot, 16/07/2026.
// Courtship-rivalry duel: a rival suitor for a hero the player is courting intercepts the player and
// challenges them to a duel over her. When that happens the rival IS the main NPC of the conversation, and
// EncounterContext.CourtshipRivalLadyName (the courted hero's name) tells PromptBuilder.AppendCourtshipChallenge
// to have him OPEN the scene by declaring the challenge in his own voice. Null in an ordinary conversation, so
// the block must fire only when the field is actually set, never intrude on a normal chat.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CourtshipChallengePromptTests
   {
      private const string Header = "COURTSHIP CHALLENGE";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildPrompt(EncounterContext context)
         => new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // STAKE: without the lady's name in the block, the rival cannot tell the player who they are actually
      // fighting over, and the challenge reads as generic aggression rather than a courtship duel.
      [Test]
      public void GIVEN_a_rival_suitor_challenge_WHEN_building_the_prompt_THEN_the_challenge_header_and_lady_name_are_injected()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CourtshipRivalLadyName = "Ira"};

         string prompt = BuildPrompt(context);

         prompt.Should().Contain(Header);
         prompt.Should().Contain("Ira");
      }

      // STAKE: an ordinary chat must never have an NPC randomly demand a duel over a lady who was never named.
      [Test]
      public void GIVEN_an_ordinary_conversation_WHEN_building_the_prompt_THEN_the_challenge_header_is_absent()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CourtshipRivalLadyName = null};

         string prompt = BuildPrompt(context);

         prompt.Should().NotContain(Header);
      }

      // STAKE: without this instruction the model could narrate the duel's outcome itself (declaring who wins)
      // instead of leaving it to the actual fight on the field and the player's own choice to accept.
      [Test]
      public void GIVEN_a_rival_suitor_challenge_WHEN_building_the_prompt_THEN_it_instructs_issuing_the_challenge_only()
      {
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CourtshipRivalLadyName = "Ira"};

         string prompt = BuildPrompt(context);

         prompt.Should().Contain("You ISSUE the challenge only");
         prompt.Should().Contain("VARY the framing to your own nature");
      }
   }
}

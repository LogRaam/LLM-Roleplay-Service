// Code written by Gabriel Mailhot, 08/03/2026.
// Pins the grammar-hardening added to STAY WITHIN WHAT YOU KNOW (Partie 10.1 of COUNCIL_ACTIONS.md, cause 1): a
// weak model can narrate an outcome in prose (coin changing hands, a gift given, someone joining) and skip the
// [ACTION] tag entirely, leaving the deed unregistered while the reply reads as if it happened. The rule now
// spells out, in the same directive, that the tag is mandatory in the SAME reply and that prose alone is inert.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AntiEmptyPromiseGrammarTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build()
      {
         var builder = new PromptBuilder();

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10});
      }

      // The core hardening: a deed the game can enact only happens when its [ACTION] tag ships in the same
      // reply, so a weaker model cannot narrate the outcome in prose and consider the deed done.
      [Test]
      public void GIVEN_the_stay_within_what_you_know_rule_WHEN_built_THEN_it_ties_any_enactable_deed_to_the_same_reply_action_tag()
      {
         string prompt = Build();

         prompt.Should().Contain("happens ONLY when you");
         prompt.Should().Contain("emit its [ACTION]");
         prompt.Should().Contain("tag in the SAME reply");
      }

      // Companion half: prose describing the deed (coin changing hands, a gift given, someone joining) without
      // the matching [ACTION] must not be treated by the model as having occurred later in the conversation.
      [Test]
      public void GIVEN_the_stay_within_what_you_know_rule_WHEN_built_THEN_prose_without_the_matching_action_is_declared_inert()
      {
         string prompt = Build();

         prompt.Should().Contain("Prose alone changes nothing in the world");
         prompt.Should().Contain("the player receives nothing and you must not later speak as though it");
      }
   }
}

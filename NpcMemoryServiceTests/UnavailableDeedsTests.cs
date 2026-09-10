// Code written by Gabriel Mailhot, 09/09/2026.
// The twin of ExtraActionTeachingsTests, for the half that was missing until today.
//
// Gabriel's ruling, 09/09/2026: "Le LLM declare, et un gating peut ensuite confirmer ou refuter. C'est la qu'il
// peut y avoir une situation frustrante pour le joueur. Il faudrait que le LLM declare en connaissant deja le
// gating." A verb had two states in the prompt, TAUGHT in full or ABSENT, and absence does not read to a language
// model as "forbidden": it reads as "not mentioned", so the model supplies the permission itself and the player
// meets the contradiction one deed later, as a system message refusing what the NPC just promised.
//
// So a deed that is shut now travels in its own field, rendered in the same breath as the deeds that are open.
// Its own field, and not appended to ExtraActionTeachings, for a reason worth stating here because it is exactly
// the kind of thing a later refactor would undo: the host reads that string's emptiness to mirror WHICH verbs
// were taught this turn (TaughtActionFacts.CourtVerbsOffered), so folding refusals into it would report the
// extended verb set as taught on precisely the turns it was refused, and the action audit would cry wolf.
//
// If these tests fail, either a shut deed has stopped reaching the model, or it has started eating a small
// model's prompt budget.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class UnavailableDeedsTests
   {
      private const string Marker = "You cannot lend them soldiers: UNIQUE_TEST_MARKER";
      private const string TeachingMarker = "type: give_influence UNIQUE_TEACHING_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context)
         => new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The point of the whole mechanism: what the NPC cannot do reaches the model BEFORE it answers, verbatim,
      // because the host composed it (DeedUnavailabilityPolicy owns the voice) and this only places it.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_a_deed_is_shut_THEN_the_model_is_told_before_it_answers()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, UnavailableDeeds = Marker}).Should().Contain(Marker);
      }

      // Held to the same budget rule as the teachings it mirrors. A small model gets the minimal action contract
      // and nothing else; withholding the teachings while keeping their refusals would be the worst of both,
      // spending the tight budget to forbid verbs that model was never taught in the first place.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_a_deed_is_shut_THEN_it_is_omitted_exactly_as_the_teachings_are()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Lean, UnavailableDeeds = Marker, ExtraActionTeachings = TeachingMarker
         };

         string prompt = Build(context);

         prompt.Should().NotContain(Marker).And.NotContain(TeachingMarker);
      }

      // Silence is the ordinary case and must cost nothing: most conversations shut no deed worth naming, and a
      // header rendered over an empty list would be tokens spent on every turn of every conversation.
      [Test]
      public void GIVEN_nothing_shut_WHEN_building_a_full_prompt_THEN_not_one_character_is_spent()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full}).Should().NotContain(Marker);
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, UnavailableDeeds = "   "}).Should().NotContain(Marker);
      }

      // Open and shut must be read together. A refusal rendered far from the teachings would leave the model
      // holding a permission in one place and its denial in another, which is the arithmetic-at-a-distance
      // failure the intimacy consent verdict was written to end in July.
      [Test]
      public void GIVEN_both_a_taught_verb_and_a_shut_deed_WHEN_building_THEN_the_refusal_follows_the_teaching()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full, ExtraActionTeachings = TeachingMarker, UnavailableDeeds = Marker
         };

         string prompt = Build(context);

         prompt.Should().Contain(TeachingMarker).And.Contain(Marker);
         prompt.IndexOf(Marker, System.StringComparison.Ordinal)
               .Should().BeGreaterThan(prompt.IndexOf(TeachingMarker, System.StringComparison.Ordinal));
      }

      // The two fields stay independent in both directions: a conversation can shut a deed while teaching none,
      // which is in fact the common shape of the bug this answers (the NPC was never taught the verb, and
      // promised it anyway).
      [Test]
      public void GIVEN_a_shut_deed_and_no_teaching_at_all_WHEN_building_THEN_the_refusal_still_reaches_the_model()
      {
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full, UnavailableDeeds = Marker})
            .Should().Contain(Marker);
      }
   }
}

// Code written by Gabriel Mailhot, 20/08/2026.
// New companion-audience reason: AMBITION (mirrors the Gratitude/SchemeWarning pattern, the freshest bespoke
// found-topic reasons). Design brief: a content, ambitious companion seeks a private word to voice ONE concrete
// ambition of their own (a command to lead, a post in the party, a fief to hold, or the player's blessing to
// wed), bound to an existing action verb, so there is NO new executor. Unlike SchemeWarning, granting the
// audience does NOT resolve anything by itself: it is the PLAYER's deed through the normal action machinery, so
// the prompt must NOT tell the model to suppress actions here, the opposite of SchemeWarning's own rule.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CompanionAudienceAmbitionPromptTests
   {
      private const string RetireAction = "type: retire";
      private const string WornYouDown = "worn you down";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Nethor",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The core of the design brief: Ambition must read as an earnest, respectful ask, not an invitation to
      // resign, an ultimatum, or a grievance. Without this the prompt could reuse the generic retirement or
      // found-topic machinery and let the companion drift into threatening to leave, which the brief rules out.
      [Test]
      public void GIVEN_an_ambition_audience_WHEN_building_the_prompt_THEN_it_is_framed_as_an_earnest_ask()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.Ambition};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("EARNEST ASK the player is free to grant or refuse");
         prompt.Should().Contain("NEVER a threat, an");
      }

      // The design brief, verbatim: unlike SchemeWarning, "do NOT tell the model to emit nothing", a granted
      // ambition must resolve through the NORMAL action pipeline. Without this line the model could be left
      // believing it must stay silent on the deed even once the player has clearly granted it.
      [Test]
      public void GIVEN_an_ambition_audience_WHEN_building_the_prompt_THEN_a_granted_ambition_is_told_to_let_the_normal_action_follow()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.Ambition};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("let the");
         prompt.Should().Contain("fitting action for that deed follow exactly as it normally would once agreed");
      }

      // The design brief's own guardrail against SchemeWarning's suppression pattern: Ambition must NEVER be
      // taught "do not emit any action for this", or a granted ambition would silently fail to fire its verb.
      [Test]
      public void GIVEN_an_ambition_audience_WHEN_building_the_prompt_THEN_no_action_is_suppressed()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.Ambition};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain("Do not emit any action for this");
      }

      // Mirrors the exact regression CompanionAudienceFoundTopicPromptTests guards for the other found-topics
      // (Nethor, devoted+fresh, wrongly taught to retire): an Ambition audience must never fall through into the
      // retirement surface either, now that it has its own dedicated branch ahead of that fallback.
      [Test]
      public void GIVEN_an_ambition_audience_WHEN_building_the_prompt_THEN_no_retirement_language_is_taught()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.Ambition};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(RetireAction);
         prompt.Should().NotContain(WornYouDown);
      }
   }
}

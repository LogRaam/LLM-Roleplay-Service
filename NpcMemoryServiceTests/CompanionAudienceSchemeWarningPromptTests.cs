// Code written by Gabriel Mailhot, 20/08/2026.
// New companion-audience reason: SCHEME WARNING. Design brief (mirrors the Gratitude pattern, the freshest
// bespoke found-topic reason): a loyal companion in the party seeks a private word to WARN the player that
// someone is secretly plotting against them (SchemeStore's own ledger), and granting the audience HEEDS the
// warning for real (the scheme is exposed and its real consequences apply, via SchemeStore.TargetHeeds). It is
// a warning born of loyalty, never a request or a bargain, and emits no action tag: the mechanical heed is
// applied by the mod on grant, not by the LLM.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CompanionAudienceSchemeWarningPromptTests
   {
      private const string RetireAction = "type: retire";
      private const string WornYouDown = "worn you down";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Nethor",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The core of the design brief: SchemeWarning must read as an urgent, protective WARNING, not an invitation
      // to resign, request something, or bargain. Without this the prompt could reuse the generic retirement or
      // found-topic machinery and let the companion drift into asking for something, which the brief rules out.
      [Test]
      public void GIVEN_a_scheme_warning_audience_WHEN_building_the_prompt_THEN_it_is_framed_as_an_urgent_warning()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.SchemeWarning};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("warning them yourself, urgently and protectively");
         prompt.Should().Contain("moving secretly against the player");
      }

      // The whole point of the reason (design brief, verbatim): "asks for nothing", never a request or a bargain,
      // exactly like Gratitude's own guardrail line.
      [Test]
      public void GIVEN_a_scheme_warning_audience_WHEN_building_the_prompt_THEN_it_explicitly_rules_out_a_request_or_bargain()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.SchemeWarning};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("NEVER a request, a bargain, or a demand");
      }

      // Design brief: the mechanical heed (SchemeStore.TargetHeeds) is applied by the mod on grant, never by the
      // LLM emitting an action tag. Without this the model could invent its own [ACTION] for the warning.
      [Test]
      public void GIVEN_a_scheme_warning_audience_WHEN_building_the_prompt_THEN_no_action_is_taught_for_it()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.SchemeWarning};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("Do not emit any action for this");
      }

      // Mirrors the exact regression CompanionAudienceFoundTopicPromptTests guards for the other found-topics
      // (Nethor, devoted+fresh, wrongly taught to retire): a SchemeWarning audience must never fall through into
      // the retirement surface either, now that it has its own dedicated branch ahead of that fallback.
      [Test]
      public void GIVEN_a_scheme_warning_audience_WHEN_building_the_prompt_THEN_no_retirement_language_is_taught()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.SchemeWarning};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(RetireAction);
         prompt.Should().NotContain(WornYouDown);
      }
   }
}

// Code written by Gabriel Mailhot, 20/08/2026.
// New companion-audience reason: GRATITUDE. Design brief (mirrors the found-topic audience pattern, see
// CompanionAudienceFoundTopicPromptTests for the sibling bug this same AppendCompanionAudience method already
// guards against): a content companion seeks the player out to THANK them for a specific kindness and reaffirm
// their loyalty, purely warm, NEVER a request/bargain/demand (a future "ask for something" reason is deliberately
// separate). Gratitude gets its own bespoke framing, like SecretAffection, rather than the generic found-topic
// stay-on-topic guardrail, because it needs the explicit "never a request or bargain" language the design calls for.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CompanionAudienceGratitudePromptTests
   {
      private const string RetireAction = "type: retire";
      private const string WornYouDown = "worn you down";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Nethor",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The core of the design brief: Gratitude must read as a THANK-YOU, not an invitation to resign, request
      // something, or bargain. Without this the prompt could reuse the generic retirement or found-topic
      // machinery and let the companion drift into asking for something, exactly what the brief rules out.
      [Test]
      public void GIVEN_a_gratitude_audience_WHEN_building_the_prompt_THEN_it_is_framed_as_a_pure_thank_you()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.Gratitude};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("thanking them for it");
         prompt.Should().Contain("reaffirm the loyalty");
      }

      // The whole point of the reason (design brief, verbatim): "NEVER a request, a bargain, or a demand". This
      // is the one line that keeps Gratitude from becoming a disguised way to ask the player for something.
      [Test]
      public void GIVEN_a_gratitude_audience_WHEN_building_the_prompt_THEN_it_explicitly_rules_out_a_request_or_bargain()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.Gratitude};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("NEVER a request, a bargain, or a demand");
      }

      // Mirrors the exact regression CompanionAudienceFoundTopicPromptTests guards for the other found-topics
      // (Nethor, devoted+fresh, wrongly taught to retire): a Gratitude audience must never fall through into the
      // retirement surface either, now that it has its own dedicated branch ahead of that fallback.
      [Test]
      public void GIVEN_a_gratitude_audience_WHEN_building_the_prompt_THEN_no_retirement_language_is_taught()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.Gratitude};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(RetireAction);
         prompt.Should().NotContain(WornYouDown);
      }
   }
}

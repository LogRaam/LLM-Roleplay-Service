// Code written by Gabriel Mailhot, 25/07/2026.
// Player-reported bug (2026-07-25): a found-topic companion audience (a mission report, a rumour, practical
// counsel...) fell through into AppendCompanionAudience's retirement block, so a devoted, fresh companion
// (Nethor: contentment 84 devoted, weariness 0 fresh) was taught by the system prompt that "the long years of
// war have worn you down" and asked to retire with no mechanical basis. Fixed by gating the retirement block to
// CompanionAudienceReason.Retirement only, and giving every other found-topic reason a stay-on-topic guardrail.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CompanionAudienceFoundTopicPromptTests
   {
      private const string RetireAction = "type: retire";
      private const string WornYouDown = "worn you down";
      private const string NotResigning = "NOT resigning";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Nethor",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // The core of the bug: none of the found-topic reasons should ever teach the retirement surface. A
      // devoted, fresh companion opening a private word about a mission, a rumour, or counsel must never be
      // handed language that tells it to claim war-weariness or offer to retire.
      [TestCase(CompanionAudienceReason.MissionFollowUp)]
      [TestCase(CompanionAudienceReason.CompanionConflict)]
      [TestCase(CompanionAudienceReason.PracticalCounsel)]
      [TestCase(CompanionAudienceReason.Callback)]
      [TestCase(CompanionAudienceReason.RumourReport)]
      public void GIVEN_a_found_topic_audience_WHEN_building_the_prompt_THEN_no_retirement_language_is_taught(
         CompanionAudienceReason reason)
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = reason};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().NotContain(RetireAction);
         prompt.Should().NotContain(WornYouDown);
         prompt.Should().Contain(NotResigning);
      }

      // A genuine Retirement audience must still get its own surface: the war-weary framing and the retire
      // action, and none of the found-topic guardrail language (which would blunt a real retirement talk).
      [Test]
      public void GIVEN_a_genuine_retirement_audience_WHEN_building_the_prompt_THEN_the_retirement_surface_is_taught()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            CompanionAudience = CompanionAudienceReason.Retirement,
            AudienceRetirementIsLanded = false
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(RetireAction);
         prompt.Should().Contain(WornYouDown);
         prompt.Should().NotContain(NotResigning);
      }

      // Light guard: a Grievance audience is untouched by the restructure and keeps its own reassure surface,
      // never the retirement one.
      [Test]
      public void GIVEN_a_grievance_audience_WHEN_building_the_prompt_THEN_the_reassure_surface_is_taught_not_retirement()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = CompanionAudienceReason.Grievance};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("reassure_companion");
         prompt.Should().NotContain(RetireAction);
      }
   }
}

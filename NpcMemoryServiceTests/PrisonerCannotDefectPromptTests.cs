// Code written by Gabriel Mailhot, 14/08/2026.
// Player report + owner decision: recruit_prisoner is DELIBERATELY strict (personal regard must reach
// PrisonerRecruitmentPolicy.RegardFloor, and a clan leader is never recruitable). Before this fix, that
// strictness was enforced only on the EXECUTOR side: AppendRecruitPrisoner taught the defection ONLY when
// EncounterContext.CanRecruitPrisoner was true, so a held captive lord below the floor (or the enemy clan's own
// leader) reached the model with NO constraining teaching at all, and the model improvised acceptance, an
// empty promise ("yes, I will join your house") the bridge could never honour. AppendPrisonerCannotDefect
// closes that gap with the symmetric NEGATIVE, taught via EncounterContext.CaptiveLordWithheldFromDefection,
// which the host derives from the SAME verdict as CanRecruitPrisoner so the two can never both be true. These
// tests pin: the negative teaches only when its flag is set, never leaks alongside the positive teaching, and
// respects the same captor-scene / round-table / council suppressions the positive teaching already has.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PrisonerCannotDefectPromptTests
   {
      private const string CannotDefectHeading = "YOU ARE A CAPTIVE LORD, NOT A DEFECTOR:";
      private const string RecruitPrisonerHeading = "SWEARING TO YOUR CAPTOR, A DEFECTION FROM THE CELL YOU MIGHT BE PERSUADED TO:";
      private const string RecruitPrisonerActionType = "type: recruit_prisoner";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context, AdultContentLevel adultLevel = AdultContentLevel.Off)
         => new PromptBuilder {AdultLevel = adultLevel}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The core of the fix: a held captive lord not (yet) eligible for recruit_prisoner must be taught, in
      // character, that they will not claim a defection they cannot make. Without this, the reported bug
      // (an NPC narrating "yes, I will join your house" while nothing happens) has no guardrail at all.
      [Test]
      public void GIVEN_captive_lord_withheld_from_defection_WHEN_built_THEN_the_negative_teaching_is_present()
      {
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive,
            CaptiveLordWithheldFromDefection = true
         };

         Build(context).Should().Contain(CannotDefectHeading);
      }

      // Default off: an ordinary conversation (or a captive who has genuinely earned recruit_prisoner) must
      // not carry the negative teaching, or every prisoner conversation would gain unwarranted "you will not
      // defect" framing.
      [Test]
      public void GIVEN_the_flag_is_not_set_WHEN_built_THEN_the_negative_teaching_is_absent()
      {
         var context = new EncounterContext {PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive};

         Build(context).Should().NotContain(CannotDefectHeading);
      }

      // Mutual exclusivity, positive side: the host derives CanRecruitPrisoner and
      // CaptiveLordWithheldFromDefection from the same verdict, so an ELIGIBLE prisoner (CanRecruitPrisoner
      // true) must show only the positive recruit_prisoner offer, never the negative refusal alongside it.
      [Test]
      public void GIVEN_the_prisoner_is_recruit_eligible_WHEN_built_THEN_only_the_positive_teaching_is_present()
      {
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive,
            CanRecruitPrisoner = true,
            CaptiveLordWithheldFromDefection = false
         };
         string prompt = Build(context);

         prompt.Should().Contain(RecruitPrisonerHeading);
         prompt.Should().Contain(RecruitPrisonerActionType);
         prompt.Should().NotContain(CannotDefectHeading);
      }

      // Mutual exclusivity, negative side: the mirror of the test above. A withheld captive lord must show
      // only the refusal, never the recruit_prisoner offer the model has no gate to honour.
      [Test]
      public void GIVEN_the_prisoner_is_withheld_from_defection_WHEN_built_THEN_only_the_negative_teaching_is_present()
      {
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive,
            CanRecruitPrisoner = false,
            CaptiveLordWithheldFromDefection = true
         };
         string prompt = Build(context);

         prompt.Should().Contain(CannotDefectHeading);
         prompt.Should().NotContain(RecruitPrisonerHeading);
         prompt.Should().NotContain(RecruitPrisonerActionType);
      }

      // Same carve-out AppendRecruitPrisoner already has: the player-as-captor scene (turn_nemesis) owns its
      // own resolution, so the ordinary prisoner-refusal framing must not leak into it.
      [Test]
      public void GIVEN_a_captor_scene_WHEN_built_THEN_the_negative_teaching_is_suppressed()
      {
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive,
            CaptiveLordWithheldFromDefection = true,
            IsCaptorScene = true,
            CaptiveIntent = CaptiveSceneIntent.Interrogation
         };

         Build(context, AdultContentLevel.Hardcore).Should().NotContain(CannotDefectHeading);
      }

      // Same carve-out AppendRecruitPrisoner already has: a council/round-table turn owns its own
      // [RESOLUTION] channel, so this 1:1 refusal framing must not appear there either.
      [Test]
      public void GIVEN_a_round_table_turn_WHEN_built_THEN_the_negative_teaching_is_suppressed()
      {
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive,
            CaptiveLordWithheldFromDefection = true,
            IsRoundTableTurn = true
         };

         Build(context).Should().NotContain(CannotDefectHeading);
      }

      // The council-narrator variant of the same carve-out (IsCouncilNarratorTurn stays alongside
      // IsRoundTableTurn per its own doc, but is checked independently in the guard clause).
      [Test]
      public void GIVEN_a_council_narrator_turn_WHEN_built_THEN_the_negative_teaching_is_suppressed()
      {
         var context = new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive,
            CaptiveLordWithheldFromDefection = true,
            IsRoundTableTurn = true,
            IsCouncilNarratorTurn = true
         };

         Build(context).Should().NotContain(CannotDefectHeading);
      }
   }
}

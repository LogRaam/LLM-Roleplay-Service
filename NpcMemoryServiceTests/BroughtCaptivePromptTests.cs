// Code written by Gabriel Mailhot, 21/08/2026.
// Bring-participants-to-a-captor-scene, increment 2: the player wants to bring ANOTHER of their own held
// prisoners into a captor scene as leverage against the scene's subject ("use Ascyron to pressure Lucon").
// WitnessEntry.IsBroughtCaptive marks such a participant. This file pins the whole discipline the design
// exists to protect: a brought captive is NEVER a free witness (no PROVOKED/PROACTIVE reaction on their own
// standing), NEVER an acting captor (AppendCompanionActingOnCaptive stays companion-only), and NEVER counted
// among the captors a subject is "outnumbered" by (the collective-captive block in
// AppendPlayerCaptorSceneRules). When addressed directly, their own turn (EncounterContext.IsBroughtCaptiveTurn)
// is framed as a coerced captive, not a witness reaction and not companion agency. The dedicated "PRESENT IN
// CHAINS" framing renders only in a captor scene, full prompt, and AdultLevel != Off, mirroring the gates
// CompanionActingOnCaptivePromptTests already pins for the companion-acting counterpart.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class BroughtCaptivePromptTests
   {
      private const string BroughtCaptivesHeading = "PRESENT IN CHAINS: OTHER PRISONERS BROUGHT TO THIS SCENE:";
      private const string BroughtCaptiveTurnHeading = "THIS TURN THE PLAYER HAS ADDRESSED YOU: ANOTHER OF THEIR PRISONERS, BROUGHT AS LEVERAGE:";
      private const string CompanionAgencyHeading = "THIS BEAT IS YOURS: THE PLAYER HAS GIVEN YOU THE PRISONER.";
      private const string OutnumberedHeading = "YOU FACE MORE THAN ONE:";
      private const string PrisonerFramingHeading = "YOU ARE THE PLAYER'S PRISONER:";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Lucon",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static WitnessEntry BroughtCaptive(string name = "Ascyron") => new() {
         Name = name,
         RelationToNpc = "another of the player's prisoners",
         IsBroughtCaptive = true,
         HeroStringId = "ascyron_id"
      };

      private static WitnessEntry Companion(string name = "Aldric") => new() {
         Name = name,
         RelationToNpc = "your companion",
         IsPlayerCompanion = true,
         HeroStringId = "aldric_id"
      };

      private static string Build(EncounterContext context, AdultContentLevel adultLevel = AdultContentLevel.Hardcore)
         => new PromptBuilder {AdultLevel = adultLevel}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The core of the fix: a held prisoner brought into a captor scene as leverage must be named and framed
      // as coerced leverage, distinct from an ordinary witness and from an acting companion.
      [Test]
      public void GIVEN_a_brought_captive_in_a_captor_scene_WHEN_built_THEN_the_framing_is_present()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CaptiveIntent = CaptiveSceneIntent.Interrogation,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         string prompt = Build(context);

         prompt.Should().Contain(BroughtCaptivesHeading);
         prompt.Should().Contain("Ascyron");
      }

      // No brought captive present: the section must render nothing at all, costing no tokens on the far more
      // common captor scene with no leverage prisoner brought along.
      [Test]
      public void GIVEN_no_brought_captive_WHEN_built_THEN_the_framing_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CaptiveIntent = CaptiveSceneIntent.Interrogation,
            Witnesses = new List<WitnessEntry> {Companion()}
         };

         Build(context).Should().NotContain(BroughtCaptivesHeading);
      }

      // Captor-scene-only by contract: a stray IsBroughtCaptive entry outside a captor scene (should never
      // happen game-side) must not render leverage/coercion framing that makes no sense in an ordinary talk.
      [Test]
      public void GIVEN_a_brought_captive_outside_a_captor_scene_WHEN_built_THEN_the_framing_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = false,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(BroughtCaptivesHeading);
      }

      // Lean fallback: a small local model's hard character budget (LeanPromptPolicyTests) must never absorb
      // a captor-scene-only section it will rarely if ever need.
      [Test]
      public void GIVEN_lean_level_WHEN_built_THEN_the_framing_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Lean,
            IsCaptorScene = true,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(BroughtCaptivesHeading);
      }

      // AdultLevel Off: every other captor-scene teaching is withheld at Off, so this new section must not be
      // the one exception that still renders scene content.
      [Test]
      public void GIVEN_adult_level_off_WHEN_built_THEN_the_framing_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context, AdultContentLevel.Off).Should().NotContain(BroughtCaptivesHeading);
      }

      // Never a free witness: the WITNESSES PRESENT role line for a brought captive must mark them as coerced
      // and non-threatening, never bare RelationToNpc text an ordinary lord witness would get.
      [Test]
      public void GIVEN_a_brought_captive_WHEN_built_THEN_their_witness_role_marks_them_non_threatening()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         string prompt = Build(context);

         prompt.Should().Contain("not a free witness, and no threat to you");
      }

      // The whole point of increment 2: bringing a prisoner along must NEVER hand them the acting-captor
      // agency increment 1 built for a real companion.
      [Test]
      public void GIVEN_only_a_brought_captive_WHEN_built_THEN_the_companion_agency_teaching_never_renders()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = false,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(CompanionAgencyHeading);
      }

      // Power balance / "outnumbered" framing: a brought captive is on the CAPTIVE side, so alone (no real
      // companion) they must never make the subject feel outnumbered by "several captors at once".
      [Test]
      public void GIVEN_only_a_brought_captive_in_a_collective_scene_WHEN_built_THEN_the_subject_is_not_outnumbered()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            IsCollectiveCaptiveScene = true,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(OutnumberedHeading);
      }

      // Mirror of the test above: a REAL companion alongside the brought captive must still trigger the
      // outnumbered framing; the fix must narrow the tally, not silently disable it altogether.
      [Test]
      public void GIVEN_a_companion_and_a_brought_captive_in_a_collective_scene_WHEN_built_THEN_the_subject_is_still_outnumbered()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            IsCollectiveCaptiveScene = true,
            Witnesses = new List<WitnessEntry> {Companion(), BroughtCaptive()}
         };

         Build(context).Should().Contain(OutnumberedHeading);
      }

      // A brought captive's OWN turn: addressed directly, they must answer as a coerced captive, not receive
      // the ordinary witness-exchange or companion-acting framing.
      [Test]
      public void GIVEN_the_brought_captive_turn_flag_is_set_WHEN_built_THEN_the_coerced_captive_framing_renders()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            IsBroughtCaptiveTurn = true,
            CaptiveIntent = CaptiveSceneIntent.Interrogation,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().Contain(BroughtCaptiveTurnHeading);
      }

      // Default off: an ordinary turn inside a captor scene must not gain this framing.
      [Test]
      public void GIVEN_the_brought_captive_turn_flag_is_not_set_WHEN_built_THEN_the_coerced_captive_framing_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            IsBroughtCaptiveTurn = false,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(BroughtCaptiveTurnHeading);
      }

      // Never companion agency on a brought captive's own turn: the two teachings must be mutually exclusive.
      [Test]
      public void GIVEN_the_brought_captive_turn_flag_is_set_WHEN_built_THEN_the_companion_agency_teaching_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            IsBroughtCaptiveTurn = true,
            CompanionActingOnCaptive = false,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(CompanionAgencyHeading);
      }

      // Regression guard mirroring CompanionActingOnCaptivePromptTests: AppendPlayerCaptorSceneRules assumes the
      // "npc" being built for IS the scene's own bound subject ("you are bound, disarmed..."), which is wrong
      // for a brought captive's own turn (a different person, not this scene's target). Must be suppressed.
      [Test]
      public void GIVEN_the_brought_captive_turn_flag_is_set_WHEN_built_THEN_the_subject_prisoner_framing_is_suppressed()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            IsBroughtCaptiveTurn = true,
            CaptiveIntent = CaptiveSceneIntent.Interrogation,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(PrisonerFramingHeading);
      }

      // Outside a captor scene the flag must render nothing, even if a caller mistakenly set it: the
      // [DIALOGUE]/[NARRATION] captive framing makes no sense outside a captor scene.
      [Test]
      public void GIVEN_the_brought_captive_turn_flag_is_set_outside_a_captor_scene_WHEN_built_THEN_the_framing_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = false,
            IsBroughtCaptiveTurn = true,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(BroughtCaptiveTurnHeading);
      }

      // Lean fallback must never carry the coerced-captive-turn teaching either.
      [Test]
      public void GIVEN_the_brought_captive_turn_flag_is_set_WHEN_built_at_lean_level_THEN_the_framing_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Lean,
            IsCaptorScene = true,
            IsBroughtCaptiveTurn = true,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context).Should().NotContain(BroughtCaptiveTurnHeading);
      }

      // AdultLevel Off must withhold the coerced-captive-turn teaching too, same as every other captor-scene
      // section.
      [Test]
      public void GIVEN_the_brought_captive_turn_flag_is_set_WHEN_built_at_adult_level_off_THEN_the_framing_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            IsBroughtCaptiveTurn = true,
            Witnesses = new List<WitnessEntry> {BroughtCaptive()}
         };

         Build(context, AdultContentLevel.Off).Should().NotContain(BroughtCaptiveTurnHeading);
      }
   }
}

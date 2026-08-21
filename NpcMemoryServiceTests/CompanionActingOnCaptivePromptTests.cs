// Code written by Gabriel Mailhot, 21/08/2026.
// Bring-participants-to-a-captor-scene, increment 1: a brought companion the player hands the prisoner to (or
// bids act on them) inside a captor scene must not merely react as an ordinary witness would (comment, defer,
// wait on the player): this beat is theirs to ACT and DESCRIBE, in their own voice. EncounterContext.
// CompanionActingOnCaptive (set by the mod from the addressed-speaker routing plus the captor-scene fact) drives
// AppendCompanionActingOnCaptive. These tests pin: the teaching renders only when the flag is set, respects the
// AdultLevel gate (explicit only at Hardcore), never leaks into the Lean fallback (LeanPromptPolicyTests guards
// that budget separately), and does not let the primary captive's own "you are the prisoner" framing
// (AppendPlayerCaptorSceneRules) contradict it on the same turn.

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
   public class CompanionActingOnCaptivePromptTests
   {
      private const string AgencyHeading = "THIS BEAT IS YOURS: THE PLAYER HAS GIVEN YOU THE PRISONER.";
      private const string PrisonerFramingHeading = "YOU ARE THE PLAYER'S PRISONER:";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Aldric",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static List<WitnessEntry> OneCompanionWitness() => new() {
         new WitnessEntry {Name = "Aldric", RelationToNpc = "your companion", IsPlayerCompanion = true}
      };

      private static string Build(EncounterContext context, AdultContentLevel adultLevel = AdultContentLevel.Hardcore)
         => new PromptBuilder {AdultLevel = adultLevel}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The core of the fix: a companion the player has handed the prisoner to must be told, explicitly, to act
      // and describe their own deed rather than merely comment from the sidelines.
      [Test]
      public void GIVEN_the_flag_is_set_in_a_captor_scene_WHEN_built_THEN_the_agency_teaching_is_present()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = true,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         Build(context).Should().Contain(AgencyHeading);
      }

      // Default off: an ordinary witness turn, even inside a captor scene, must not gain this agency framing,
      // only the addressed companion this turn was actually handed the prisoner.
      [Test]
      public void GIVEN_the_flag_is_not_set_WHEN_built_THEN_the_agency_teaching_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = false,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         Build(context).Should().NotContain(AgencyHeading);
      }

      // The flag is documented as captor-scene-only; an ordinary conversation must never carry it even if a
      // caller mistakenly set it, since the physical-act/[NARRATION] framing makes no sense outside a scene.
      [Test]
      public void GIVEN_the_flag_is_set_outside_a_captor_scene_WHEN_built_THEN_the_agency_teaching_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = false,
            CompanionActingOnCaptive = true,
            Witnesses = OneCompanionWitness()
         };

         Build(context).Should().NotContain(AgencyHeading);
      }

      // The physical-act discipline: whatever the companion does to the prisoner belongs in [NARRATION], never
      // [DIALOGUE], mirroring the captor's own rule instead of inventing a second, looser one.
      [Test]
      public void GIVEN_the_flag_is_set_WHEN_built_THEN_physical_acts_are_routed_to_narration()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = true,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         Build(context).Should().Contain("belongs in [NARRATION]");
      }

      // AdultLevel gate, Hardcore side: explicit description only unlocks once the human player has opted in,
      // exactly like every other captive/CNC scene in the prompt.
      [Test]
      public void GIVEN_hardcore_WHEN_built_THEN_explicit_acts_are_permitted()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = true,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         string prompt = Build(context, AdultContentLevel.Hardcore);

         prompt.Should().Contain("explicit acts included");
         prompt.Should().Contain("SCENE FRAMING: CONSENSUAL NON-CONSENT (CNC):");
      }

      // AdultLevel gate, below-Hardcore side: the companion must be held to the same non-explicit ceiling as
      // every other captive scene below Hardcore, never a looser one just because a companion is acting.
      [Test]
      public void GIVEN_below_hardcore_WHEN_built_THEN_the_teaching_stays_non_explicit()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = true,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         string prompt = Build(context, AdultContentLevel.Mature);

         prompt.Should().Contain(AgencyHeading);
         prompt.Should().Contain("Keep it non-explicit at this content level");
         prompt.Should().NotContain("explicit acts included");
         prompt.Should().NotContain("SCENE FRAMING: CONSENSUAL NON-CONSENT (CNC):");
      }

      // AdultLevel Off: every other captor-scene teaching is withheld at Off (AppendIntimacyConsentRules' own
      // top gate), so the companion's turn must not become the one exception that still renders scene content.
      [Test]
      public void GIVEN_adult_level_off_WHEN_built_THEN_the_agency_teaching_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = true,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         Build(context, AdultContentLevel.Off).Should().NotContain(AgencyHeading);
      }

      // Lean fallback: a captor-scene-only, full-prompt section must never eat into the small local model's
      // hard character budget (LeanPromptPolicyTests guards that budget on the always-on lean path separately).
      [Test]
      public void GIVEN_lean_level_WHEN_built_THEN_the_agency_teaching_is_absent()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Lean,
            IsCaptorScene = true,
            CompanionActingOnCaptive = true,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         Build(context).Should().NotContain(AgencyHeading);
      }

      // The regression this whole feature could otherwise cause: AppendPlayerCaptorSceneRules assumes the
      // "npc" being built for IS the bound prisoner ("you are bound, disarmed..."), which is flatly wrong for a
      // companion's own turn. When CompanionActingOnCaptive is set, that framing must be suppressed so the two
      // sections never contradict each other in the same prompt.
      [Test]
      public void GIVEN_the_flag_is_set_WHEN_built_THEN_the_prisoner_framing_is_suppressed()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = true,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         Build(context).Should().NotContain(PrisonerFramingHeading);
      }

      // Mirror of the test above: the ordinary captive's own turn (flag NOT set) must keep receiving its usual
      // "you are the prisoner" framing: the new suppression must not swallow the whole scene for everyone.
      [Test]
      public void GIVEN_the_flag_is_not_set_WHEN_built_THEN_the_prisoner_framing_still_renders()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            IsCaptorScene = true,
            CompanionActingOnCaptive = false,
            CaptiveIntent = CaptiveSceneIntent.Domination,
            Witnesses = OneCompanionWitness()
         };

         Build(context).Should().Contain(PrisonerFramingHeading);
      }
   }
}

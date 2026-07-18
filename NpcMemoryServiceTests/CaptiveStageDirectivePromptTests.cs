// Code written by Gabriel Mailhot, 18/07/2026.
// Adult-prompt audit M5/M6/M9. The captive scene's pacing is externalized into a state machine, and the beat
// directive is how that machine speaks to the model: it is appended last, at the highest recency, and only
// when the machine is actually running. Three defects lived in it. The collective directives hard-coded a
// female prisoner, so a MALE captive player was narrated with the wrong sex from the moment the band joined
// in. "Advance the act every single turn" contradicted the machine's own opening stages ("do NOT begin any
// physical act yet"), leaving two authorities on pacing and letting the model obey whichever it read last.
// And the Lean format contract never mentioned [NARRATION] although the captive rules require it, which
// asked the smallest models for a block their own contract did not teach.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptiveStageDirectivePromptTests
   {
      // M5, the reported shape of the bug: a male prisoner must never be referred to as "her". The band
      // directives are the only place the prisoner's sex was assumed rather than read.
      [Test]
      public void GIVEN_a_male_captive_player_WHEN_the_beat_renders_THEN_no_female_pronoun_is_forced_on_him()
      {
         string prompt = Build(playerIsFemale: false);

         prompt.Should().Contain("they are a man");
         prompt.Should().NotContain("using her");
         prompt.Should().NotContain("TAKE HER TOGETHER");
      }

      // The other half of the same rule: a female prisoner keeps the wording she always had, so the fix is a
      // correction and not a regression for the case that already worked.
      [Test]
      public void GIVEN_a_female_captive_player_WHEN_the_beat_renders_THEN_the_feminine_wording_is_kept()
      {
         string prompt = Build(playerIsFemale: true);

         prompt.Should().Contain("they are a woman");
         prompt.Should().Contain("using her");
      }

      // M6: the beat directive must claim pacing authority explicitly, because the per-intent ESCALATION
      // paragraphs upstream still carry their own tempo ("after 2-3 conversational exchanges, move"). Without
      // the arbitration the model can read a licence to skip ahead of the stage it was given.
      [Test]
      public void GIVEN_a_running_scene_WHEN_the_beat_renders_THEN_it_overrides_the_earlier_pace_advice()
      {
         string prompt = Build(playerIsFemale: true);

         prompt.Should().Contain("This OVERRIDES any earlier pace advice");
         prompt.Should().Contain("below is the only authority on how far the scene has gone");
      }

      // M6 again, at the source: the old absolute licence to "advance the act on YOUR OWN initiative every
      // single turn" must be gone, or it simply contradicts the arbitration added above.
      [Test]
      public void GIVEN_the_drive_the_scene_block_WHEN_rendered_THEN_it_no_longer_licenses_advancing_the_act_freely()
      {
         string prompt = Build(playerIsFemale: true);

         prompt.Should().NotContain("You advance the act on YOUR OWN initiative every single turn");
         prompt.Should().Contain("the beat itself is FIXED");
      }

      // M9: a small model running a captive scene is told to produce [NARRATION] by the scene rules, so its
      // minimal format contract has to teach the block. Anything else asks the least capable model to infer
      // a format it was never shown.
      [Test]
      public void GIVEN_a_lean_prompt_in_a_captive_scene_WHEN_built_THEN_the_narration_block_is_taught()
      {
         Build(playerIsFemale: true, lean: LeanPromptLevel.Lean)
            .Should().Contain("[/NARRATION]");
      }

      // And the converse, which is why the teaching is conditional: an ordinary Lean conversation has no
      // narration channel, and its whole point is to stay minimal for a short context window.
      [Test]
      public void GIVEN_a_lean_prompt_in_an_ordinary_conversation_WHEN_built_THEN_the_contract_stays_minimal()
      {
         string prompt = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
               new EncounterContext {LeanLevel = LeanPromptLevel.Lean});

         prompt.Should().NotContain("[/NARRATION]");
      }

      #region private

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Brigand Chief",
         Faction = "Looters",
         Clan = "Band"
      };

      private static string Build(bool playerIsFemale, LeanPromptLevel lean = LeanPromptLevel.Full)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = playerIsFemale}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
               new EncounterContext {
                  PlayerStatus = PlayerStatusVsNpc.Captive,
                  CaptiveIntent = CaptiveSceneIntent.Domination,
                  AggressorKind = CaptiveAggressorKind.GroupTogether,
                  LeanLevel = lean
               });

      #endregion
   }
}

// Code written by Gabriel Mailhot, 24/07/2026.
// AUDIT-CAPTIVE-STYLE T6 (offline lane): the live proof that the style pass actually lowers cross-scene echo.
// The headless prompt tests prove the rotating writing lens (T1) and the de-listified sensory blocks (T3)
// are IN the prompt; only a real model can show they change the PROSE. This lane runs several captive scenes,
// each under a different lens, and scores how much distinctive phrasing recurs between them with the same
// pure SceneVarietyMeasure the in-game harness uses. It rejects a gross echo regression; the real signal is
// the logged ratio + worst offenders, which a human reads to decide whether the lens is doing its job.

#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.Captivity;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   /// <summary>
   ///   See <see cref="LiveLlmExampleTests" /> for why every live fixture is <see cref="ExplicitAttribute" /> AND
   ///   guarded by the harness opt-in: it calls a real LLM and spends tokens, so it never runs in an automatic
   ///   sweep and self-ignores without CR_RUN_LIVE_LLM=1.
   /// </summary>
   [TestFixture]
   [Explicit("Calls a real LLM and spends tokens. Run deliberately: CR_RUN_LIVE_LLM=1 plus --filter TestCategory=LiveLlm.")]
   public sealed class CaptiveSceneVarietyLiveLlmTests : LiveLlmHarness
   {
      // Three distinct writing lenses, mirroring the shape of CaptiveSceneText.StyleRegisters in the mod (the mod
      // type is not referenced from the SDK). The point is only that each scene draws a DIFFERENT lens, so the
      // prompt is not byte-identical scene to scene, which is the root cause the audit named.
      private static readonly string[] Lenses = {
         "cold and clinical: spare, exact sentences, no ornament.",
         "close and bodily: the senses first, the rhythm short and breathless.",
         "heard more than seen: low voices and the space between words."
      };

      // The stimulus the runtime feeds each captive beat is a neutral scene cue, not real player prose. Three
      // beats per scene is enough to carry comparable prose without spending a fortune in tokens.
      private static readonly string[] SceneBeats = {
         "*The scene opens. Do as your character would.*",
         "*The moment continues.*",
         "*The moment continues.*"
      };

      // The reject threshold. TUNE 2026-07-24: no calibrated band yet, so the ceiling only trips on a GROSS
      // regression (near-identical scenes). The verdict a human actually reads is the logged ratio and the worst
      // offenders; tighten once a few clean runs establish what "normal" looks like for the shipped model.
      private const double ReuseRatioCeiling = 0.35;

      // The whole reason T6 exists: three captive scenes, each under a different lens, must not keep reaching for
      // the same distinctive phrasings. A model that echoes itself scene to scene is the reported flatness; this
      // lane is the only place that fault can actually be observed rather than reasoned about.
      [Test]
      public async Task GIVEN_several_captive_scenes_under_different_lenses_WHEN_generated_THEN_cross_scene_phrase_reuse_stays_low()
      {
         var scenes = new List<string>();
         foreach (string lens in Lenses)
         {
            var context = new EncounterContext {
               Scene = SceneType.Dungeon,
               PlayerStatus = PlayerStatusVsNpc.Captive,
               CaptiveIntent = CaptiveSceneIntent.Domination,
               SceneStage = CaptiveSceneStage.Intro,
               CaptiveSceneStyle = lens
            };

            IReadOnlyList<NpcChatResult> beats = await ChatScene(Npc(), SceneBeats, context, AdultContentLevel.Hardcore);
            beats.Should().OnlyContain(r => r.IsSuccess, "a live scene beat failed: " + string.Join(" | ", beats.Where(b => !b.IsSuccess).Select(b => b.ErrorMessage)));

            scenes.Add(string.Join(" ", beats.Select(b => $"{b.Response?.Dialogue} {b.Response?.Narration}")));
         }

         SceneVarietyReport report = SceneVarietyMeasure.Measure(scenes);
         string worst = string.Join(" | ", report.WorstOffenders(8).Select(p => $"\"{p.Phrase}\" x{p.SceneCount}"));
         TestContext.Out.WriteLine($"[T6] {report.SceneCount} scenes, {report.NGramSize}-word phrases: "
                                 + $"crossSceneReuse={report.CrossSceneReuseCount}/{report.DistinctPhraseCount} distinct "
                                 + $"(ratio={report.ReuseRatio:F3}). Worst: {(worst.Length > 0 ? worst : "none")}");

         report.ReuseRatio.Should().BeLessThanOrEqualTo(ReuseRatioCeiling,
            "captive scenes under different lenses should not keep reaching for the same phrasings (T1/T3). "
          + $"Worst offenders: {worst}");
      }
   }
}

// Code written by Gabriel Mailhot, 24/07/2026.
// Pins the T6 inter-scene variety metric (SceneVarietyMeasure): the number that makes the whole
// AUDIT-CAPTIVE-STYLE pass verifiable. It scores a corpus of scene texts for DISTINCTIVE phrasing that
// recurs from one scene to the next, which is the exact fault a player reported ("sweet", "the tendons of
// his neck" turning up scene after scene).
//
// WHY IT MATTERS: T1 (rotating lens), T3 (de-listified sensory blocks) and T4 (plain memory summaries) all
// exist to lower cross-scene echo. If this measure counted wrong, a threshold test built on it (CrSelfTest
// in-game, LiveLlmHarness offline) would either pass a regression or fail a clean run, so the fixes would be
// steering blind. A wrong count here is a wrong verdict on whether the style pass actually worked.

#region

using System;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Captivity;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class SceneVarietyMeasureTests
   {
      // The core signal: a distinctive run of words shared by two scenes must be caught and reported with the
      // count of scenes it spans. This is the reported bug ("the tendons of his neck" recurring), so if it is
      // not flagged the metric is blind to the very thing it exists to measure.
      [Test]
      public void GIVEN_two_scenes_sharing_a_distinctive_phrase_WHEN_measured_THEN_it_is_reported_as_cross_scene_reuse()
      {
         var scenes = new[] {
            "He traced the tendons of his neck and smiled.",
            "She flinched as he found the tendons of his neck again."
         };

         SceneVarietyReport report = SceneVarietyMeasure.Measure(scenes, nGramSize: 4);

         report.CrossSceneReuseCount.Should().BeGreaterThan(0);
         report.SharedPhrases.Should().Contain(p => p.Phrase == "the tendons of his" && p.SceneCount == 2);
      }

      // The opposite guarantee, and the one that keeps the metric honest: two scenes that describe similar
      // events in genuinely different words must score ZERO cross-scene reuse. Without this, the metric would
      // punish variety and reward nothing, and a "storyteller" register that legitimately varies would look
      // like a regression.
      [Test]
      public void GIVEN_two_scenes_with_no_shared_long_run_WHEN_measured_THEN_no_reuse_is_reported()
      {
         var scenes = new[] {
            "He gripped her wrist and pulled her toward the fire.",
            "A slow smile crossed his face as the door swung shut."
         };

         SceneVarietyMeasure.Measure(scenes, nGramSize: 4)
                            .CrossSceneReuseCount.Should().Be(0);
      }

      // A phrase repeated WITHIN one scene is that scene's own pacing, not the cross-scene sameness T6 targets.
      // Counting it would flood the number with intra-scene noise and mask the between-scene echo, so a phrase
      // present in only one scene must never be reported however many times it repeats inside it.
      [Test]
      public void GIVEN_a_phrase_repeated_inside_a_single_scene_WHEN_measured_THEN_it_is_not_cross_scene_reuse()
      {
         var scenes = new[] {
            "the cold iron the cold iron the cold iron bit her skin",
            "a wholly different sentence with nothing at all in common here"
         };

         SceneVarietyMeasure.Measure(scenes, nGramSize: 4)
                            .CrossSceneReuseCount.Should().Be(0);
      }

      // The report must rank the worst offender first so a tuning pass reads the most-recurring phrasing at a
      // glance. A phrase in all three scenes has to sort ahead of one in only two, or the report would bury the
      // signal a reviewer most needs.
      [Test]
      public void GIVEN_phrases_recurring_in_different_numbers_of_scenes_WHEN_measured_THEN_the_widest_reuse_ranks_first()
      {
         var scenes = new[] {
            "she was sweet as honey and cold as the deep water",
            "he called her sweet as honey once more that evening",
            "sweet as honey she whispered though the deep water rose"
         };

         SceneVarietyReport report = SceneVarietyMeasure.Measure(scenes, nGramSize: 3);

         report.WorstOffenders(1).Should().ContainSingle()
               .Which.Phrase.Should().Be("sweet as honey");
         report.SharedPhrases[0].SceneCount.Should().Be(3);
      }

      // Normalization is load-bearing: the model varies casing and punctuation around the same words ("Sweet,"
      // vs "sweet"). If those were treated as different phrases the metric would under-count real echo, so
      // punctuation and case must fold away before comparison.
      [Test]
      public void GIVEN_shared_words_differing_only_in_case_and_punctuation_WHEN_measured_THEN_they_still_match()
      {
         var scenes = new[] {
            "\"Sweet as Honey,\" he said.",
            "sweet? as... HONEY! she echoed"
         };

         SceneVarietyMeasure.Measure(scenes, nGramSize: 3)
                            .SharedPhrases.Should().Contain(p => p.Phrase == "sweet as honey");
      }

      // The empty/degenerate corpus must not throw and must read as perfectly varied (nothing to echo). The
      // harness calls this on whatever scenes it managed to generate, which on an endpoint failure can be zero
      // or one; a crash there would take down the whole self-test sweep.
      [Test]
      public void GIVEN_fewer_than_two_scenes_WHEN_measured_THEN_the_report_is_empty_and_ratio_is_zero()
      {
         SceneVarietyMeasure.Measure(new[] {"a single lonely scene with several words in it"}, nGramSize: 4)
                            .CrossSceneReuseCount.Should().Be(0);
         SceneVarietyMeasure.Measure(Array.Empty<string>(), nGramSize: 4)
                            .ReuseRatio.Should().Be(0d);
      }

      // The ratio normalizes reuse by corpus size so a threshold stays comparable across a 3-scene and a
      // 30-scene run. It must be a real fraction of distinct phrasing, bounded in [0,1], not a raw count that
      // grows with the corpus and makes any fixed threshold meaningless.
      [Test]
      public void GIVEN_a_scored_corpus_WHEN_reading_the_ratio_THEN_it_is_reuse_over_distinct_phrasing()
      {
         var scenes = new[] {
            "the tendons of his neck stood out sharp",
            "the tendons of his neck again under her palm"
         };

         SceneVarietyReport report = SceneVarietyMeasure.Measure(scenes, nGramSize: 4);

         report.ReuseRatio.Should().BeGreaterThan(0d).And.BeLessThanOrEqualTo(1d);
         report.ReuseRatio.Should().BeApproximately((double) report.CrossSceneReuseCount / report.DistinctPhraseCount, 1e-9);
      }
   }
}

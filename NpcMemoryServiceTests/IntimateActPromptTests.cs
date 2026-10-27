// Code written by Gabriel Mailhot, 27/10/2026.
// The gestures a character loves and dislikes reach the prompt at Hardcore only, beside their kinks (Gabriel,
// 26/10/2026: "le LLM a tendance a aimer tous les gestes sexuels que le joueur peut decrire"; a refusal "pourrait etre
// franc, ou encore un malaise"). IntimateAct is saved as an integer, so its ordinals are frozen here. If this is wrong,
// every character still adores whatever the player describes, a disliked gesture is met with feigned pleasure, a lower
// adult level is shown gestures it never asked for, or a save reads one gesture as another.

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
   public class IntimateActPromptTests
   {
      // STAKE: Gabriel's own example: a character who particularly loves oral sex and hates anal sex says so in bed.
      [Test]
      public void GIVEN_a_character_who_loves_oral_and_dislikes_anal_WHEN_the_hardcore_prompt_is_built_THEN_both_are_stated()
      {
         string prompt = Build(AdultContentLevel.Hardcore, new List<IntimateAct> {IntimateAct.OralGiving}, new List<IntimateAct> {IntimateAct.AnalReceiving});

         prompt.Should().Contain("Gestures they particularly love in bed:");
         prompt.Should().Contain("- using their mouth on their partner");
         prompt.Should().Contain("Gestures they dislike:");
         prompt.Should().Contain("- being taken anally");
      }

      // STAKE: "franc, ou encore un malaise": a disliked gesture is refused plainly or met with a readable unease, never
      // with feigned pleasure (NPCs never lie), and pressing past it cools them.
      [Test]
      public void GIVEN_a_disliked_gesture_WHEN_the_prompt_is_built_THEN_it_is_refused_or_met_with_unease_never_feigned()
      {
         string prompt = Build(AdultContentLevel.Hardcore, new List<IntimateAct>(), new List<IntimateAct> {IntimateAct.RoughHandling});

         prompt.Should().Contain("they never pretend to enjoy");
         prompt.Should().Contain("let an unease show that a partner can read");
         prompt.Should().Contain("they cool toward the one who pressed");
      }

      // STAKE: Gabriel (27/10/2026): "une chose qu'elle aime moins": a gesture they are less fond of is met without
      // eagerness, a temperature the player can read, and it is no offence.
      [Test]
      public void GIVEN_a_gesture_they_are_less_fond_of_WHEN_the_hardcore_prompt_is_built_THEN_it_is_met_without_eagerness()
      {
         string prompt = Build(AdultContentLevel.Hardcore, new List<IntimateAct> {IntimateAct.OnTop}, null, new List<IntimateAct> {IntimateAct.DirtyTalk});

         prompt.Should().Contain("Gestures they are less fond of:");
         prompt.Should().Contain("- crude words, said and heard in the act");
         prompt.Should().Contain("go along with, but without eagerness");
      }

      // STAKE: the gestures are Hardcore content: an Explicit game is never told them.
      [Test]
      public void GIVEN_an_explicit_game_WHEN_the_prompt_is_built_THEN_no_gesture_is_named()
      {
         string prompt = Build(AdultContentLevel.Explicit, new List<IntimateAct> {IntimateAct.OralGiving}, new List<IntimateAct> {IntimateAct.AnalReceiving});

         prompt.Should().NotContain("Gestures they particularly love");
         prompt.Should().NotContain("Gestures they dislike");
      }

      // STAKE: a profile from before 27/10/2026 (nothing rolled yet) adds no empty heading.
      [Test]
      public void GIVEN_a_profile_with_no_gestures_rolled_WHEN_the_prompt_is_built_THEN_nothing_is_added()
      {
         string prompt = Build(AdultContentLevel.Hardcore, null, null);

         prompt.Should().NotContain("Gestures they");
      }

      // STAKE: the block sits in every Hardcore prompt and Compact does not drop it; the Lean budget guards build at
      // Mature and cannot see it, so its own size is held here (about 250 tokens for a full set of tastes).
      [Test]
      public void GIVEN_a_character_with_a_full_set_of_tastes_WHEN_the_hardcore_prompt_is_built_THEN_the_block_stays_small()
      {
         var many = new List<IntimateAct> {IntimateAct.Kissing, IntimateAct.OnTop, IntimateAct.HandsAndFingers};
         var two = new List<IntimateAct> {IntimateAct.RoughHandling, IntimateAct.DirtyTalk};
         int with = Build(AdultContentLevel.Hardcore, many, two, new List<IntimateAct> {IntimateAct.Beneath}).Length;
         int without = Build(AdultContentLevel.Hardcore, null, null).Length;

         (with - without).Should().BeLessThan(1100);
      }

      // STAKE: IntimateAct is saved as an integer: inserting or reordering a member would read one gesture as another
      // in every save. New members are appended last.
      [TestCase(IntimateAct.None, 0)]
      [TestCase(IntimateAct.Kissing, 1)]
      [TestCase(IntimateAct.OralGiving, 2)]
      [TestCase(IntimateAct.OralReceiving, 3)]
      [TestCase(IntimateAct.AnalGiving, 4)]
      [TestCase(IntimateAct.AnalReceiving, 5)]
      [TestCase(IntimateAct.OnTop, 6)]
      [TestCase(IntimateAct.Beneath, 7)]
      [TestCase(IntimateAct.FromBehind, 8)]
      [TestCase(IntimateAct.HandsAndFingers, 9)]
      [TestCase(IntimateAct.RoughHandling, 10)]
      [TestCase(IntimateAct.DirtyTalk, 11)]
      public void GIVEN_a_saved_gesture_WHEN_cast_to_int_THEN_its_ordinal_is_frozen(IntimateAct act, int ordinal)
      {
         ((int) act).Should().Be(ordinal);
      }

      // STAKE: a member appended without its frozen ordinal above is caught here.
      [Test]
      public void GIVEN_the_gesture_enum_WHEN_its_members_are_counted_THEN_each_has_its_frozen_ordinal()
      {
         System.Enum.GetValues(typeof(IntimateAct)).Length.Should().Be(12);
      }

      private static string Build(AdultContentLevel level, List<IntimateAct>? loved, List<IntimateAct>? disliked, List<IntimateAct>? lessFond = null)
      {
         var npc = new NpcProfile {
            Id = "npc_test",
            Name = "Test Lady",
            Faction = "Empire",
            Clan = "Pethros",
            ReputationWithPlayer = 30,
            Romantic = new RomanticProfile {
               Status = RomanticStatus.Intimate,
               Orientation = SexualOrientation.BiCurious,
               Preferences = new List<RomanticPreference>(),
               LovedActs = loved,
               DislikedActs = disliked,
               LessFondActs = lessFond
            }
         };

         return new PromptBuilder {AdultLevel = level}.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, new EncounterContext());
      }
   }
}

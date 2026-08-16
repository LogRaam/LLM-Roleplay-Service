// Code written by Gabriel Mailhot, 16/08/2026.
// In a collective captive scene the player projects into it through two things a well-composing but less-explicit
// prose model tends to drop: what the men SAY while using the prisoner (crude praise, insults, goading), and the
// detailed moment each man FINISHES. The collective block now teaches both explicitly so even a restrained model
// delivers the running commentary and the graphic peak. These tests pin that both directives are present in a
// collective scene and absent from a solo one (where there are no "men" to voice or to finish).

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
   public class CaptiveCollectiveEmphasisPromptTests
   {
      // The men falling silent during the act wastes the collective premise: the running commentary of several men
      // enjoying the prisoner out loud is a large part of the humiliation. The block must push talk THROUGH the act.
      [Test]
      public void GIVEN_a_collective_captive_scene_WHEN_building_the_prompt_THEN_the_men_are_told_to_talk_through_the_act()
      {
         string prompt = Build(collective: true);

         prompt.Should().Contain("THEY TALK AS THEY USE THE PRISONER");
      }

      // The graphic finish is what makes the use feel real to the player; a model that fades past it or lets a man
      // "finish and step back" robs the scene of its point. The block must demand each man's climax be depicted.
      [Test]
      public void GIVEN_a_collective_captive_scene_WHEN_building_the_prompt_THEN_each_mans_finish_must_be_depicted()
      {
         string prompt = Build(collective: true);

         prompt.Should().Contain("DEPICT EACH MAN'S FINISH");
      }

      // Gabriel (2026-08-16): a synchronised group climax (all four taking him at once and all spending together)
      // reads as staged, not real. The block must push the men to take turns or overlap but land their finishes at
      // DIFFERENT moments, so the scene stays believable.
      [Test]
      public void GIVEN_a_collective_captive_scene_WHEN_building_the_prompt_THEN_the_finishes_must_be_staggered()
      {
         string prompt = Build(collective: true);

         prompt.Should().Contain("STAGGER THEM, DO NOT SYNCHRONISE");
      }

      // Both directives are about the OTHER men, so a solo captor scene (no witnesses, not collective) must not carry
      // them - there is no one to voice a running commentary or to take a separate turn and finish.
      [Test]
      public void GIVEN_a_solo_captive_scene_WHEN_building_the_prompt_THEN_the_collective_emphasis_is_absent()
      {
         string prompt = Build(collective: false);

         prompt.Should().NotContain("THEY TALK AS THEY USE THE PRISONER");
         prompt.Should().NotContain("DEPICT EACH MAN'S FINISH");
         prompt.Should().NotContain("STAGGER THEM, DO NOT SYNCHRONISE");
      }

      #region private

      private static string Build(bool collective)
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = false};
         var context = new EncounterContext
         {
            LeanLevel = LeanPromptLevel.Full,
            PlayerStatus = PlayerStatusVsNpc.Captive,
            CaptiveIntent = CaptiveSceneIntent.PersonalDesire,
            IsCollectiveCaptiveScene = collective,
            Witnesses = collective
               ? new List<WitnessEntry> {new WitnessEntry {Name = "Dranos", RelationToNpc = "your soldier"}}
               : new List<WitnessEntry>()
         };

         var npc = new NpcProfile {Id = "npc_test", Name = "Susada", Faction = "Empire", Clan = "Osticos"};

         return builder.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      #endregion
   }
}

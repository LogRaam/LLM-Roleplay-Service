// Code written by Gabriel Mailhot, 03/10/2026.
// Lordfadooboo (Nexus, 03/10/2026): Rhagaea, empress, held in an Aserai cell, said "few men of the hills trouble
// themselves with the affairs of empresses held in foreign cells". Her prompt told her no word of the world reaches
// her, and nothing told her the reverse: that the world talks of her. The model generalised "I hear nothing" into
// "nobody hears of me". If these rules are wrong, a captive ruler plays herself as forgotten, or an ordinary captive
// is told the whole realm speaks of him.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class CaptiveNotorietyPromptTests
   {
      // Rhagaea's case, at every prompt size: she hears nothing, and she knows the world hears of her.
      [TestCase(LeanPromptLevel.Full)]
      [TestCase(LeanPromptLevel.Lean)]
      public void GIVEN_a_captive_ruler_WHEN_her_prompt_is_built_THEN_she_knows_her_capture_is_talked_of(LeanPromptLevel level)
      {
         string prompt = Build(new EncounterContext {LeanLevel = level, HeldFor = "for some weeks now", CaptiveNotoriety = "a ruler"});

         prompt.Should().Contain("a ruler taken and held is talked of");
         prompt.Should().Contain("no word of the world has reached you");
      }

      // An ordinary captive keeps the plain cut-off: nobody tells him the realm speaks of him.
      [Test]
      public void GIVEN_an_ordinary_captive_WHEN_his_prompt_is_built_THEN_nothing_says_the_world_talks_of_him()
      {
         Build(new EncounterContext {HeldFor = "for some weeks now"}).Should().NotContain("is talked of");
      }

      private static string Build(EncounterContext context)
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "rhagaea", Name = "Rhagaea", Clan = "Pethros", Faction = "Southern Empire"},
                                                  new WorldState {CurrentDay = 10}, context);
   }
}

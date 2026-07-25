// Code written by Gabriel Mailhot, 24/07/2026.
// AUDIT-CAPTIVE-STYLE T1: a player reported that captive CNC scenes reuse the same words and turns of phrase
// from one scene to the next ("sweet", "the tendons of his neck"). The root cause was a byte-identical system
// prompt each scene, so the model fell back on its most-probable phrasings. The fix is a rotating WRITING LENS
// drawn once per scene (EncounterContext.CaptiveSceneStyle) and rendered by AppendSceneStyleDirective in the
// dynamic tail. These tests pin the two load-bearing properties: the lens reaches the model when a scene set
// one, and it is framed as HOW to write, never WHAT happens (so it cannot fight the scene's own stage/consent
// rules), plus the boundary that an ordinary non-captive turn never sees it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SceneStyleDirectivePromptTests
   {
      private const string Heading = "THE TELLING OF THIS SCENE";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Ulfric",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The feature: a lens the scene drew must reach the model. Without it, the prompt is byte-identical scene
      // to scene, which is the reported cross-scene sameness.
      [Test]
      public void GIVEN_a_scene_with_a_writing_lens_WHEN_built_THEN_the_lens_directive_is_rendered()
      {
         string prompt = Build(new EncounterContext {CaptiveSceneStyle = "cold and clinical: spare, exact sentences."});

         prompt.Should().Contain(Heading);
         prompt.Should().Contain("cold and clinical");
      }

      // The load-bearing boundary: the lens governs PROSE, not content. If it read as changing what happens, it
      // would fight the scene's own stage and consent rules; the directive must say plainly it shapes HOW you
      // write, never what happens or how far it goes.
      [Test]
      public void GIVEN_the_lens_directive_WHEN_built_THEN_it_governs_how_not_what()
      {
         string prompt = Build(new EncounterContext {CaptiveSceneStyle = "heard more than seen: low voices, the space between words."});

         prompt.Should().Contain("its writing lens, not its events");
         prompt.Should().Contain("never WHAT happens or how far it goes");
      }

      // An ordinary turn draws no lens, so the block must be absent: printing it on a non-captive conversation
      // would be noise, and there is nothing to shape.
      [Test]
      public void GIVEN_no_lens_WHEN_built_THEN_the_directive_is_absent()
      {
         Build(new EncounterContext()).Should().NotContain(Heading);
      }
   }
}

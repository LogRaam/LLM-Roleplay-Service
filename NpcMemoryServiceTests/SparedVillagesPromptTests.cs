// Code written by Gabriel Mailhot, 01/11/2026.
// A lord at war with the player who drew to spare the player's villages (DepaHN, 27/10/2026; Gabriel, 01/11/2026) must know it.
// If the prompt is wrong, he threatens to burn what the game will never let him burn, or every enemy claims a mercy he has not shown.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SparedVillagesPromptTests
   {
      private const string Resolve = "you have resolved not to raid or burn their";

      private static string Prompt(bool spares)
      {
         var context = new EncounterContext {SparesPlayerVillages = spares};
         var npc = new NpcProfile {Id = "npc_test", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);
      }

      // STAKE: gating before declaration: the game keeps him from the villages, so he must not threaten them.
      [Test]
      public void GIVEN_a_lord_who_spares_the_villages_WHEN_he_speaks_THEN_he_knows_his_resolve()
      {
         Prompt(true).Should().Contain(Resolve);
      }

      // STAKE: the lord who drew to raid, and every other enemy, promises nothing: NPCs never lie.
      [Test]
      public void GIVEN_a_lord_who_made_no_such_resolve_WHEN_he_speaks_THEN_no_mercy_is_put_in_his_mouth()
      {
         Prompt(false).Should().NotContain(Resolve);
      }
   }
}

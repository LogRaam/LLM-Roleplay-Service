// Code written by Gabriel Mailhot, 16/11/2026.
// What these pin: who is handed the player's parents' real names. The block exists so a character who speaks of the
// player's parents never invents a name (EncounterContext.PlayerFatherName's own doc). VonLemberg (Discord, 16/11/2026,
// "Player Parentage Observer Knowledge"): a stranger who had never heard the player's name was handed them all the same.
// Gabriel (16/11/2026): such a stranger knows nothing of the parents and still invents nothing; everyone else as before.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PlayerParentagePromptTests
   {
      // STAKE: a lord who knows the player still gets the real names, so he never invents a father.
      [Test]
      public void GIVEN_a_speaker_who_knows_the_player_WHEN_the_prompt_is_built_THEN_the_parents_real_names_are_given()
      {
         Build(nameIsNews: false).Should().Contain("these are their real names").And.Contain("father Garios").And.Contain("mother Ilina");
      }

      // STAKE: VonLemberg: a stranger told "You have never heard the name" could still say "your father, Garios".
      [Test]
      public void GIVEN_a_stranger_the_name_is_news_to_WHEN_the_prompt_is_built_THEN_no_parent_is_named_and_none_invented()
      {
         string prompt = Build(nameIsNews: true);

         prompt.Should().NotContain("Garios").And.NotContain("Ilina");
         prompt.Should().Contain("You know nothing of their parents").And.Contain("never invent a name");
      }

      private static string Build(bool nameIsNews)
         => new PromptBuilder {PlayerName = "Arthur"}.BuildSystemPrompt(
            new NpcProfile {Id = "n", Name = "Chief Rolan", Faction = "Sturgia", Clan = "Rolan"}, new WorldState {CurrentDay = 12},
            new EncounterContext {
               Scene = SceneType.Keep, PlayerFatherName = "Garios", PlayerMotherName = "Ilina", PlayerNameIsNewsToSpeaker = nameIsNews
            });
   }
}

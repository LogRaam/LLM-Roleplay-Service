// Code written by Gabriel Mailhot, 14/11/2026.
// What this pins: the text other mods write of a character (the host's character-text door) reaches the prompt, a
// secret as something the character keeps without ever lying about it, and nothing changes when no mod writes anything.
//
// Bhelogan (Discord, 14/11/2026), whose Detective's Notebook wrote its descriptions into character_overrides.json between
// markers: "The per-character text source you described, yes please ... Private: model only, never in the panel ... the
// same for the player (in place of player_description.txt) ... stable within a conversation, so it can sit in the cached
// prefix." If this breaks, a mod's text never reaches the model, a secret is taught as something to tell, or every
// prompt changes for players who run no such mod.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ModCharacterTextPromptTests
   {
      private static string Build(EncounterContext context)
      {
         PromptBuilder real = LeanRealPromptBudgetTests.RealBuilder();
         var builder = new PromptBuilder {
            ActionVocabulary = real.ActionVocabulary, WorldDescription = real.WorldDescription,
            BehaviorGuidelinesOverride = real.BehaviorGuidelinesOverride, AdultLevel = real.AdultLevel, PlayerName = "Dinkle"
         };

         return builder.BuildSystemPrompt(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // THE REPORT: what a mod writes of the character is read by the model.
      [Test]
      public void GIVEN_a_mods_backstory_WHEN_the_prompt_is_built_THEN_the_character_is_told_it_as_their_own()
      {
         string prompt = Build(new EncounterContext {NpcModBackstory = "You mend the kennel run every spring."});

         prompt.Should().Contain("MORE OF WHO YOU ARE").And.Contain("You mend the kennel run every spring.");
      }

      // STAKE: a secret is kept, never denied with a lie (characters never lie).
      [Test]
      public void GIVEN_a_mods_secret_WHEN_the_prompt_is_built_THEN_it_is_kept_without_a_falsehood()
      {
         string prompt = Build(new EncounterContext {NpcModPrivate = "You poisoned your brother."});

         prompt.Should().Contain("WHAT YOU KEEP TO YOURSELF").And.Contain("never deny it with a falsehood").And.Contain("You poisoned your brother.");
      }

      // THE REPORT: "the same for the player": a mod's text of the player sits under THE PLAYER.
      [Test]
      public void GIVEN_a_mods_text_of_the_player_WHEN_the_prompt_is_built_THEN_it_sits_under_the_player()
      {
         string prompt = Build(new EncounterContext {PlayerModText = "A long scar crosses the player's jaw."});

         prompt.IndexOf("A long scar crosses the player's jaw.", System.StringComparison.Ordinal)
               .Should().BeGreaterThan(prompt.IndexOf("THE PLAYER:", System.StringComparison.Ordinal));
      }

      // STAKE: a player with no such mod gets exactly the prompt they had.
      [Test]
      public void GIVEN_no_mod_text_WHEN_the_prompt_is_built_THEN_neither_section_appears()
      {
         string prompt = Build(new EncounterContext());

         prompt.Should().NotContain("MORE OF WHO YOU ARE").And.NotContain("WHAT YOU KEEP TO YOURSELF");
      }
   }
}

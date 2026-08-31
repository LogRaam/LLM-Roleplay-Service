// Witness gender fix (player report 2026-08-31): in a multi-person scene the model guessed each witness's
// sex from their NAME alone and got it wrong, voicing a female companion as "he". WitnessEntry.IsFemale now
// carries the sex beside the name ("- Arwa, a woman (...)"), and a guard line forbids inferring gender from
// a name. Synthetic flavour witnesses (a soldier, an apprentice) have no Hero and stay genderless (null).
// These tests pin the per-name clause, the genderless fallback, and the guard line's presence/absence.

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
   public class WitnessGenderPromptTests
   {
      private const string GuardLine =
         "The gender stated beside each name above is a fact you know. Do not infer anyone's gender from their name.";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(params WitnessEntry[] witnesses)
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            Witnesses = new List<WitnessEntry>(witnesses)
         };

         return new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // The root fix: a female witness is STATED a woman beside her name, so the model never has to guess.
      [Test]
      public void GIVEN_a_female_witness_WHEN_building_the_prompt_THEN_she_is_stated_a_woman()
      {
         string prompt = Build(new WitnessEntry {Name = "Arwa", RelationToNpc = "your companion", IsFemale = true});

         prompt.Should().Contain("- Arwa, a woman (your companion)");
      }

      [Test]
      public void GIVEN_a_male_witness_WHEN_building_the_prompt_THEN_he_is_stated_a_man()
      {
         string prompt = Build(new WitnessEntry {Name = "Derthert", RelationToNpc = "your liege", IsFemale = false});

         prompt.Should().Contain("- Derthert, a man (your liege)");
      }

      // A synthetic flavour witness has no Hero and so no sex: the line keeps the old, genderless rendering
      // rather than minting a guess.
      [Test]
      public void GIVEN_a_witness_without_sex_WHEN_building_the_prompt_THEN_the_line_carries_no_gender_clause()
      {
         string prompt = Build(new WitnessEntry {Name = "A guard", RelationToNpc = "one of your men"});

         prompt.Should().Contain("- A guard (one of your men)");
         prompt.Should().NotContain("- A guard, a woman");
         prompt.Should().NotContain("- A guard, a man");
      }

      // The guard line backs the per-name clause: a stated sex is a fact, never an inference from a name.
      // It appears the moment ONE witness carries a sex, and stays out of an all-genderless room so that
      // prompt is unchanged byte-for-byte.
      [Test]
      public void GIVEN_at_least_one_gendered_witness_WHEN_building_the_prompt_THEN_the_guard_line_is_shown()
      {
         string prompt = Build(
            new WitnessEntry {Name = "Arwa", RelationToNpc = "your companion", IsFemale = true},
            new WitnessEntry {Name = "A guard", RelationToNpc = "one of your men"});

         prompt.Should().Contain(GuardLine);
      }

      [Test]
      public void GIVEN_no_gendered_witness_WHEN_building_the_prompt_THEN_the_guard_line_is_absent()
      {
         string prompt = Build(new WitnessEntry {Name = "A guard", RelationToNpc = "one of your men"});

         prompt.Should().NotContain(GuardLine);
      }
   }
}

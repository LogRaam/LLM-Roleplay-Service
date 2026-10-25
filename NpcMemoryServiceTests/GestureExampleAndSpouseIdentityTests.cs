// Code written by Gabriel Mailhot, 25/10/2026.
// Guards two lines a small local model leans on. Eisendrache2 (Nexus, 25/10/2026, v2.7.20, LM Studio with
// qwen3.5-9B): "The Ai starts to describe thing too much. Like leaning on a table" and "My wife is speaking to me
// and the AI is describing HIS actions." The dialogue style gave ONE gesture example, "*He leans back in his
// chair*", in every prompt including the compact one, while the narrative voice that carries the female examples
// is Full only: a 9B model copied the example, furniture and pronoun alike. The example now follows the
// character's own gender. The spouse bond was named only on an empty ledger or inside the intimacy rules (gone at
// Adult Off), so a wife with memories was never told who she is speaking to; identity now says it in every
// prompt. If this regresses, a small model narrates a woman as "he" again, or a wife speaks to her husband as to
// anyone else.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GestureExampleAndSpouseIdentityTests
   {
      private const string SpouseLine = "You are married to the player";

      private static NpcProfile Npc(bool female, bool withMemory)
      {
         var npc = new NpcProfile {
            Id = "npc_test", Name = "Test Hero", Faction = "Vlandia", Clan = "dey Meroc", Age = 30, IsFemale = female,
            Romantic = new RomanticProfile {IsFemale = female, Orientation = SexualOrientation.Heterosexual}
         };
         if (withMemory) npc.Events.Add(new NotableEvent(3, NotableEventType.Other, "The player brought her news of the war."));
         return npc;
      }

      private static string Prompt(NpcProfile npc, EncounterContext context, AdultContentLevel adult, bool playerIsFemale = false)
         => new PromptBuilder {AdultLevel = adult, PlayerIsFemale = playerIsFemale}
           .BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, context);

      // STAKE: the reported bug. A woman's prompt showed a man's gesture, and a small model narrated her as "he".
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_a_female_character_WHEN_building_the_prompt_THEN_no_gesture_example_is_written_for_a_man(LeanPromptLevel lean)
      {
         string prompt = Prompt(Npc(female: true, withMemory: false), new EncounterContext {LeanLevel = lean}, AdultContentLevel.Mature);

         prompt.Should().NotContain("*He ");
         prompt.Should().Contain("*She ");
      }

      // STAKE: the reported "leaning on a table". The one example a small model copies must not be a gesture
      // against furniture.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_building_it_THEN_the_gesture_example_leans_on_no_furniture()
      {
         string prompt = Prompt(Npc(female: false, withMemory: false), new EncounterContext {LeanLevel = LeanPromptLevel.Lean}, AdultContentLevel.Mature);

         prompt.Should().NotContain("leans back in his chair");
         prompt.Should().Contain("*He ");
      }

      // STAKE: Eisendrache2's log (25/10/2026): a 9B model on the compact prompt narrated its own gestures as "*I lean
      // against the post*", because the only person rule the compact prompt kept was the EVENT summary's "first person".
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_building_it_THEN_gestures_are_taught_in_the_third_person()
      {
         string prompt = Prompt(Npc(female: true, withMemory: false), new EncounterContext {LeanLevel = LeanPromptLevel.Lean}, AdultContentLevel.Mature);

         prompt.Should().Contain("Brief third-person stage directions (never \"I\") may add color:");
      }

      // STAKE: the Full prompt already teaches the third person in its narrative voice; saying it twice wastes tokens.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_building_it_THEN_the_compact_third_person_line_is_not_repeated()
      {
         string prompt = Prompt(Npc(female: true, withMemory: false), new EncounterContext {LeanLevel = LeanPromptLevel.Full}, AdultContentLevel.Mature);

         prompt.Should().NotContain("third-person stage directions");
         prompt.Should().Contain("Narrate in the third person");
      }

      // STAKE: a wife with a shared past, at Adult Off, was never told she is speaking to her own husband.
      [TestCase(LeanPromptLevel.Lean)]
      [TestCase(LeanPromptLevel.Full)]
      public void GIVEN_the_player_is_the_spouse_AND_adult_content_is_off_AND_there_are_memories_WHEN_building_the_prompt_THEN_the_marriage_is_named(LeanPromptLevel lean)
      {
         string prompt = Prompt(Npc(female: true, withMemory: true), new EncounterContext {LeanLevel = lean, NpcSpouseIsPlayer = true}, AdultContentLevel.Off);

         prompt.Should().Contain(SpouseLine + ": the one speaking to you is your own husband");
      }

      // STAKE: the word must match the player, or a husband is greeted as a wife.
      [Test]
      public void GIVEN_a_female_player_is_the_spouse_WHEN_building_the_prompt_THEN_she_is_named_the_wife()
      {
         string prompt = Prompt(Npc(female: false, withMemory: true), new EncounterContext {NpcSpouseIsPlayer = true}, AdultContentLevel.Off, playerIsFemale: true);

         prompt.Should().Contain(SpouseLine + ": the one speaking to you is your own wife");
      }

      // STAKE: anyone else must never be told the player is their spouse.
      [Test]
      public void GIVEN_the_player_is_not_the_spouse_WHEN_building_the_prompt_THEN_no_marriage_is_named()
      {
         string prompt = Prompt(Npc(female: true, withMemory: true), new EncounterContext {NpcSpouseIsPlayer = false}, AdultContentLevel.Off);

         prompt.Should().NotContain(SpouseLine);
      }
   }
}

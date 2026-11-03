// Code written by Gabriel Mailhot, 03/11/2026.
// Player report (EmhyrTyl, Nexus, 03/11/2026): on a local 26B model, a noble agreed on day one to marry the player,
// though already married, and never mentioned it; the game then refused "under the laws of Calradia". The spouse was
// in the prompt (YOUR FAMILY: Spouse), but nothing said it rules marriage out, and the consent block that names the
// vows is absent when adult content is off. These tests pin the line that tells a married character so: said in full
// and in Lean (the small model is the one that forgets), and never said to the player's own spouse or to the unwed.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class AlreadyMarriedPromptTests
   {
      private const string AlreadyMarried = "YOU ARE ALREADY MARRIED";

      private static NpcProfile Npc(string? spouse) => new() {
         Id = "npc_test",
         Name = "Test Lady",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         SpouseName = spouse
      };

      private static string Build(NpcProfile npc, EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(npc, new WorldState {CurrentDay = 1}, context);

      // STAKE: the reported case: a married noble agreed to wed the player, and the game refused the match the
      // character had promised. Told plainly, the character says so instead of promising what never happens.
      [Test]
      public void GIVEN_an_npc_married_to_someone_else_WHEN_built_THEN_they_are_told_they_cannot_wed_the_player()
      {
         string prompt = Build(Npc("Lord Aldric"), new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Free});

         prompt.Should().Contain(AlreadyMarried + ", to Lord Aldric");
         prompt.Should().Contain("You cannot wed the player");
      }

      // STAKE: EmhyrTyl played a small local model; if Lean dropped the line, the players most likely to meet the
      // bug would keep it.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_the_npc_is_married_THEN_the_line_is_still_said()
      {
         string prompt = Build(Npc("Lord Aldric"), new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            LeanLevel = LeanPromptLevel.Lean
         });

         prompt.Should().Contain(AlreadyMarried);
      }

      // STAKE: the player's own spouse told "you cannot wed the player" would deny the marriage the player is in.
      [Test]
      public void GIVEN_the_npc_is_the_players_spouse_WHEN_built_THEN_the_line_is_not_said()
      {
         string prompt = Build(Npc("The Player"), new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcSpouseIsPlayer = true
         });

         prompt.Should().NotContain(AlreadyMarried);
      }

      // STAKE: an unwed character told they are married would invent a spouse.
      [Test]
      public void GIVEN_an_unwed_npc_WHEN_built_THEN_the_line_is_not_said()
      {
         string prompt = Build(Npc(null), new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Free});

         prompt.Should().NotContain(AlreadyMarried);
      }
   }
}

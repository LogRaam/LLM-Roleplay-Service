// Code written by Gabriel Mailhot, 09/10/2026.
// HakiTakiUmba (Nexus, 09/10/2026): the player's own family travelling in the party (a wife, a brother) never asked
// for a private word, only the tavern companions did. They now may, on the same found-topics. The audience framing
// was written for hired companions ("in their service", "between comrades", "one who has served and hopes to rise"):
// a wife addressed that way reads as a retainer. If this breaks, the player's wife thanks him for the "shared
// service between you" or voices her hopes "as one who has served".

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class KinAudiencePromptTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_kin", Name = "Ira", Faction = "Vlandia", Clan = "the player's clan"};

      private static string Prompt(CompanionAudienceReason reason, bool kin)
         => new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, CompanionAudience = reason, AudienceFromKin = kin});

      // A wife or a brother who asked for a word speaks as family: told so, and never as one in the player's service.
      [TestCase(CompanionAudienceReason.Callback)]
      [TestCase(CompanionAudienceReason.Gratitude)]
      [TestCase(CompanionAudienceReason.SchemeWarning)]
      [TestCase(CompanionAudienceReason.Ambition)]
      [TestCase(CompanionAudienceReason.PracticalCounsel)]
      public void GIVEN_family_asking_for_a_word_WHEN_the_prompt_is_built_THEN_they_speak_as_kin_not_as_hired_help(CompanionAudienceReason reason)
      {
         string prompt = Prompt(reason, true);

         prompt.Should().Contain("of the player's own family");
         prompt.Should().NotContain("in their service");
         prompt.Should().NotContain("between comrades");
         prompt.Should().NotContain("shared service");
         prompt.Should().NotContain("one who has served");
      }

      // A hired companion keeps the framing written for them: the family line must not leak into their audience.
      [TestCase(CompanionAudienceReason.Gratitude)]
      [TestCase(CompanionAudienceReason.Ambition)]
      public void GIVEN_a_hired_companion_asking_for_a_word_WHEN_the_prompt_is_built_THEN_the_companions_framing_stands(CompanionAudienceReason reason)
      {
         string prompt = Prompt(reason, false);

         prompt.Should().NotContain("of the player's own family");
         prompt.Should().Contain("service");
      }
   }
}

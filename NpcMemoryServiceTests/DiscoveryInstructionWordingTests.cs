// Code written by Gabriel Mailhot, 21/07/2026.
// Player report (Nexus): PromptBuilder.AppendDiscoveryInstructions used to teach
// "description: what this player now perceives, in their voice (one sentence)" - a model easily read "in
// their voice" as license to write ABOUT the player rather than FROM the player's viewpoint about the NPC,
// and the Encyclopedia's discovery section (meant to hold only facts about the NPC) filled up with facts
// about the player instead. This pins the corrected wording: the taught description is explicitly
// third-person about the NPC ("YOU, the NPC"), the ambiguous "in their voice" phrase is gone, and the
// example given is an NPC-subject sentence, not a player-subject one.
// DiscoverySubjectGuard (Parsing) is the runtime safety net for whatever drift remains; this test only
// pins the TEACHING half of the fix.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class DiscoveryInstructionWordingTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lady",
         Faction = "Khuzait",
         Clan = "Kherit"
      };

      private static string BuildPrompt(AdultContentLevel level)
         => new PromptBuilder {AdultLevel = level}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = LeanPromptLevel.Full});

      // The reported ambiguity: "in their voice" invited the model to write in the PLAYER's own voice
      // instead of describing the NPC. Removed outright so no rewording of this fix can silently regress it.
      [Test]
      public void GIVEN_adult_content_is_on_WHEN_building_the_prompt_THEN_the_ambiguous_in_their_voice_phrasing_is_absent()
         => BuildPrompt(AdultContentLevel.Mature).Should().NotContain("in their voice");

      // The corrected teaching must say the subject is the NPC, unambiguously, since that is the one
      // sentence a model actually reads before emitting the field.
      [Test]
      public void GIVEN_adult_content_is_on_WHEN_building_the_prompt_THEN_the_description_is_taught_as_being_about_the_npc()
         => BuildPrompt(AdultContentLevel.Mature).Should().Contain("about YOU, the NPC, not the player");

      // Discovery teaching (and the whole channel) is gated off at Off, same as before this fix; pins that
      // the rewrite did not accidentally remove or invert that gate.
      [Test]
      public void GIVEN_adult_content_is_off_WHEN_building_the_prompt_THEN_no_discovery_block_is_taught()
         => BuildPrompt(AdultContentLevel.Off).Should().NotContain("[DISCOVERY]");
   }
}

// Code written by Gabriel Mailhot, 10/11/2026.
// HOW READILY YOU BELIEVE THE PLAYER renders the host's credulity note (shrewd, trusting, or wary after a broken faith)
// in the stable prefix, and ties it back to the claims rule. Gabriel (10/11/2026), after AI Influence 7.0.0's naivety.
// If this is wrong, a trusting character's nature is drowned by the guidelines' general caution, a trusting lord pays a
// reward on an unproven deed because nothing said belief is not payment, or every prompt carries an empty heading.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CredulityNoteTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_test", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};

      // STAKE: the nature is the same all conversation; above the marker it is cached, and it says it goes first.
      [Test]
      public void GIVEN_a_credulity_note_WHEN_the_prompt_is_built_THEN_it_renders_above_the_marker_and_points_at_the_claims_rule()
      {
         var context = new EncounterContext {CredulityNote = "You are trusting, perhaps too trusting."};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState(), context);

         prompt.Should().Contain("HOW READILY YOU BELIEVE THE PLAYER (your own nature; it goes before the general caution of the guidelines):");
         prompt.IndexOf("You are trusting, perhaps too trusting.").Should().BeLessThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading));
         prompt.Should().Contain("still follows WHAT THEY SAY THEY HAVE DONE IS A CLAIM");
      }

      // STAKE: the ordinary character carries no heading at all; most prompts stay as they were.
      [Test]
      public void GIVEN_no_credulity_note_WHEN_the_prompt_is_built_THEN_no_heading_is_written()
      {
         new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState(), new EncounterContext()).Should().NotContain("HOW READILY YOU BELIEVE");
      }
   }
}

// Code written by Gabriel Mailhot, 29/10/2026.
// How long replies run, as the player chooses in the MCM. RaveN017Mod (Nexus PM, 29/10/2026) found the replies too long,
// "like a novel", and asked for a word limit; the setting counts sentences instead, since a word cap cuts a reply off
// mid-sentence. If the lines go wrong, a player who chose short replies still gets speeches, a narrative style quietly
// lengthens them again, or a player who never touched the setting finds his characters suddenly terse.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ReplyLengthPromptTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

      private static string Build(ReplyLength length, string? style = null)
         => new PromptBuilder {ReplyLength = length}.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, NarrativeStyle = style});

      // The report: a player who asks for short replies is taught short replies, even for a story.
      [Test]
      public void GIVEN_short_replies_chosen_WHEN_the_prompt_is_built_THEN_every_reply_stays_within_three_sentences()
      {
         string prompt = Build(ReplyLength.Short);

         prompt.Should().Contain("- 1 to 3 sentences, always");
         prompt.Should().Contain("Even a story, a memory or a strong feeling stays within three sentences.");
         prompt.Should().NotContain("- 3-7 sentences for normal exchanges.");
      }

      // The middle setting: shorter than the shipped length, room for a story.
      [Test]
      public void GIVEN_balanced_replies_chosen_WHEN_the_prompt_is_built_THEN_two_to_four_sentences_are_taught()
      {
         Build(ReplyLength.Balanced).Should().Contain("- 2 to 4 sentences for normal exchanges");
      }

      // A player who never opens the setting keeps exactly the length the mod has always taught.
      [Test]
      public void GIVEN_the_default_WHEN_the_prompt_is_built_THEN_the_shipped_length_is_taught_unchanged()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext());

         prompt.Should().Contain("- 3-7 sentences for normal exchanges.");
         prompt.Should().NotContain("the player chose");
      }

      // A narrative style may run speeches long at the shipped length, but never past a length the player chose.
      [Test]
      public void GIVEN_a_style_and_short_replies_WHEN_built_THEN_the_style_does_not_lengthen_them()
      {
         string shortWithStyle = Build(ReplyLength.Short, "Write like an old saga.");
         string fullWithStyle = Build(ReplyLength.Full, "Write like an old saga.");

         shortWithStyle.Should().Contain("except the reply length the player chose, which holds");
         shortWithStyle.Should().NotContain("norm yields");
         fullWithStyle.Should().Contain("\"3-7 sentences\" norm yields");
      }
   }
}

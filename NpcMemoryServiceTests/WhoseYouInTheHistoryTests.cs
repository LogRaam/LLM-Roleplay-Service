// Code written by Gabriel Mailhot, 20/09/2026.
// What these pin: that the word "you" never quietly changes who it means.
//
// reminensce (Nexus, 19/09/2026): Chief Rolan, a Sturgian noble, had just taunted the player over the player's own
// defeat at Vladiv (a line another mod injects into the vanilla dialogue). A few turns into the CR chat, Rolan said
// the player was boasting of HIS defeat at Vladiv and claiming HE would never match the player's prowess. He had
// taken both the defeat and the taunt for his own.
//
// Nothing in the prompt carried that taunt, so the character was not copying it. What it did carry was the memory
// line, written the way the memory templates wrote them: "Faced you in battle and held the field against you at
// Vladiv." In a prompt that says "You are Chief Rolan" and means it everywhere else, that line asks the model to
// read "you" as the player for one block and then change back. The mod now names the player in new memories, but
// every save already written holds the old wording, so the block states the convention outright.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class WhoseYouInTheHistoryTests
   {
      private const string OldStyleMemory = "Faced you in battle and held the field against you near Vladiv.";

      // THE SAVES THAT ALREADY EXIST: the memory cannot be rewritten, so the reader is told how to read it.
      [Test]
      public void GIVEN_memories_written_in_the_old_wording_WHEN_the_prompt_is_built_THEN_the_block_says_whose_you_is_whose()
      {
         string prompt = Build("Adam of Vlandia", OldStyleMemory);

         prompt.Should().Contain("In the lines below, \"you\" and \"your\" mean the player, Adam of Vlandia");
         prompt.Should().Contain("\"I\", \"me\" and \"my\" mean you");
         prompt.Should().Contain(OldStyleMemory, "the memory itself is left exactly as it was written");
      }

      // The convention must still be stated when the host has no name to give, or the line would name nobody.
      [Test]
      public void GIVEN_no_player_name_WHEN_the_prompt_is_built_THEN_the_convention_is_still_stated()
      {
         Build("", OldStyleMemory).Should().Contain("mean the PLAYER");
      }

      // A first meeting has no memories, so there is nothing to explain and the line would be noise.
      [Test]
      public void GIVEN_a_character_with_no_memories_WHEN_the_prompt_is_built_THEN_no_convention_line_is_spent()
      {
         Build("Adam of Vlandia").Should().NotContain("In the lines below");
      }

      #region what the model writes back

      // A summary the model writes is read back every turn for the rest of the campaign, so "the player" in one is
      // a puzzle it solves again and again. Named once, at the moment it is stored.
      [Test]
      public void GIVEN_a_summary_that_says_the_player_WHEN_it_is_stored_THEN_it_names_them_instead()
      {
         var profile = new NpcProfile {Id = "n", Name = "Chief Rolan", Faction = "Sturgia", Clan = "Rolan"};

         ProfileMutator.ApplyNotableEvent(profile, NotableEventType.Other,
            "I told the player I would not forget Vladiv.", 40, "Adam of Vlandia");

         profile.Events[0].summary.Should().Be("I told Adam of Vlandia I would not forget Vladiv.");
      }

      // Everything else in the sentence is left alone: a memory is never worth mangling to enforce a convention.
      [Test]
      public void GIVEN_a_summary_with_no_such_phrase_WHEN_it_is_stored_THEN_it_is_kept_word_for_word()
      {
         const string said = "I refused you the grain, and I meant it.";
         var profile = new NpcProfile {Id = "n", Name = "Chief Rolan", Faction = "Sturgia", Clan = "Rolan"};

         ProfileMutator.ApplyNotableEvent(profile, NotableEventType.Other, said, 40, "Adam of Vlandia");

         profile.Events[0].summary.Should().Be(said);
      }

      // No name to substitute, no substitution, and never an exception: the host may not know the name yet.
      [Test]
      public void GIVEN_no_player_name_WHEN_a_summary_is_normalized_THEN_it_comes_back_untouched()
      {
         const string said = "I told the player I would not forget Vladiv.";

         EventSubjectNormalizer.Normalize(said, null).Should().Be(said);
         EventSubjectNormalizer.Normalize(said, "   ").Should().Be(said);
         EventSubjectNormalizer.Normalize(null, "Adam").Should().BeNull();
      }

      // "Player" as the actual player name would make the substitution a no-op with extra steps, and could mangle
      // a sentence rather than clarify it.
      [Test]
      public void GIVEN_a_player_literally_named_player_WHEN_a_summary_is_normalized_THEN_nothing_is_swapped()
      {
         const string said = "I told the player I would not forget Vladiv.";

         EventSubjectNormalizer.Normalize(said, "the player").Should().Be(said);
      }

      #endregion

      #region private

      private static string Build(string playerName, params string[] memories)
      {
         var npc = new NpcProfile {Id = "n", Name = "Chief Rolan", Faction = "Sturgia", Clan = "Rolan"};

         foreach (string memory in memories)
            npc.Events.Add(new NotableEvent(10, NotableEventType.Other, memory));

         return new PromptBuilder {PlayerName = playerName}.BuildSystemPrompt(
            npc, new WorldState {CurrentDay = 40},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, Scene = SceneType.Keep});
      }

      #endregion
   }
}

// Code written by Gabriel Mailhot, 04/10/2026.
// Branching tales (Calradia Remembers, docs/design/quest-editor/goal.md): a written tale may let the player take a
// choice BY WORDS ("the player promises to keep silent for a price"), taken when the action interpreter reports it in
// a conversation with that character. The mod writes the choices open in that conversation into
// EncounterContext.StoryChoicesForInterpreter; this pins where they go. If it is wrong, the interpreter is never told
// (the choice can never be taken), every conversation pays for choices it does not have (the cached head changes per
// conversation and stops being cached), or the CHARACTER is told the ways ahead, which Gabriel's rule forbids: a
// character is told only what they know at this stage, never what comes next.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class StoryChoicesForInterpreterTests
   {
      private const string Choices = "STORY CHOICES THE PLAYER MAY TAKE BY WORDS IN THIS CONVERSATION:\n- choice his_price: the player agrees to keep silent for a price";

      // The interpreter reads the open choices of this conversation, after its cached head, with the reply to analyze.
      [Test]
      public void GIVEN_choices_by_words_open_in_this_conversation_WHEN_the_interpreter_prompt_is_built_THEN_it_reads_them_after_its_cached_head()
      {
         string facts = ActionInterpreterContextBuilder.Project(new EncounterContext {StoryChoicesForInterpreter = Choices}, new NpcProfile {Id = "lord_varn", Name = "Varn", Clan = "", Faction = ""});
         string prompt = ActionInterpreterPromptBuilder.Build("Very well. Take the silver.", facts);

         prompt.Should().StartWith(ActionInterpreterPromptBuilder.StablePrefix);
         prompt.IndexOf("choice his_price", System.StringComparison.Ordinal).Should().BeGreaterThan(ActionInterpreterPromptBuilder.StablePrefix.Length);
      }

      // A conversation with no choice by words is told nothing about tales: the interpreter gets nothing to emit.
      [Test]
      public void GIVEN_no_choice_by_words_open_WHEN_the_interpreter_facts_are_projected_THEN_they_say_nothing_of_story_choices()
      {
         ActionInterpreterContextBuilder.Project(new EncounterContext(), new NpcProfile {Id = "lord_varn", Name = "Varn", Clan = "", Faction = ""})
                                        .Should().NotContain("STORY CHOICE");
      }

      // Gabriel: a character is never told what comes next. The choices go to the interpreter only, never into the
      // prompt the character speaks from.
      [Test]
      public void GIVEN_choices_for_the_interpreter_WHEN_the_characters_own_prompt_is_built_THEN_the_character_is_not_told_them()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "lord_varn", Name = "Varn", Clan = "", Faction = ""},
            new WorldState {CurrentDay = 10}, new EncounterContext {StoryChoicesForInterpreter = Choices});

         prompt.Should().NotContain("his_price").And.NotContain("STORY CHOICES");
      }
   }
}

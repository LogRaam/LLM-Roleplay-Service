// Code written by Gabriel Mailhot, 04/10/2026.
// Branching tales (Calradia Remembers, docs/design/quest-editor/goal.md): a written tale may let the player take a
// choice BY WORDS ("the player promises to keep silent for a price"), taken when the action interpreter reports it in
// a conversation with that character. The mod writes the choices open in that conversation into
// EncounterContext.StoryChoicesForInterpreter; this pins where they go. If it is wrong, whoever reads the turn's actions
// is never told (the choice can never be taken), every conversation pays for choices it does not have (the cached head
// changes per conversation and stops being cached), or in Enhanced the CHARACTER is told the ways ahead, which
// Gabriel's rule forbids: a character is told only what they know at this stage, never what comes next.
// Gabriel (25/10/2026), on Standard composition, where the one call writes its own tags: "le mode Enhanced a ete concu
// apres pour ameliorer ce que fait le mode regulier [...] pour reproduire le comportement de Regulier", then "je
// confirme" to teaching the open choices there like any other open deed, for the [ACTION] block only.

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

      // Gabriel: a character is never told what comes next. In Enhanced the interpreter reads the actions, so the
      // choices go to it only, never into the prompt the character speaks from.
      [Test]
      public void GIVEN_an_interpreter_turn_WHEN_the_characters_own_prompt_is_built_THEN_the_character_is_not_told_the_choices()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "lord_varn", Name = "Varn", Clan = "", Faction = ""},
            new WorldState {CurrentDay = 10}, new EncounterContext {StoryChoicesForInterpreter = Choices, ProseReadsItsOwnActions = false});

         prompt.Should().NotContain("his_price").And.NotContain("STORY CHOICES");
      }

      // STAKE: in Standard the one call writes its own tags; without the choices a tale's choice by words could never
      // be taken there (the parity map's story_choice debt, 25/10/2026).
      [Test]
      public void GIVEN_a_turn_whose_own_reply_is_read_for_actions_WHEN_the_prompt_is_built_THEN_the_open_choices_are_taught_below_the_marker()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "lord_varn", Name = "Varn", Clan = "", Faction = ""},
            new WorldState {CurrentDay = 10}, new EncounterContext {StoryChoicesForInterpreter = Choices, ProseReadsItsOwnActions = true});

         prompt.IndexOf("choice his_price", System.StringComparison.Ordinal)
               .Should().BeGreaterThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal));
      }

      // STAKE: the character who knows the ways out must not hand them to the player; the choice is the player's to
      // make in their own words ("je confirme", with the rule not to announce or steer).
      [Test]
      public void GIVEN_the_choices_taught_to_the_character_WHEN_the_prompt_is_built_THEN_it_is_told_they_are_not_its_to_announce_or_steer_toward()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "lord_varn", Name = "Varn", Clan = "", Faction = ""},
            new WorldState {CurrentDay = 10}, new EncounterContext {StoryChoicesForInterpreter = Choices, ProseReadsItsOwnActions = true});

         prompt.Should().Contain("not yours to announce, offer, hint at or steer toward");
      }

      // STAKE: Compact is for small local models, which run Standard most; dropping the choices there would leave a
      // tale's choice by words impossible for exactly those players.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_the_reply_is_read_for_actions_THEN_the_open_choices_are_still_taught()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "lord_varn", Name = "Varn", Clan = "", Faction = ""},
            new WorldState {CurrentDay = 10},
            new EncounterContext {StoryChoicesForInterpreter = Choices, ProseReadsItsOwnActions = true, LeanLevel = LeanPromptLevel.Lean});

         prompt.Should().Contain("choice his_price");
      }
   }
}

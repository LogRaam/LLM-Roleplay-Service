// Code written by Gabriel Mailhot, 21/07/2026.
// Player report (2026-07-21, Nexus): a Spanish-speaking player writing to DeepSeek V3 always got English back,
// while Gabriel's own French always mirrors correctly. The auto-detect rule (PromptBuilder.AppendLanguageMirror)
// only names English/French/German/Ukrainian as worked examples; a language outside that list, on a weaker
// model that follows an abstract instruction loosely against an otherwise-English prompt, gets ignored. The
// fix is PromptBuilder.ReplyLanguage: an explicit, named language from the host's own setting that removes the
// guesswork entirely, for conversations, letters, and generated memories alike (all three share this builder).

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ReplyLanguagePromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // Regression guard: a player who never touches the setting must keep today's behaviour exactly, worked
      // examples and all. This is what already serves Gabriel's French correctly.
      [Test]
      public void GIVEN_no_reply_language_set_WHEN_built_THEN_the_auto_detect_mirror_with_examples_is_used()
      {
         var builder = new PromptBuilder();
         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext());

         prompt.Should().Contain("Detect the language of the player's last message and reply in that SAME language");
         prompt.Should().Contain("Player writes in French     → you reply in French.");
      }

      // The actual fix: a named language is stated as a fixed fact, not inferred, so a model that ignores the
      // abstract rule (or lacks this language among its worked examples) has nothing left to infer.
      [Test]
      public void GIVEN_a_reply_language_set_WHEN_built_THEN_the_reply_is_pinned_to_it_instead_of_detected()
      {
         var builder = new PromptBuilder {ReplyLanguage = "Spanish"};
         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext());

         prompt.Should().Contain("Always write your reply in Spanish");
         prompt.Should().NotContain("Detect the language of the player's last message");
      }

      // A pinned language must not loosen the machine-read contract: section labels and action keywords stay
      // parseable English regardless, or the mod's own [ACTION]/[EVENT] parser breaks the moment this is used.
      [Test]
      public void GIVEN_a_reply_language_set_WHEN_built_THEN_section_labels_and_action_keywords_stay_english()
      {
         var builder = new PromptBuilder {ReplyLanguage = "Spanish"};
         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext());

         prompt.Should().Contain("Keep ALL section labels ([DIALOGUE], [NARRATION], [ACTION], [EVENT]");
         prompt.Should().Contain("action keyword (change_relation, give_gold, give_item …) in English.");
      }

      // The commoner prompt is a second, independent call site (BuildCommonerSystemPrompt does not reuse
      // BuildSystemPrompt), so without its own coverage a fix here could silently miss transient NPCs.
      [Test]
      public void GIVEN_a_reply_language_set_WHEN_a_commoner_prompt_is_built_THEN_it_is_pinned_too()
      {
         var builder = new PromptBuilder {ReplyLanguage = "Spanish"};
         string prompt = builder.BuildCommonerSystemPrompt(Npc(), new CommonsKnowledge());

         prompt.Should().Contain("Always write your reply in Spanish");
      }
   }
}

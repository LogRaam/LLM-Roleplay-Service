// Code written by Gabriel Mailhot, 17/11/2026.
// Goeth (Discord, 17/11/2026): a 150-line night in a tavern came back as two summary lines; with the real words of earlier
// talks in the prompt, characters "pick up threads naturally", keep their tone, "stop contradicting what was actually said".
// The host (Calradia Remembers) picks the passages; PromptBuilder writes them. These pin where and how: below the cache
// marker (they change every turn; above it they would re-bill the whole prefix), in the Full prompt only (Gabriel: a switch
// that spares tokens), labelled so the character knows who said what, and framed so old words neither get recited nor
// outrank what changed since.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class RecalledExchangesPromptTests
   {
      private static readonly NpcProfile Npc = new() {Id = "lord_1_1", Name = "Lucon", Faction = "Empire", Clan = "Lucon"};

      private static EncounterContext WithRecall(LeanPromptLevel lean = LeanPromptLevel.Full) => new() {
         LeanLevel = lean,
         RecalledExchanges = new List<RecalledExchange> {
            new() {
               When = "12 days ago", Place = "Poros", AlsoPresent = new List<string> {"Martira"},
               Lines = new List<RecalledLine> {
                  new() {Speaker = "PLAYER", Text = "Teach me the sword, and I will teach you to laugh."},
                  new() {Speaker = "YOU", Text = "A bargain, then. Mind your feet."}
               }
            }
         }
      };

      private static string Prompt(EncounterContext context) => new PromptBuilder().BuildSystemPrompt(Npc, new WorldState {CurrentDay = 900}, context);

      // STAKE: the character gets the exact words of the earlier night, labelled, with when, where and who else heard.
      [Test]
      public void GIVEN_recalled_words_WHEN_the_prompt_is_built_THEN_they_appear_word_for_word_and_labelled()
      {
         string prompt = Prompt(WithRecall());

         prompt.Should().Contain("WORDS FROM EARLIER TALKS");
         prompt.Should().Contain("-- 12 days ago, in Poros; also there: Martira --");
         prompt.Should().Contain("[PLAYER] Teach me the sword, and I will teach you to laugh.");
         prompt.Should().Contain("[YOU] A bargain, then. Mind your feet.");
         prompt.Should().Contain("Never recite them back");
      }

      // STAKE (Gabriel, 17/11/2026): a mission told over several talks is one story, and its latest terms are the ones that hold.
      [Test]
      public void GIVEN_passages_on_one_matter_WHEN_built_THEN_the_matter_and_the_latest_are_named()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            RecalledExchanges = new List<RecalledExchange> {
               new() {When = "20 days ago", About = "Varcheg", Lines = new List<RecalledLine> {new() {Speaker = "YOU", Text = "I will pay one thousand."}}},
               new() {When = "2 days ago", About = "Varcheg", Latest = true, Lines = new List<RecalledLine> {new() {Speaker = "YOU", Text = "Two thousand, not one."}}}
            }
         };

         string prompt = Prompt(context);

         prompt.Should().Contain("-- 20 days ago; about Varcheg --");
         prompt.Should().Contain("-- 2 days ago; about Varcheg, the latest said on it --");
         prompt.Should().Contain("the one marked the latest says how it stands now");
      }

      // STAKE: words that change every turn above the marker would re-bill the whole cached prefix each reply.
      [Test]
      public void GIVEN_recalled_words_WHEN_the_prompt_is_built_THEN_they_sit_below_the_cache_marker()
      {
         string prompt = Prompt(WithRecall());

         prompt.IndexOf("WORDS FROM EARLIER TALKS", System.StringComparison.Ordinal)
               .Should().BeGreaterThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal));
      }

      // STAKE: the compact prompt is for short contexts and small budgets; the switch must not swell it.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_built_THEN_no_recalled_words_are_written()
      {
         Prompt(WithRecall(LeanPromptLevel.Lean)).Should().NotContain("WORDS FROM EARLIER TALKS");
      }

      // STAKE: a player with the switch off (the default) gets exactly the prompt they had.
      [Test]
      public void GIVEN_nothing_recalled_WHEN_built_THEN_the_prompt_is_unchanged()
      {
         Prompt(new EncounterContext()).Should().Be(Prompt(new EncounterContext {RecalledExchanges = new List<RecalledExchange>()}));
         Prompt(new EncounterContext()).Should().NotContain("WORDS FROM EARLIER TALKS");
      }
   }
}

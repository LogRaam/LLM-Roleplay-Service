// Code written by Gabriel Mailhot, 29/08/2026.
// Modder report, two related fixes to the world-fact prompt blocks:
//  (A) a weak model lifted a fact's Summary word for word instead of voicing it as hearsay in its own
//      words, so the always-on world-fact blocks now carry an explicit ANTI-VERBATIM rule.
//  (B) awareness alone (WorldRumorsBlock's small top-N) never consults the player's CURRENT message, so a
//      fact the NPC DOES know but that ranked low got a hallucinated answer instead. QueryRelevantFactsBlock
//      is a SECOND, deterministic (no extra LLM call), message-relevant block, resolved game-side and
//      rendered here under its own header, distinct from the ambient WorldRumorsBlock.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class QueryRelevantFactsPromptTests
   {
      private const string QueryRelevantHeader = "WHAT YOU KNOW THAT TOUCHES ON THIS";
      private const string QueryRelevantLine = "- Lord Derthert broke Lord Caladog's host near Pravend. QUERY_FACT_MARKER.";
      private const string WorldRumorsLine = "- A tournament was held at Sargot. RUMOR_MARKER.";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      // STAKE: this is the whole point of Part B, a fact the ambient top-N never surfaced must still reach
      // the model when it bears on what the player just asked, under its own distinct header so it never
      // gets confused with, or silently folds into, the unrelated ambient rumour list.
      [Test]
      public void GIVEN_a_query_relevant_facts_block_WHEN_building_the_prompt_THEN_it_renders_under_its_own_header()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, QueryRelevantFactsBlock = QueryRelevantLine};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(QueryRelevantHeader);
         prompt.Should().Contain(QueryRelevantLine);
      }

      // Blank guard: in normal play, no relevant fact means nothing at all should render, keeping the
      // prompt byte-identical to before this field existed (the design's explicit requirement).
      [Test]
      public void GIVEN_no_query_relevant_facts_WHEN_building_the_prompt_THEN_the_header_is_absent()
      {
         var builder = new PromptBuilder();
         var withNone = new EncounterContext {LeanLevel = LeanPromptLevel.Full};
         var withBlank = new EncounterContext {LeanLevel = LeanPromptLevel.Full, QueryRelevantFactsBlock = "   "};

         string promptNone = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withNone);
         string promptBlank = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, withBlank);

         promptNone.Should().NotContain(QueryRelevantHeader);
         promptBlank.Should().NotContain(QueryRelevantHeader);
      }

      // The two world-fact blocks are additive, not a replacement: the ambient rumour feed must keep
      // rendering exactly as before even when the new query-relevant block is also present.
      [Test]
      public void GIVEN_both_ambient_rumours_and_a_query_relevant_fact_WHEN_building_the_prompt_THEN_both_blocks_render()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            WorldRumorsBlock = WorldRumorsLine,
            QueryRelevantFactsBlock = QueryRelevantLine
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("WHAT YOU'VE HEARD");
         prompt.Should().Contain(WorldRumorsLine);
         prompt.Should().Contain(QueryRelevantHeader);
         prompt.Should().Contain(QueryRelevantLine);
      }

      // STAKE (Part A): a modder reported weaker models lifting a fact's Summary word for word instead of
      // voicing it as hearsay. The block must explicitly tell the model the fact is knowledge, not a line
      // to quote, and forbid repeating it verbatim, in every world-fact block that injects raw fact text.
      [Test]
      public void GIVEN_the_ambient_world_rumours_block_WHEN_building_the_prompt_THEN_it_forbids_verbatim_repetition()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, WorldRumorsBlock = WorldRumorsLine};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("word for word");
         prompt.Should().Contain("not a line to");
      }

      // The new query-relevant block carries the SAME hearsay/own-words/anti-verbatim framing as the
      // ambient block (design requirement: "the Part A rule applies here too").
      [Test]
      public void GIVEN_the_query_relevant_facts_block_WHEN_building_the_prompt_THEN_it_also_forbids_verbatim_repetition()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, QueryRelevantFactsBlock = QueryRelevantLine};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("word for word");
      }

      // STAKE (Part A, commoner path): the same weak-model regurgitation risk exists on the slim commoner
      // prompt (BuildCommonerSystemPrompt), which injects CommonsKnowledge.RumorsBlock raw the same way.
      [Test]
      public void GIVEN_commoner_rumours_WHEN_building_the_commoner_prompt_THEN_it_forbids_verbatim_repetition()
      {
         var builder = new PromptBuilder();
         var knowledge = new CommonsKnowledge {RumorsBlock = "- The well ran dry last week. COMMONER_RUMOR_MARKER."};

         string prompt = builder.BuildCommonerSystemPrompt(Npc(), knowledge);

         prompt.Should().Contain("word for word");
         prompt.Should().Contain(knowledge.RumorsBlock);
      }
   }
}

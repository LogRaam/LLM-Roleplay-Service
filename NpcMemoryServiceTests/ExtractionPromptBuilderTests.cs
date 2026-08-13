// Code written by Gabriel Mailhot, 13/08/2026.
// PROSE+EXTRACT spike: the whole idea rests on one risky claim, that a fast second model can read prose a first
// model wrote and emit the SAME [ACTION]/[EVENT] tags the live pipeline already parses. ExtractionPromptBuilder
// is the contract that makes that possible. These tests pin the two properties the spike depends on and cannot
// verify in-game cheaply: (1) the prompt is assembled STABLE-PREFIX FIRST, so a provider can prompt-cache the
// invariant head across every extraction call (the whole cost argument for a two-call chain), and (2) an
// extractor OUTPUT written in the taught format round-trips through the REAL SectionResponseParser into the
// expected actions and event, proving the builder teaches the parser's own format and not a new one. If either
// breaks, the spike is invalid: caching savings evaporate, or the extractor's tags never reach the game.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ExtractionPromptBuilderTests
   {
      private const string SampleContextFacts = "NPC: Caladog of Fen Company (Battanian). Current regard toward player: +12.";
      private const string SampleProse = "*The warmth drains from his face.* Cowards. Get off my land. *He turns his shoulder to you, done.*";

      // The cost case for the whole two-call chain is prompt caching: the extractor's role and format never change,
      // so a provider must be able to cache them. That only works if the built prompt LITERALLY begins with the
      // invariant head. If per-turn data ever crept in ahead of it, the cache breakpoint would shift every call
      // and the caching savings that justify a second model would vanish.
      [Test]
      public void GIVEN_any_extraction_prompt_WHEN_built_THEN_it_starts_with_the_stable_prefix()
      {
         string prompt = ExtractionPromptBuilder.Build(SampleProse, SampleContextFacts);

         prompt.Should().StartWith(ExtractionPromptBuilder.StablePrefix);
      }

      // The extractor is worthless if the stable head does not actually teach the three reactive signals the spike
      // scopes: the regard shift (change_relation), the memory line ([EVENT]), and the break-off (end_conversation).
      // These are the tokens the parser keys on, so their literal presence in the cacheable prefix is the vocabulary
      // the extractor is being taught.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_teaches_the_three_core_reactive_tags()
      {
         string prefix = ExtractionPromptBuilder.StablePrefix;

         prefix.Should().Contain("change_relation");
         prefix.Should().Contain("[EVENT]");
         prefix.Should().Contain("end_conversation");
      }

      // The single sentence that makes an EXTRACTOR different from a roleplay model: it must NOT continue or rewrite
      // the prose it is handed, only tag it. Without this rule a chat-tuned model reads the prose as a turn to
      // answer and writes fresh dialogue, which the parser would then mine for tags from text the NPC never said.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_carries_the_do_not_rewrite_rule()
      {
         string prefix = ExtractionPromptBuilder.StablePrefix;

         prefix.Should().Contain("ALREADY WRITTEN");
         prefix.Should().Contain("Do NOT rewrite");
      }

      // A real spike run had the extractor label the player "the coward" in the memory summary, because nothing
      // told it how to refer to the player and the old wording ("never a name") pushed it to an epithet. The
      // memory record must name the player consistently, or the NPC's remembered history rots into a mix of
      // epithets that also bakes a one-time insult into a permanent record.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_names_the_player_and_forbids_epithets()
      {
         string prefix = ExtractionPromptBuilder.StablePrefix;

         prefix.Should().Contain("Refer to the PLAYER by the name given in the facts");
         prefix.Should().Contain("the coward"); // named as a forbidden example, not a licence
      }

      // The extractor gets a situational digest (place, who is present, the player's standing) so memories are
      // grounded ("near Veron Castle"), but that context must ANCHOR only, never license invention: without this
      // guard the extractor could write "he threatened me with his army" from a mere "player has an army" fact.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_grounds_in_facts_without_inventing()
      {
         string prefix = ExtractionPromptBuilder.StablePrefix;

         prefix.Should().Contain("GROUND IN THE FACTS, DO NOT INVENT");
         prefix.Should().Contain("record ONLY what actually happened");
      }

      // Caching aside, correctness requires the per-turn data to sit AFTER the invariant head: the extractor must
      // see the context facts and the prose it is meant to analyze, and they must be the variable tail, not woven
      // into the cached prefix (which would defeat caching and change the "stable" head every call).
      [Test]
      public void GIVEN_context_and_prose_WHEN_built_THEN_both_appear_after_the_prefix()
      {
         string prompt = ExtractionPromptBuilder.Build(SampleProse, SampleContextFacts);

         int prefixEnd = ExtractionPromptBuilder.StablePrefix.Length;
         prompt.IndexOf(SampleContextFacts, System.StringComparison.Ordinal).Should().BeGreaterThanOrEqualTo(prefixEnd);
         prompt.IndexOf(SampleProse, System.StringComparison.Ordinal).Should().BeGreaterThan(prompt.IndexOf(SampleContextFacts, System.StringComparison.Ordinal));
      }

      // The prose is the payload, and it must be clearly fenced off from the instructions so the model knows where
      // the reply-to-analyze begins. A missing delimiter is exactly what makes a model treat the whole thing as one
      // instruction and start answering.
      [Test]
      public void GIVEN_a_built_prompt_WHEN_inspected_THEN_the_prose_is_introduced_by_a_clear_delimiter()
      {
         string prompt = ExtractionPromptBuilder.Build(SampleProse, SampleContextFacts);

         prompt.Should().Contain("REPLY TO ANALYZE:");
         prompt.IndexOf("REPLY TO ANALYZE:", System.StringComparison.Ordinal)
               .Should().BeLessThan(prompt.IndexOf(SampleProse, System.StringComparison.Ordinal));
      }

      // The load-bearing claim of the whole spike: an extractor OUTPUT written in the format this builder teaches
      // must parse, through the SAME SectionResponseParser the live chat uses, into a real change_relation with its
      // delta, an end_conversation, and an [EVENT] with type and summary. If this round-trip fails, the builder is
      // teaching a format the game cannot consume and the two-call chain cannot work.
      [Test]
      public void GIVEN_an_extractor_output_in_the_taught_format_WHEN_parsed_THEN_it_yields_the_expected_actions_and_event()
      {
         // A faithful example of what the extractor is asked to emit for the "grave insult, NPC ends it" case.
         const string extractorOutput =
            "[ACTION]\n" +
            "type: change_relation\n" +
            "delta: -15\n" +
            "[/ACTION]\n" +
            "[ACTION]\n" +
            "type: end_conversation\n" +
            "[/ACTION]\n" +
            "[EVENT]\n" +
            "type: conflict\n" +
            "summary: I threw the player off my land after they insulted my kin.\n" +
            "[/EVENT]";

         var parser = new SectionResponseParser();
         ParsedResponse parsed = parser.Parse(extractorOutput);

         parsed.Actions.Should().ContainSingle(a => a.Type == "change_relation")
               .Which.Parameters["delta"].Should().Be("-15");
         parsed.Actions.Should().Contain(a => a.Type == "end_conversation");

         parsed.NewEventData.Should().NotBeNull();
         parsed.NewEventData!.Type.Should().Be(NotableEventType.Conflict);
         parsed.NewEventData.Summary.Should().Contain("threw the player off my land");
      }

      // The intimacy sample the spike ships must also round-trip: a flirt/intimacy [EVENT] plus a small positive
      // regard, no end_conversation. This guards the OTHER end of the tag vocabulary (a warm beat, not a break-off)
      // so the format is proven across the range of prose the extractor will see, not just the angry case.
      [Test]
      public void GIVEN_an_intimacy_extractor_output_WHEN_parsed_THEN_it_yields_an_intimacy_event_and_positive_regard_with_no_end()
      {
         const string extractorOutput =
            "[ACTION]\n" +
            "type: change_relation\n" +
            "delta: 3\n" +
            "[/ACTION]\n" +
            "[EVENT]\n" +
            "type: intimacy\n" +
            "summary: I gave myself to the player after twenty days apart.\n" +
            "[/EVENT]";

         var parser = new SectionResponseParser();
         ParsedResponse parsed = parser.Parse(extractorOutput);

         parsed.Actions.Should().ContainSingle(a => a.Type == "change_relation")
               .Which.Parameters["delta"].Should().Be("3");
         parsed.Actions.Should().NotContain(a => a.Type == "end_conversation");

         parsed.NewEventData.Should().NotBeNull();
         parsed.NewEventData!.Type.Should().Be(NotableEventType.Intimacy);
      }
   }
}

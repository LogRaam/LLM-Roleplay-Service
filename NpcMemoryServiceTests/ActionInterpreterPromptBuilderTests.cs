// Code written by Gabriel Mailhot, 13/08/2026.
// PROSE + INTERPRETER spike: the whole idea rests on one risky claim, that a fast second model can read prose a
// first model wrote and emit the SAME [ACTION]/[EVENT] tags the live pipeline already parses.
// ActionInterpreterPromptBuilder is the contract that makes that possible. These tests pin the two properties the
// spike depends on and cannot verify in-game cheaply: (1) the prompt is assembled STABLE-PREFIX FIRST, so a
// provider can prompt-cache the invariant head across every interpreter call (the whole cost argument for a
// two-call chain), and (2) an interpreter OUTPUT written in the taught format round-trips through the REAL
// SectionResponseParser into the expected actions and event, proving the builder teaches the parser's own format
// and not a new one. If either breaks, the spike is invalid: caching savings evaporate, or the interpreter's tags
// never reach the game.

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
   public class ActionInterpreterPromptBuilderTests
   {
      private const string SampleContextFacts = "NPC: Caladog of Fen Company (Battanian). Current regard toward player: +12.";
      private const string SampleProse = "*The warmth drains from his face.* Cowards. Get off my land. *He turns his shoulder to you, done.*";

      // The cost case for the whole two-call chain is prompt caching: the interpreter's role and format never change,
      // so a provider must be able to cache them. That only works if the built prompt LITERALLY begins with the
      // invariant head. If per-turn data ever crept in ahead of it, the cache breakpoint would shift every call
      // and the caching savings that justify a second model would vanish.
      [Test]
      public void GIVEN_any_interpreter_prompt_WHEN_built_THEN_it_starts_with_the_stable_prefix()
      {
         string prompt = ActionInterpreterPromptBuilder.Build(SampleProse, SampleContextFacts);

         prompt.Should().StartWith(ActionInterpreterPromptBuilder.StablePrefix);
      }

      // The interpreter is worthless if the stable head does not actually teach the three reactive signals the spike
      // scopes: the regard shift (change_relation), the memory line ([EVENT]), and the break-off (end_conversation).
      // These are the tokens the parser keys on, so their literal presence in the cacheable prefix is the vocabulary
      // the interpreter is being taught.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_teaches_the_three_core_reactive_tags()
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefix;

         prefix.Should().Contain("change_relation");
         prefix.Should().Contain("[EVENT]");
         prefix.Should().Contain("end_conversation");
      }

      // The single sentence that makes an ACTION INTERPRETER different from a roleplay model: it must NOT continue or
      // rewrite the prose it is handed, only tag it. Without this rule a chat-tuned model reads the prose as a turn to
      // answer and writes fresh dialogue, which the parser would then mine for tags from text the NPC never said.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_carries_the_do_not_rewrite_rule()
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefix;

         prefix.Should().Contain("ALREADY WRITTEN");
         prefix.Should().Contain("Do NOT rewrite");
      }

      // A real spike run had the interpreter label the player "the coward" in the memory summary, because nothing
      // told it how to refer to the player and the old wording ("never a name") pushed it to an epithet. The
      // memory record must name the player consistently, or the NPC's remembered history rots into a mix of
      // epithets that also bakes a one-time insult into a permanent record.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_names_the_player_and_forbids_epithets()
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefix;

         prefix.Should().Contain("Refer to the PLAYER by the name given in the facts");
         prefix.Should().Contain("the coward"); // named as a forbidden example, not a licence
      }

      // A real run had an NPC press a purse into the player's hands in the prose, yet no give_gold was emitted, so the
      // gift never became concrete (the player's coin never moved). The interpreter must be taught the economic tags
      // with their DIRECTION rule, or money that changes hands in the prose is silently lost from the game state.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_teaches_give_gold_and_take_gold_with_direction()
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefix;

         prefix.Should().Contain("give_gold");
         prefix.Should().Contain("take_gold");
         prefix.Should().Contain("amount:");
         // The one rule that keeps the two apart: who hands coin to whom.
         prefix.Should().Contain("Direction is what matters");
      }

      // The give_gold round-trip: an interpreter reading "she pressed a purse into your hands" must produce a
      // give_gold the live parser turns into a real transfer with an amount. Without this the prose depicts a gift the
      // game never grants, exactly the disconnect this tag exists to close.
      [Test]
      public void GIVEN_a_give_gold_interpreter_output_WHEN_parsed_THEN_it_yields_a_give_gold_action_with_an_amount()
      {
         const string interpreterOutput =
            "[ACTION]\n" +
            "type: give_gold\n" +
            "amount: 100\n" +
            "[/ACTION]\n" +
            "[EVENT]\n" +
            "type: collaboration\n" +
            "summary: I pressed a purse into the player's hands to see them through the winter.\n" +
            "[/EVENT]";

         var parser = new SectionResponseParser();
         ParsedResponse parsed = parser.Parse(interpreterOutput);

         parsed.Actions.Should().ContainSingle(a => a.Type == "give_gold")
               .Which.Parameters["amount"].Should().Be("100");
      }

      // A real rp_bench run had the interpreter tag turns 2, 3, and 4 of the SAME ongoing conversation as
      // first_meeting, and separately tag turn 1 with a committed consort (regard +45, long history) as
      // first_meeting even though its own summary said "after twenty days apart" (it knew there was history). The
      // type list alone gave no guidance on WHEN first_meeting applies, so the model defaulted to it. This pins
      // that the prefix now restricts first_meeting to a genuine first-ever encounter, keyed on the regard/bond
      // facts the digest always carries and on this not being a later turn of an exchange already under way.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_restricts_first_meeting_to_a_genuine_first_encounter()
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefix;

         prefix.Should().Contain("first_meeting is ONLY for a genuine first-ever encounter");
         prefix.Should().Contain("any regard other than +0");
         prefix.Should().Contain("a later turn of an exchange already under way");
      }

      // The interpreter gets a situational digest (place, who is present, the player's standing) so memories are
      // grounded ("near Veron Castle"), but that context must ANCHOR only, never license invention: without this
      // guard the interpreter could write "he threatened me with his army" from a mere "player has an army" fact.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_grounds_in_facts_without_inventing()
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefix;

         prefix.Should().Contain("GROUND IN THE FACTS, DO NOT INVENT");
         prefix.Should().Contain("record ONLY what actually happened");
      }

      // Caching aside, correctness requires the per-turn data to sit AFTER the invariant head: the interpreter must
      // see the context facts and the prose it is meant to analyze, and they must be the variable tail, not woven
      // into the cached prefix (which would defeat caching and change the "stable" head every call).
      [Test]
      public void GIVEN_context_and_prose_WHEN_built_THEN_both_appear_after_the_prefix()
      {
         string prompt = ActionInterpreterPromptBuilder.Build(SampleProse, SampleContextFacts);

         int prefixEnd = ActionInterpreterPromptBuilder.StablePrefix.Length;
         prompt.IndexOf(SampleContextFacts, System.StringComparison.Ordinal).Should().BeGreaterThanOrEqualTo(prefixEnd);
         prompt.IndexOf(SampleProse, System.StringComparison.Ordinal).Should().BeGreaterThan(prompt.IndexOf(SampleContextFacts, System.StringComparison.Ordinal));
      }

      // The prose is the payload, and it must be clearly fenced off from the instructions so the model knows where
      // the reply-to-analyze begins. A missing delimiter is exactly what makes a model treat the whole thing as one
      // instruction and start answering.
      [Test]
      public void GIVEN_a_built_prompt_WHEN_inspected_THEN_the_prose_is_introduced_by_a_clear_delimiter()
      {
         string prompt = ActionInterpreterPromptBuilder.Build(SampleProse, SampleContextFacts);

         prompt.Should().Contain("REPLY TO ANALYZE:");
         prompt.IndexOf("REPLY TO ANALYZE:", System.StringComparison.Ordinal)
               .Should().BeLessThan(prompt.IndexOf(SampleProse, System.StringComparison.Ordinal));
      }

      // The whole point of the Unified Action Catalog (Stage 1): before this, the interpreter only knew 5 verbs
      // while the bridge dispatched 60+, so a real deed the prose narrated (a marriage, a granted fief, a
      // dispatched companion) could never be turned into a matching [ACTION] and silently never fired. This pins
      // that the catalog's verbs actually reached the taught prefix, spanning a governance verb (grant_fief), a
      // personal-command verb (dispatch_mission), and a romance verb (marry) so the whole breadth is covered, not
      // just one corner of it.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_it_teaches_a_sample_of_the_catalog_verbs_beyond_the_core_five()
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefix;

         prefix.Should().Contain("marry");
         prefix.Should().Contain("grant_fief");
         prefix.Should().Contain("dispatch_mission");
      }

      // The rich, hand-tuned wording for the five core reactive signals is the one thing this catalog wiring must
      // never weaken. If the catalog-rendered "OTHER ACTIONS" section were spliced in BEFORE them, or interleaved
      // with them, a provider's prompt cache would still work (the whole thing is still the stable prefix), but a
      // careless future edit could start treating the core block as just more catalog output and erode its
      // carefully-tuned framing. Pinning the ORDER (core signals first, catalog reference after) keeps the two
      // concerns visually and structurally separate.
      [Test]
      public void GIVEN_the_stable_prefix_WHEN_inspected_THEN_the_catalog_section_comes_after_the_core_five_signals()
      {
         string prefix = ActionInterpreterPromptBuilder.StablePrefix;

         int coreEnd = prefix.IndexOf("[/EVENT]", System.StringComparison.Ordinal);
         int catalogStart = prefix.IndexOf("OTHER ACTIONS YOU MAY EMIT", System.StringComparison.Ordinal);

         coreEnd.Should().BeGreaterThan(-1);
         catalogStart.Should().BeGreaterThan(coreEnd);
      }

      // The load-bearing claim of the whole spike: an interpreter OUTPUT written in the format this builder teaches
      // must parse, through the SAME SectionResponseParser the live chat uses, into a real change_relation with its
      // delta, an end_conversation, and an [EVENT] with type and summary. If this round-trip fails, the builder is
      // teaching a format the game cannot consume and the two-call chain cannot work.
      [Test]
      public void GIVEN_an_interpreter_output_in_the_taught_format_WHEN_parsed_THEN_it_yields_the_expected_actions_and_event()
      {
         // A faithful example of what the interpreter is asked to emit for the "grave insult, NPC ends it" case.
         const string interpreterOutput =
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
         ParsedResponse parsed = parser.Parse(interpreterOutput);

         parsed.Actions.Should().ContainSingle(a => a.Type == "change_relation")
               .Which.Parameters["delta"].Should().Be("-15");
         parsed.Actions.Should().Contain(a => a.Type == "end_conversation");

         parsed.NewEventData.Should().NotBeNull();
         parsed.NewEventData!.Type.Should().Be(NotableEventType.Conflict);
         parsed.NewEventData.Summary.Should().Contain("threw the player off my land");
      }

      // The intimacy sample the spike ships must also round-trip: a flirt/intimacy [EVENT] plus a small positive
      // regard, no end_conversation. This guards the OTHER end of the tag vocabulary (a warm beat, not a break-off)
      // so the format is proven across the range of prose the interpreter will see, not just the angry case.
      [Test]
      public void GIVEN_an_intimacy_interpreter_output_WHEN_parsed_THEN_it_yields_an_intimacy_event_and_positive_regard_with_no_end()
      {
         const string interpreterOutput =
            "[ACTION]\n" +
            "type: change_relation\n" +
            "delta: 3\n" +
            "[/ACTION]\n" +
            "[EVENT]\n" +
            "type: intimacy\n" +
            "summary: I gave myself to the player after twenty days apart.\n" +
            "[/EVENT]";

         var parser = new SectionResponseParser();
         ParsedResponse parsed = parser.Parse(interpreterOutput);

         parsed.Actions.Should().ContainSingle(a => a.Type == "change_relation")
               .Which.Parameters["delta"].Should().Be("3");
         parsed.Actions.Should().NotContain(a => a.Type == "end_conversation");

         parsed.NewEventData.Should().NotBeNull();
         parsed.NewEventData!.Type.Should().Be(NotableEventType.Intimacy);
      }
   }
}

// Code written by Gabriel Mailhot, 11/05/2026.
//
// SectionResponseParser is the seam between a stochastic text generator and the mod's persisted
// state: every dialogue line, memory, event, reputation delta and quest the player experiences
// passes through here first. The model WILL eventually emit something malformed (a missing close
// tag, a garbage line, an unrecognized token, a stray bracket it invented as a speaker label) and
// this parser is the only thing standing between that and either a broken conversation or corrupted
// state. Its contract throughout is: never throw, degrade gracefully, and never let one malformed
// section take an otherwise good response down with it. Note: [MEMORY] is parsed and tested here in
// full, but the shipped mod sets EnableMemoryBlock=false (the model is never asked to produce it) and
// only logs response.Memory for diagnostics, it is not persisted game state; this file is the SDK's
// generic regression suite, not CR-specific, and keeps the contract correct for any host that does
// request [MEMORY].

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class SectionResponseParserTests
   {
      private SectionResponseParser _parser = null!;

      // ---------- Dialogue extraction ----------

      // Baseline happy path: a well-formed [DIALOGUE] block returns exactly the spoken line,
      // trimmed, with no other section populated. Everything else in this file is a variation on
      // what happens when the input ISN'T this clean.
      [Test]
      public void Dialogue_only_with_tags_is_extracted_trimmed()
      {
         var raw = "[DIALOGUE]\nBonjour, voyageur.\n[/DIALOGUE]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("Bonjour, voyageur.");
         result.Memory.Should().BeNull();
      }

      // ---------- Degraded inputs ----------

      // A network hiccup or an empty completion must never crash the chat UI or leave stale data
      // from the previous turn behind: empty input in, all-empty/null sections out.
      [Test]
      public void Empty_input_returns_empty_dialogue_and_no_sections()
      {
         var result = _parser.Parse("");

         result.Dialogue.Should().BeEmpty();
         result.Memory.Should().BeNull();
         result.NewEventData.Should().BeNull();
         result.Reputation.Should().BeNull();
      }

      // ---------- Quest-block malformation is surfaced, never silent ----------

      // A quest-type token the model invented (not in the alias table) must not silently read as
      // "no quest offered": QuestBlockMalformed tells the player their task did NOT register,
      // instead of the spoken offer and the persisted state quietly diverging (ROADMAP,
      // "NOTHING-IS-SILENT", ratified 2026-07-08).
      [Test]
      public void A_quest_block_with_an_unknown_type_flags_malformed_instead_of_vanishing()
      {
         var raw = "[DIALOGUE]Go do this for me.[/DIALOGUE]\n[QUEST]\ntype: escort_caravan\ndescription: See them safe.\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().BeNull();
         result.QuestBlockMalformed.Should().BeTrue();
      }

      // Same family: a [QUEST] block with no type token at all is just as malformed as an
      // unrecognized one, and must be flagged the same way.
      [Test]
      public void A_quest_block_missing_its_type_flags_malformed()
      {
         var raw = "[QUEST]\ndescription: A task with no type token.\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().BeNull();
         result.QuestBlockMalformed.Should().BeTrue();
      }

      // The negative-space check for the two tests above: a normal, well-formed quest offer must
      // never trip the malformed warning. A false positive here would tell a truthful player their
      // fine offer "came through garbled" when it registered correctly.
      [Test]
      public void A_wellformed_quest_block_parses_and_is_not_flagged()
      {
         var raw = "[QUEST]\ntype: bandit_clear\ntarget_settlement: Pravend\ndescription: Drive them off.\nreward_gold: 200\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.Type.Should().Be(QuestType.BanditClear);
         result.QuestBlockMalformed.Should().BeFalse();
      }

      // Distinguishes "the NPC didn't offer a quest this turn" (silent, correct, the common case)
      // from "the NPC tried and failed" (flagged, tested above); the two must never be conflated,
      // or every ordinary reply without a quest would nag the player about a garbled offer.
      [Test]
      public void No_quest_block_at_all_is_not_flagged_malformed()
      {
         var result = _parser.Parse("[DIALOGUE]Fine weather.[/DIALOGUE]");

         result.QuestGiven.Should().BeNull();
         result.QuestBlockMalformed.Should().BeFalse();
      }

      // Pins that ProvideGoods' extra fields (category, required_count) flow through alongside the
      // common quest fields: this quest type resolves completion by counted goods delivered, not
      // just gold/relation, so losing either field here would make that quest uncompletable.
      [Test]
      public void A_provide_goods_quest_block_parses_its_category_and_count()
      {
         var raw = "[QUEST]\ntype: provide_goods\ncategory: horses\nrequired_count: 15\nreward_gold: 400\ndescription: My cavalry wants for mounts.\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.Type.Should().Be(QuestType.ProvideGoods);
         result.QuestGiven.Category.Should().Be("horses");
         result.QuestGiven.RequiredCount.Should().Be(15);
         result.QuestBlockMalformed.Should().BeFalse();
      }

      // Same alias-table principle as the quest-type table: each entry is a way a model naturally
      // phrases a notable-event category. A missed alias falls through to NotableEventType.Other
      // (tested below), which the memory/grudge systems can treat very differently from the
      // specific category the NPC actually meant.
      [TestCase("conflict", NotableEventType.Conflict)]
      [TestCase("betrayal", NotableEventType.Betrayal)]
      [TestCase("confrontation", NotableEventType.Confrontation)]
      [TestCase("flirt", NotableEventType.Flirt)]
      [TestCase("collaboration", NotableEventType.Collaboration)]
      [TestCase("first_meeting", NotableEventType.FirstMeeting)]
      [TestCase("meeting", NotableEventType.FirstMeeting)]
      [TestCase("intimacy", NotableEventType.Intimacy)]
      public void Event_type_aliases_are_recognized(string rawType, NotableEventType expected)
      {
         var raw = $"[EVENT]\ntype: {rawType}\nsummary: x\n[/EVENT]";

         var result = _parser.Parse(raw);

         result.NewEventData.Should().NotBeNull();
         result.NewEventData!.Type.Should().Be(expected);
      }

      // ---------- Full well-formed response ----------

      // Integration-level pin: all four sections coexist and are extracted independently from a
      // single well-formed reply. Guards against a change to one section's parsing (e.g. REPUTATION)
      // silently breaking extraction of another (e.g. MEMORY) sharing the same response.
      [Test]
      public void Full_response_extracts_all_four_sections()
      {
         var raw =
            "[DIALOGUE]\nTu m'as trahi à Ustokh.\n[/DIALOGUE]\n" + "[MEMORY]\n" + "topic: confrontation_betrayal\n" + "sentiment: marked_hostility\n" + "decision: refused_alliance\n" + "[/MEMORY]\n" + "[EVENT]\n" + "type: confrontation\n" + "summary: Player confronted about Ustokh.\n" + "[/EVENT]\n" + "[REPUTATION]\n" + "clan_delta: -5\n" + "faction_delta: -2\n" + "[/REPUTATION]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("Tu m'as trahi à Ustokh.");

         result.Memory.Should().NotBeNull();
         result.Memory!.Topic.Should().Be("confrontation_betrayal");
         result.Memory.Sentiment.Should().Be("marked_hostility");
         result.Memory.Decision.Should().Be("refused_alliance");

         result.NewEventData.Should().NotBeNull();
         result.NewEventData!.Type.Should().Be(NotableEventType.Confrontation);
         result.NewEventData.Summary.Should().Be("Player confronted about Ustokh.");

         result.Reputation.Should().NotBeNull();
         result.Reputation!.ClanDelta.Should().Be(-5);
         result.Reputation.FactionDelta.Should().Be(-2);
      }

      // Models sometimes echo the prompt's own example-value markup (a leading "#") back verbatim
      // instead of substituting a real value. Stripping it prevents a leading "#" contaminating
      // stored topic/sentiment strings that later feed memory retrieval or logging.
      [Test]
      public void Hash_prefix_on_values_is_stripped()
      {
         var raw = "[MEMORY]\ntopic: #alliance\nsentiment: #suspicion\n[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Memory.Should().NotBeNull();
         result.Memory!.Topic.Should().Be("alliance");
         result.Memory.Sentiment.Should().Be("suspicion");
      }

      // A stray non-"key: value" line inside a section (a model aside, a malformed continuation)
      // must not corrupt or abort parsing of the real fields around it, just be skipped in place.
      [Test]
      public void Lines_without_colon_in_kv_section_are_silently_ignored()
      {
         var raw =
            "[MEMORY]\n" + "topic: a\n" + "this line is garbage\n" + "sentiment: b\n" + "[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Memory.Should().NotBeNull();
         result.Memory!.Topic.Should().Be("a");
         result.Memory.Sentiment.Should().Be("b");
      }

      // A section that never got its closing tag (the model was cut off, or moved straight to the
      // next section) must be dropped WITHOUT taking a later, well-formed section down with it:
      // here the broken [MEMORY] is lost but [REPUTATION] still parses.
      [Test]
      public void Malformed_section_missing_closing_tag_is_skipped_others_survive()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" + "[MEMORY]\ntopic: x\nsentiment: y\n" + "[REPUTATION]\nclan_delta: 4\n[/REPUTATION]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("hi");
         result.Memory.Should().BeNull();
         result.Reputation.Should().NotBeNull();
         result.Reputation!.ClanDelta.Should().Be(4);
      }

      // Decision is optional: an NPC musing without reaching a decision must still produce a valid
      // ConversationMemory rather than being dropped entirely for lacking a field nothing requires.
      [Test]
      public void Memory_without_optional_decision_still_parses()
      {
         var raw = "[MEMORY]\ntopic: a\nsentiment: b\n[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Memory.Should().NotBeNull();
         result.Memory!.Decision.Should().BeNull();
      }

      // ---------- Memory section behaviors ----------

      // Topic is required (unlike Decision): a [MEMORY] block that never states what it's about
      // carries no usable information and must be dropped rather than stored with a blank topic.
      [Test]
      public void Memory_without_required_topic_returns_null()
      {
         var raw = "[MEMORY]\nsentiment: x\ndecision: y\n[/MEMORY]";
         var result = _parser.Parse(raw);
         result.Memory.Should().BeNull();
      }

      // Dialogue is the one field ParsedResponse guarantees is never null. Weaker/local models
      // frequently skip the bracketed format altogether; without this fallback the player would
      // see a blank NPC turn instead of at least the raw text the model actually produced.
      [Test]
      public void Missing_dialogue_tag_falls_back_to_text_before_first_section()
      {
         var raw =
            "Some dialogue here.\n" + "[MEMORY]\ntopic: x\nsentiment: y\n[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("Some dialogue here.");
         result.Memory.Should().NotBeNull();
      }

      // Same fallback, no section tags at all: the entire reply is treated as dialogue rather than
      // discarded, since Dialogue is the only field guaranteed non-null.
      [Test]
      public void Plain_text_without_any_tag_is_used_as_dialogue()
      {
         var result = _parser.Parse("Just plain text.");
         result.Dialogue.Should().Be("Just plain text.");
      }

      // Models often write deltas with an explicit leading "+"; this must parse as a normal signed
      // int rather than being rejected as unparseable, which would silently swallow a real positive
      // reputation change.
      [Test]
      public void Positive_reputation_delta_is_parsed()
      {
         var raw = "[REPUTATION]\nfaction_delta: +7\n[/REPUTATION]";

         var result = _parser.Parse(raw);

         result.Reputation.Should().NotBeNull();
         result.Reputation!.FactionDelta.Should().Be(7);
      }

      // Garbage instead of a number must not crash, and must not silently coerce to a bogus 0:
      // a fabricated "0" would look identical to an intentional zero-delta, so an unparseable
      // value has to fall through to null/dropped instead.
      [Test]
      public void Reputation_with_non_numeric_value_is_ignored()
      {
         var raw = "[REPUTATION]\nclan_delta: notanumber\n[/REPUTATION]";
         var result = _parser.Parse(raw);
         result.Reputation.Should().BeNull();
      }

      // ---------- Reputation behaviors ----------

      // ClanDelta and FactionDelta are independent: supplying one must not fabricate a value for
      // the other, which would apply a relation change the NPC never actually stated.
      [Test]
      public void Reputation_with_only_clan_delta_leaves_faction_null()
      {
         var raw = "[REPUTATION]\nclan_delta: -3\n[/REPUTATION]";

         var result = _parser.Parse(raw);

         result.Reputation.Should().NotBeNull();
         result.Reputation!.ClanDelta.Should().Be(-3);
         result.Reputation.FactionDelta.Should().BeNull();
      }

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      // ---------- Tolerance & robustness ----------

      // Models are inconsistent about tag casing (lowercase, mixed case). Case-sensitive matching
      // would silently drop an entire section that the model wrote perfectly well, just not in the
      // exact casing the parser expected.
      [Test]
      public void Tags_are_case_insensitive()
      {
         var raw = "[dialogue]hi[/Dialogue][memory]topic: a\nsentiment: b[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("hi");
         result.Memory.Should().NotBeNull();
      }

      // ---------- Event type mapping ----------

      // An event type token outside the known alias table must not be dropped or crash: it falls
      // to NotableEventType.Other so the event (and its summary) is still recorded, rather than
      // vanishing entirely just because the model phrased the category in a new way.
      [Test]
      public void Unknown_event_type_maps_to_Other_and_keeps_summary()
      {
         var raw = "[EVENT]\ntype: weird_thing\nsummary: Strange.\n[/EVENT]";

         var result = _parser.Parse(raw);

         result.NewEventData.Should().NotBeNull();
         result.NewEventData!.Type.Should().Be(NotableEventType.Other);
         result.NewEventData.Summary.Should().Be("Strange.");
      }

      // Guards the same "never crash on garbage" contract as the empty-input case, for a
      // whitespace-only completion (e.g. a truncated or degenerate LLM response).
      [Test]
      public void Whitespace_input_returns_empty_dialogue()
      {
         var result = _parser.Parse("   \n  \t ");
         result.Dialogue.Should().BeEmpty();
      }

      // ---------- Bracket-scrub anchoring (leading label only) ----------

      // Weaker models sometimes prefix the line with a stray bracketed label (their own name, or a
      // tag they invented) instead of using the *asterisks* convention for action. That label must
      // not leak into the displayed dialogue line the player reads.
      [Test]
      public void Leading_stray_bracketed_label_is_stripped()
      {
         var raw = "[DIALOGUE]\n[Vesha the Crow] Bonjour, voyageur.\n[/DIALOGUE]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("Bonjour, voyageur.");
      }

      // The scrub must be anchored to the LEADING position only: a bracketed token elsewhere in the
      // body (e.g. a quoted "[unreadable]" in prose) is legitimate dialogue content, not a stray
      // speaker label, and stripping it would corrupt the NPC's actual line.
      [Test]
      public void Mid_dialogue_bracketed_token_is_preserved()
      {
         var raw = "[DIALOGUE]\nHe signed it \"[unreadable]\" and left.\n[/DIALOGUE]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("He signed it \"[unreadable]\" and left.");
      }

      // The scrub fires AT MOST ONCE: a second bracketed token right after the first is real
      // dialogue content (or the model's own error), not a second speaker label to also strip.
      // Over-eager stripping here would eat into the actual spoken line.
      [Test]
      public void Only_the_first_leading_bracketed_token_is_stripped()
      {
         var raw = "[DIALOGUE]\n[Vesha] [still bracketed] rest of line.\n[/DIALOGUE]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("[still bracketed] rest of line.");
      }
   }
}
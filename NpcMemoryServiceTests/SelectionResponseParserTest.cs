// Code written by Gabriel Mailhot, 11/05/2026.

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

      [Test]
      public void Dialogue_only_with_tags_is_extracted_trimmed()
      {
         var raw = "[DIALOGUE]\nBonjour, voyageur.\n[/DIALOGUE]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("Bonjour, voyageur.");
         result.Memory.Should().BeNull();
      }

      // ---------- Degraded inputs ----------

      [Test]
      public void Empty_input_returns_empty_dialogue_and_no_sections()
      {
         var result = _parser.Parse("");

         result.Dialogue.Should().BeEmpty();
         result.Memory.Should().BeNull();
         result.NewEventData.Should().BeNull();
         result.Reputation.Should().BeNull();
      }

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

      [Test]
      public void Hash_prefix_on_values_is_stripped()
      {
         var raw = "[MEMORY]\ntopic: #alliance\nsentiment: #suspicion\n[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Memory.Should().NotBeNull();
         result.Memory!.Topic.Should().Be("alliance");
         result.Memory.Sentiment.Should().Be("suspicion");
      }

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

      [Test]
      public void Memory_without_optional_decision_still_parses()
      {
         var raw = "[MEMORY]\ntopic: a\nsentiment: b\n[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Memory.Should().NotBeNull();
         result.Memory!.Decision.Should().BeNull();
      }

      // ---------- Memory section behaviors ----------

      [Test]
      public void Memory_without_required_topic_returns_null()
      {
         var raw = "[MEMORY]\nsentiment: x\ndecision: y\n[/MEMORY]";
         var result = _parser.Parse(raw);
         result.Memory.Should().BeNull();
      }

      [Test]
      public void Missing_dialogue_tag_falls_back_to_text_before_first_section()
      {
         var raw =
            "Some dialogue here.\n" + "[MEMORY]\ntopic: x\nsentiment: y\n[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("Some dialogue here.");
         result.Memory.Should().NotBeNull();
      }

      [Test]
      public void Plain_text_without_any_tag_is_used_as_dialogue()
      {
         var result = _parser.Parse("Just plain text.");
         result.Dialogue.Should().Be("Just plain text.");
      }

      [Test]
      public void Positive_reputation_delta_is_parsed()
      {
         var raw = "[REPUTATION]\nfaction_delta: +7\n[/REPUTATION]";

         var result = _parser.Parse(raw);

         result.Reputation.Should().NotBeNull();
         result.Reputation!.FactionDelta.Should().Be(7);
      }

      [Test]
      public void Reputation_with_non_numeric_value_is_ignored()
      {
         var raw = "[REPUTATION]\nclan_delta: notanumber\n[/REPUTATION]";
         var result = _parser.Parse(raw);
         result.Reputation.Should().BeNull();
      }

      // ---------- Reputation behaviors ----------

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

      [Test]
      public void Tags_are_case_insensitive()
      {
         var raw = "[dialogue]hi[/Dialogue][memory]topic: a\nsentiment: b[/MEMORY]";

         var result = _parser.Parse(raw);

         result.Dialogue.Should().Be("hi");
         result.Memory.Should().NotBeNull();
      }

      // ---------- Event type mapping ----------

      [Test]
      public void Unknown_event_type_maps_to_Other_and_keeps_summary()
      {
         var raw = "[EVENT]\ntype: weird_thing\nsummary: Strange.\n[/EVENT]";

         var result = _parser.Parse(raw);

         result.NewEventData.Should().NotBeNull();
         result.NewEventData!.Type.Should().Be(NotableEventType.Other);
         result.NewEventData.Summary.Should().Be("Strange.");
      }

      [Test]
      public void Whitespace_input_returns_empty_dialogue()
      {
         var result = _parser.Parse("   \n  \t ");
         result.Dialogue.Should().BeEmpty();
      }
   }
}
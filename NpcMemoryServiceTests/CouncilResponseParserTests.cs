// Code written by Gabriel Mailhot, 22/08/2026.
// CouncilResponseParser reads the council's new ONE-CALL GROUP SCENE output (see CouncilPromptBuilder): an
// optional shared [SCENE], one or more [SPEAKER: Name] blocks (every seated member, possibly more than once for
// cross-talk), then zero or more [RESOLUTION] and actor-attributed change_relation [ACTION] blocks. These tests
// pin the contract the mod's later rendering increment depends on: each speaker resolved against the real
// roster (tolerantly), cross-talk preserved in order, the [RESOLUTION] shape unchanged from the existing
// council catalogue, and a parser that never throws on a blank or malformed reply.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CouncilResponseParserTests
   {
      private CouncilResponseParser _parser = null!;
      private static readonly IReadOnlyList<string> Seats = new List<string> {"Ajin the Hawk", "Hophtalamos the Shipwright"};

      [SetUp]
      public void SetUp() => _parser = new CouncilResponseParser();

      // Baseline: the whole point of the rebuild is that EVERY seated member's own block survives, attributed to
      // them individually, not folded into one undifferentiated reply.
      [Test]
      public void GIVEN_two_speaker_blocks_WHEN_parsing_THEN_both_are_split_and_attributed_in_order()
      {
         var raw =
            "[SPEAKER: Ajin the Hawk]\nThe roads grow unsafe, my lord.\n" +
            "[SPEAKER: Hophtalamos the Shipwright]\nMy ships have seen it too.";

         var result = _parser.Parse(raw, Seats);

         result.Contributions.Should().HaveCount(2);
         result.Contributions[0].SpeakerName.Should().Be("Ajin the Hawk");
         result.Contributions[0].Text.Should().Be("The roads grow unsafe, my lord.");
         result.Contributions[1].SpeakerName.Should().Be("Hophtalamos the Shipwright");
         result.Contributions[1].Text.Should().Be("My ships have seen it too.");
      }

      // The equal-participation contract's own worked example: a member takes a SECOND block for cross-talk
      // (a retort). Both must survive, in order, as two separate contributions from the same speaker, or a
      // reply reading naturally as back-and-forth collapses into one merged block.
      [Test]
      public void GIVEN_a_member_speaks_twice_for_cross_talk_WHEN_parsing_THEN_both_blocks_are_kept_separately_in_order()
      {
         var raw =
            "[SPEAKER: Ajin the Hawk]\nWe should ride at once.\n" +
            "[SPEAKER: Hophtalamos the Shipwright]\nToo hasty, Ajin.\n" +
            "[SPEAKER: Ajin the Hawk]\nHasty keeps our people alive.";

         var result = _parser.Parse(raw, Seats);

         result.Contributions.Should().HaveCount(3);
         result.Contributions[0].SpeakerName.Should().Be("Ajin the Hawk");
         result.Contributions[1].SpeakerName.Should().Be("Hophtalamos the Shipwright");
         result.Contributions[2].SpeakerName.Should().Be("Ajin the Hawk");
         result.Contributions[2].Text.Should().Be("Hasty keeps our people alive.");
      }

      // [SCENE] is shared narration belonging to no single speaker: it must be lifted out on its own, never
      // folded into the first member's own spoken block.
      [Test]
      public void GIVEN_a_closed_scene_block_WHEN_parsing_THEN_it_is_extracted_separately_from_the_speakers()
      {
         var raw =
            "[SCENE]The fire crackles; every eye turns to the map.[/SCENE]\n" +
            "[SPEAKER: Ajin the Hawk]\nHere is where they struck.";

         var result = _parser.Parse(raw, Seats);

         result.SceneNarration.Should().Be("The fire crackles; every eye turns to the map.");
         result.Contributions.Should().HaveCount(1);
         result.Contributions[0].Text.Should().Be("Here is where they struck.");
      }

      // Truncation net, same discipline as SectionResponseParser's own NARRATION/QUEST tolerance: a reply cut
      // off by the token limit opens [SCENE] and never reaches [/SCENE]. Losing it silently would mean the
      // opening mood the model spent tokens on simply vanishes.
      [Test]
      public void GIVEN_an_unclosed_scene_block_WHEN_parsing_THEN_it_is_still_recovered_up_to_the_next_speaker()
      {
         var raw = "[SCENE]A tense hush falls over the hall.\n[SPEAKER: Ajin the Hawk]\nLet us begin.";

         var result = _parser.Parse(raw, Seats);

         result.SceneNarration.Should().Be("A tense hush falls over the hall.");
         result.Contributions[0].Text.Should().Be("Let us begin.");
      }

      // No [SCENE] at all is the ordinary case (most turns need none): must be null, never an empty string a
      // consumer would render as a blank narration bubble.
      [Test]
      public void GIVEN_no_scene_block_WHEN_parsing_THEN_scene_narration_is_null()
      {
         var raw = "[SPEAKER: Ajin the Hawk]\nHere is where they struck.";

         _parser.Parse(raw, Seats).SceneNarration.Should().BeNull();
      }

      // The [RESOLUTION] format is byte-for-byte the same as the existing (older) council path's own: all four
      // baseline fields must parse, or the mod's existing CouncilLift.SettleQuest silently loses grounding data
      // for a resolution recorded through this new one-call path specifically.
      [Test]
      public void GIVEN_a_well_formed_quest_resolution_WHEN_parsing_THEN_all_four_fields_are_parsed()
      {
         var raw =
            "[SPEAKER: Ajin the Hawk]\nConsider it done.\n" +
            "[RESOLUTION]\ntype: quest\nactor: Ajin the Hawk\ntarget_settlement: Pravend\ndetail: ride to Pravend and clear the bandits\n[/RESOLUTION]";

         var result = _parser.Parse(raw, Seats);

         result.Resolutions.Should().HaveCount(1);
         result.Resolutions[0].Type.Should().Be("quest");
         result.Resolutions[0].Actor.Should().Be("Ajin the Hawk");
         result.Resolutions[0].TargetSettlement.Should().Be("Pravend");
         result.Resolutions[0].Detail.Should().Be("ride to Pravend and clear the bandits");
      }

      // The other seated member may ALSO pledge in the same reply (this is the whole point of equal
      // participation): both resolutions must survive, in order.
      [Test]
      public void GIVEN_two_resolutions_from_two_different_actors_WHEN_parsing_THEN_both_are_parsed_in_order()
      {
         var raw =
            "[SPEAKER: Ajin the Hawk]\nI will ride out.\n" +
            "[RESOLUTION]\ntype: quest\nactor: Ajin the Hawk\ntarget_settlement: Pravend\ndetail: clear the bandits\n[/RESOLUTION]\n" +
            "[SPEAKER: Hophtalamos the Shipwright]\nAnd I will fund the ships.\n" +
            "[RESOLUTION]\ntype: give_gold\nactor: Hophtalamos the Shipwright\ntarget_amount: 200\ndetail: fund the fleet\n[/RESOLUTION]";

         var result = _parser.Parse(raw, Seats);

         result.Resolutions.Should().HaveCount(2);
         result.Resolutions[0].Actor.Should().Be("Ajin the Hawk");
         result.Resolutions[1].Actor.Should().Be("Hophtalamos the Shipwright");
         result.Resolutions[1].TargetAmount.Should().Be(200);
      }

      // The regard-is-real channel: an actor-attributed change_relation must be recovered with both fields, or
      // the "your regard is real" guarantee (ratified 2026-07-24) has nothing to act on.
      [Test]
      public void GIVEN_an_actor_attributed_change_relation_action_WHEN_parsing_THEN_actor_and_delta_are_parsed()
      {
         var raw =
            "[SPEAKER: Hophtalamos the Shipwright]\nWell spoken, Ajin.\n" +
            "[ACTION]\ntype: change_relation\nactor: Hophtalamos the Shipwright\ndelta: 2\n[/ACTION]";

         var result = _parser.Parse(raw, Seats);

         result.RelationShifts.Should().HaveCount(1);
         result.RelationShifts[0].Actor.Should().Be("Hophtalamos the Shipwright");
         result.RelationShifts[0].Delta.Should().Be(2);
      }

      // More than one member may feel moved in the same reply: both shifts must survive independently.
      [Test]
      public void GIVEN_two_change_relation_actions_from_different_actors_WHEN_parsing_THEN_both_shifts_are_parsed()
      {
         var raw =
            "[SPEAKER: Ajin the Hawk]\nI trust you less for that.\n" +
            "[ACTION]\ntype: change_relation\nactor: Ajin the Hawk\ndelta: -3\n[/ACTION]\n" +
            "[SPEAKER: Hophtalamos the Shipwright]\nA fair point, well made.\n" +
            "[ACTION]\ntype: change_relation\nactor: Hophtalamos the Shipwright\ndelta: 1\n[/ACTION]";

         var result = _parser.Parse(raw, Seats);

         result.RelationShifts.Should().HaveCount(2);
         result.RelationShifts[0].Delta.Should().Be(-3);
         result.RelationShifts[1].Delta.Should().Be(1);
      }

      // Tolerant match, case: a model that lower-cases or otherwise re-cases a listed name must still resolve to
      // the real seat, exactly as SectionResponseParser tolerates a model's own casing drift elsewhere.
      [Test]
      public void GIVEN_a_speaker_name_in_different_casing_WHEN_parsing_THEN_it_still_resolves_to_the_seated_name()
      {
         var raw = "[SPEAKER: AJIN THE HAWK]\nHere is where they struck.";

         var result = _parser.Parse(raw, Seats);

         result.Contributions[0].SpeakerName.Should().Be("Ajin the Hawk");
         result.Contributions[0].SpeakerMatched.Should().BeTrue();
      }

      // Tolerant match, first name only: a model naming just "Ajin" for a seat listed as "Ajin the Hawk" must
      // still resolve, or a shortened, perfectly natural reference to a well-known seat would wrongly flag as
      // unrecognised.
      [Test]
      public void GIVEN_a_speaker_named_by_first_name_only_WHEN_parsing_THEN_it_resolves_to_the_full_seated_name()
      {
         var raw = "[SPEAKER: Ajin]\nHere is where they struck.";

         var result = _parser.Parse(raw, Seats);

         result.Contributions[0].SpeakerName.Should().Be("Ajin the Hawk");
         result.Contributions[0].SpeakerMatched.Should().BeTrue();
      }

      // Tolerant match, contains fallback: a model that adds a title or epithet the roster does not carry (or
      // the reverse) must still resolve via a substring match either direction.
      [Test]
      public void GIVEN_a_speaker_named_with_an_extra_title_WHEN_parsing_THEN_it_still_resolves_via_the_contains_fallback()
      {
         var raw = "[SPEAKER: Lord Ajin the Hawk]\nHere is where they struck.";

         var result = _parser.Parse(raw, Seats);

         result.Contributions[0].SpeakerName.Should().Be("Ajin the Hawk");
         result.Contributions[0].SpeakerMatched.Should().BeTrue();
      }

      // A name matching NO seat at all (a hallucinated or misspelled member) must be KEPT, never dropped, with
      // its raw text and a clear flag, so the mod can decide how to render an unmatched block rather than the
      // parser silently losing a spoken line.
      [Test]
      public void GIVEN_a_speaker_name_matching_no_seat_WHEN_parsing_THEN_the_block_is_kept_with_its_raw_name_and_flagged_unmatched()
      {
         var raw = "[SPEAKER: A Stranger At The Door]\nWho let this man in?";

         var result = _parser.Parse(raw, Seats);

         result.Contributions.Should().HaveCount(1);
         result.Contributions[0].SpeakerName.Should().Be("A Stranger At The Door");
         result.Contributions[0].SpeakerMatched.Should().BeFalse();
      }

      // Robustness net: a completely blank reply (a dropped call, an empty completion) must degrade to an
      // empty-but-non-null result, since the mod's rendering loop iterates every list unconditionally.
      [Test]
      public void GIVEN_a_blank_response_WHEN_parsing_THEN_an_empty_but_non_null_result_is_returned()
      {
         var result = _parser.Parse("", Seats);

         result.Should().NotBeNull();
         result.SceneNarration.Should().BeNull();
         result.Contributions.Should().BeEmpty();
         result.Resolutions.Should().BeEmpty();
         result.RelationShifts.Should().BeEmpty();
      }

      // Robustness net: a reply carrying no recognised tag at all (a model that ignored the format entirely)
      // must degrade the same way as a blank one, never throw.
      [Test]
      public void GIVEN_a_response_with_no_recognised_tags_WHEN_parsing_THEN_an_empty_but_non_null_result_is_returned()
      {
         var result = _parser.Parse("The model simply wrote a paragraph of plain prose.", Seats);

         result.Contributions.Should().BeEmpty();
         result.Resolutions.Should().BeEmpty();
         result.RelationShifts.Should().BeEmpty();
      }

      // Missing type on a [RESOLUTION] mirrors SectionResponseParser's own "not a decision at all" rule: it must
      // be silently skipped rather than recorded with a null/guessed type the lift could never settle.
      [Test]
      public void GIVEN_a_resolution_missing_type_WHEN_parsing_THEN_it_is_silently_skipped()
      {
         var raw =
            "[SPEAKER: Ajin the Hawk]\nSomething vague.\n" +
            "[RESOLUTION]\nactor: Ajin the Hawk\ndetail: no type here\n[/RESOLUTION]";

         _parser.Parse(raw, Seats).Resolutions.Should().BeEmpty();
      }

      // A change_relation whose delta was left un-parseable (garbage, no digits) must default to 0 rather than
      // throw, mirroring TryParseSignedInt's own tolerance everywhere else in the SDK.
      [Test]
      public void GIVEN_a_change_relation_action_with_an_unparseable_delta_WHEN_parsing_THEN_the_delta_defaults_to_zero()
      {
         var raw = "[SPEAKER: Ajin the Hawk]\nHmph.\n[ACTION]\ntype: change_relation\nactor: Ajin the Hawk\ndelta: unclear\n[/ACTION]";

         var result = _parser.Parse(raw, Seats);

         result.RelationShifts.Should().HaveCount(1);
         result.RelationShifts[0].Delta.Should().Be(0);
      }

      // Null seated-roster net: a caller that has not yet resolved its own roster into names must not crash the
      // parser; every speaker simply comes back unmatched.
      [Test]
      public void GIVEN_a_null_seated_names_list_WHEN_parsing_THEN_speakers_are_returned_unmatched_rather_than_throwing()
      {
         var raw = "[SPEAKER: Ajin the Hawk]\nHere is where they struck.";

         var result = _parser.Parse(raw, null!);

         result.Contributions.Should().HaveCount(1);
         result.Contributions[0].SpeakerMatched.Should().BeFalse();
         result.Contributions[0].SpeakerName.Should().Be("Ajin the Hawk");
      }
   }
}

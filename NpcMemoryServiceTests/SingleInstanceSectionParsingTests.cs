// Code written by Gabriel Mailhot, 01/07/2026.
//
// [EVENT]/[MEMORY]/[REPUTATION]/[STANCE]/[DISCOVERY] only ever take their FIRST block: ExtractSection
// stops at the first closing tag it finds. Only [ACTION] and [WITNESS_REACTION] are collected as
// lists. A model that repeats itself or "corrects itself" mid-reply with a second EVENT, DISCOVERY
// or REPUTATION block must still resolve to exactly ONE outcome, never two, or a single exchange
// could double-fire as two distinct notable events, two conflicting reputation deltas, or two
// different "discovered traits" from what the NPC actually said once. These tests pin WHICH one
// wins: the first.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Documents that single-instance sections (<see cref="SectionResponseParser.ExtractSection" />
   ///   callers such as <c>[EVENT]</c>, <c>[MEMORY]</c>, <c>[REPUTATION]</c>, <c>[STANCE]</c>,
   ///   <c>[DISCOVERY]</c>) take only the FIRST matching block when the LLM (incorrectly) emits
   ///   more than one — unlike <c>[ACTION]</c> and <c>[WITNESS_REACTION]</c>, which are collected.
   /// </summary>
   [TestFixture]
   public class SingleInstanceSectionParsingTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      // A duplicated [EVENT] must not register twice in the NPC's notable-event history, or a
      // single exchange would read back later as two separate incidents that never both happened.
      [Test]
      public void Two_event_blocks_only_the_first_is_kept()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[EVENT]\ntype: conflict\nsummary: First clash.\n[/EVENT]\n" +
            "[EVENT]\ntype: collaboration\nsummary: Second, ignored.\n[/EVENT]";

         var result = _parser.Parse(raw);

         result.NewEventData.Should().NotBeNull();
         result.NewEventData!.Type.Should().Be(NotableEventType.Conflict);
         result.NewEventData.Summary.Should().Be("First clash.");
      }

      // A model repeating or revising itself could emit two different "discovered traits" from
      // one exchange when only one was actually revealed; the spurious second block must lose,
      // not both get appended to the NPC's DiscoveredTraits.
      [Test]
      public void Two_discovery_blocks_only_the_first_is_kept()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[DISCOVERY]\nkey: orientation\ndescription: First.\n[/DISCOVERY]\n" +
            "[DISCOVERY]\nkey: archetype\ndescription: Second, ignored.\n[/DISCOVERY]";

         var result = _parser.Parse(raw);

         result.Discovery.Should().NotBeNull();
         result.Discovery!.Key.Should().Be("orientation");
      }

      // Two REPUTATION blocks in one reply must not sum or override into a compounded/reversed
      // relation swing (5 then -99 must never net to -99); first-wins keeps a single well-formed
      // delta authoritative instead of letting a second, possibly contradictory one take over.
      [Test]
      public void Two_reputation_blocks_only_the_first_is_kept()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[REPUTATION]\nclan_delta: 5\n[/REPUTATION]\n" +
            "[REPUTATION]\nclan_delta: -99\n[/REPUTATION]";

         var result = _parser.Parse(raw);

         result.Reputation.Should().NotBeNull();
         result.Reputation!.ClanDelta.Should().Be(5);
      }
   }
}

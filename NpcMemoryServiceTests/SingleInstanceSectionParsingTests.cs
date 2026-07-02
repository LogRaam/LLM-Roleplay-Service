// Code written by Gabriel Mailhot, 01/07/2026.

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

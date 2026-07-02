// Code written by Gabriel Mailhot, 01/07/2026.

#region

using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Documents <see cref="SectionResponseParser" /> behaviour for the <c>[DISCOVERY]</c>
   ///   section, which was previously untested.
   /// </summary>
   [TestFixture]
   public class DiscoveryParsingTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      [Test]
      public void Discovery_with_key_and_description_is_parsed()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[DISCOVERY]\nkey: orientation\ndescription: She seems drawn to men.\n[/DISCOVERY]";

         var result = _parser.Parse(raw);

         result.Discovery.Should().NotBeNull();
         result.Discovery!.Key.Should().Be("orientation");
         result.Discovery.Description.Should().Be("She seems drawn to men.");
         // GameDay is stamped by the consumer at persist time, not by the parser.
         result.Discovery.GameDay.Should().Be(0);
      }

      [Test]
      public void Discovery_missing_key_returns_null()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[DISCOVERY]\ndescription: She seems drawn to men.\n[/DISCOVERY]";

         var result = _parser.Parse(raw);

         result.Discovery.Should().BeNull();
      }

      [Test]
      public void Discovery_missing_description_returns_null()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[DISCOVERY]\nkey: orientation\n[/DISCOVERY]";

         var result = _parser.Parse(raw);

         result.Discovery.Should().BeNull();
      }

      [Test]
      public void No_discovery_section_returns_null()
      {
         var result = _parser.Parse("[DIALOGUE]hi[/DIALOGUE]");
         result.Discovery.Should().BeNull();
      }
   }
}

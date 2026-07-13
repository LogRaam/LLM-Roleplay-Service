// Code written by Gabriel Mailhot, 01/07/2026.
//
// [DISCOVERY] is how the model surfaces a personal trait an NPC just revealed; ProfileMutator.Apply
// appends it to that NPC's DiscoveredTraits (deduplicated by Key) and it later resurfaces on the
// hero's Encyclopedia page. GameDay always comes out of this parser as 0, the consumer stamps the
// real game day at persist time, so DiscoveredTrait.GameDay is deliberately not under test here,
// only the parser's own contract of "0 unless the consumer overrides it".

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

      // Both fields must round-trip intact: Key becomes the dedup identity ProfileMutator matches
      // on, Description is what actually gets shown on the player-facing Encyclopedia page. Also
      // pins the GameDay=0 contract: the parser never stamps a real day, only the consumer does.
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

      // Key is the identity ProfileMutator dedupes on; a revelation with no Key would be an
      // untraceable trait that could be appended over and over instead of being recognized as
      // the same fact, so it must be dropped entirely rather than stored half-identified.
      [Test]
      public void Discovery_missing_key_returns_null()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[DISCOVERY]\ndescription: She seems drawn to men.\n[/DISCOVERY]";

         var result = _parser.Parse(raw);

         result.Discovery.Should().BeNull();
      }

      // A bare Key with no Description would surface as a blank line on the player-facing
      // Encyclopedia page; require both fields or drop the whole discovery, never persist a half
      // trait.
      [Test]
      public void Discovery_missing_description_returns_null()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[DISCOVERY]\nkey: orientation\n[/DISCOVERY]";

         var result = _parser.Parse(raw);

         result.Discovery.Should().BeNull();
      }

      // Baseline negative-space check: no [DISCOVERY] section this turn must never fabricate a
      // revelation that was never made.
      [Test]
      public void No_discovery_section_returns_null()
      {
         var result = _parser.Parse("[DIALOGUE]hi[/DIALOGUE]");
         result.Discovery.Should().BeNull();
      }
   }
}

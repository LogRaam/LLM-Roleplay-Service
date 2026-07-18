// Code written by Gabriel Mailhot, 04/07/2026.
// Locks the ONE canonical banding of ReputationWithPlayer (P0.1, affect-coherence reform): every boundary
// here must match the encyclopedia page's own words exactly, since that page is player-visible canon and
// every other consumer (PromptBuilder.DescribePersonalRegard) now delegates to this same table.

#region

using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class RegardBandsTests
   {
      // This is the regression case P0.1 was built to fix, kept as its own named test so the exact bug
      // (-15 disagreeing between the two old tables) can never silently come back.
      [Test]
      public void GIVEN_a_value_just_inside_the_wary_band_WHEN_describing_THEN_it_reads_wary()
      {
         // The exact case that used to disagree: -15 read "wary" on the encyclopedia page but
         // "neutral" in the prompt (PromptBuilder's old 5-band table bottomed its neutral case at -9).
         RegardBands.Describe(-15).Should().Be("wary");
      }

      // The dead center of the range, where the widest, most-hit band sits: an off-by-one in the pivot's
      // comparison operators would misroute the single most common value an NPC starts at.
      [Test]
      public void GIVEN_zero_WHEN_describing_THEN_it_reads_neutral()
      {
         RegardBands.Describe(0).Should().Be("neutral");
      }

      // Both edges of the "neutral" band pinned by hand, in each direction: -5 must still read neutral
      // while -6 crosses into wary, and +4 must still read neutral while +5 crosses into cordial regard.
      // This is exactly the shape of bug the class exists to prevent (a boundary that reads differently
      // depending on which of the three old vocabularies you asked).
      [TestCase(-5, "neutral")]
      [TestCase(-6, "wary")]
      [TestCase(5, "cordial regard")]
      [TestCase(4, "neutral")]
      public void GIVEN_a_value_at_the_neutral_pivot_edges_WHEN_describing_THEN_the_boundary_lands_correctly(int value, string expected)
      {
         RegardBands.Describe(value).Should().Be(expected);
      }

      // One case per tier across the entire [-100, 100] range, each wording taken verbatim from the
      // encyclopedia page (the pre-existing player-visible canon this table now centralizes). A value
      // landing on the wrong word here means the prompt (PromptBuilder.DescribePersonalRegard) and the
      // encyclopedia page would once again describe the SAME numeric relation differently.
      [TestCase(100, "oathbound devotion")]
      [TestCase(95, "oathbound devotion")]
      [TestCase(94, "unshakable devotion")]
      [TestCase(85, "unshakable devotion")]
      [TestCase(70, "most cherished ally")]
      [TestCase(50, "profound admiration")]
      [TestCase(30, "deep affection")]
      [TestCase(15, "genuine regard")]
      [TestCase(5, "cordial regard")]
      [TestCase(-20, "wary")]
      [TestCase(-35, "quiet distrust")]
      [TestCase(-55, "ill-disposed")]
      [TestCase(-70, "bitter grudge")]
      [TestCase(-85, "open enmity")]
      [TestCase(-95, "mortal foe")]
      [TestCase(-96, "implacable hatred")]
      [TestCase(-100, "implacable hatred")]
      public void GIVEN_every_tier_across_the_full_range_WHEN_describing_THEN_each_band_reads_its_canonical_word(int value, string expected)
      {
         RegardBands.Describe(value).Should().Be(expected);
      }
   }
}

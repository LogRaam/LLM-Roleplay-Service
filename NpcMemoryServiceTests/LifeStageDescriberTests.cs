// Code written by Gabriel Mailhot, 31/07/2026.
// LifeStageDescriber turns a raw hero age into the phrase the identity directive tells the NPC to
// speak as (see NpcMemoryService.Core.Prompts.PromptBuilder.AppendIdentity). Pinning each band and
// both boundaries here is what lets Gabriel confirm, without reading PromptBuilder, that a lord of
// 24 never gets narrated as "an elder of many winters" and a lord of 55 never gets narrated as a
// youth (the player report this feature answers).

#region

using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class LifeStageDescriberTests
   {
      // A negative age can never occur from a real hero, but the caller (PromptBuilder) only tests
      // Age > 0 before rendering the line at all, so Describe itself must stay defensive and never throw.
      [Test]
      public void GIVEN_a_negative_age_WHEN_describing_THEN_returns_empty_string()
      {
         LifeStageDescriber.Describe(-5).Should().BeEmpty();
      }

      // Age 0 is the sentinel for "unknown" (NpcProfile.Age docs: 0 when unknown, e.g. a profile from
      // a save written before this field existed). The empty string is what lets PromptBuilder omit the
      // age line entirely for such a profile instead of announcing a false age of zero.
      [Test]
      public void GIVEN_age_zero_WHEN_describing_THEN_returns_empty_string()
      {
         LifeStageDescriber.Describe(0).Should().BeEmpty();
      }

      // The youth band is the one the player report was about: a 20-25 year old lord was talking as if
      // old and long-lived. 24 must land here, not in the next band.
      [Test]
      public void GIVEN_age_24_WHEN_describing_THEN_flush_of_youth()
      {
         LifeStageDescriber.Describe(24).Should().Be("in the flush of youth");
      }

      // Boundary: 25 is where the directive stops calling the NPC youthful. A lord of exactly 25 is
      // no longer "in the flush of youth" so the LLM cannot lean on the guarding player report to
      // undersell someone who has already crossed into the prime of life.
      [Test]
      public void GIVEN_age_25_WHEN_describing_THEN_prime_of_life()
      {
         LifeStageDescriber.Describe(25).Should().Be("in the prime of life");
      }

      // Interior pin for the prime-of-life band, well clear of either boundary.
      [Test]
      public void GIVEN_age_30_WHEN_describing_THEN_prime_of_life()
      {
         LifeStageDescriber.Describe(30).Should().Be("in the prime of life");
      }

      // Boundary: 39 is still the prime of life, the last year before middle age.
      [Test]
      public void GIVEN_age_39_WHEN_describing_THEN_prime_of_life()
      {
         LifeStageDescriber.Describe(39).Should().Be("in the prime of life");
      }

      // Boundary: 40 tips into middle years. This is the band a mature lord (Derthert-style veteran
      // ruler) should read as, without yet claiming decades of deeds an elder would have.
      [Test]
      public void GIVEN_age_40_WHEN_describing_THEN_middle_years()
      {
         LifeStageDescriber.Describe(40).Should().Be("in your middle years");
      }

      // Boundary: 54 is the last year still described as middle years, one below the elder band.
      [Test]
      public void GIVEN_age_54_WHEN_describing_THEN_middle_years()
      {
         LifeStageDescriber.Describe(54).Should().Be("in your middle years");
      }

      // Boundary: 55 is where the directive lets the NPC speak of a long life. Below this line an NPC
      // must never claim decades of deeds or old age (the exact failure this feature fixes); at and
      // above it, the elder framing is finally earned.
      [Test]
      public void GIVEN_age_55_WHEN_describing_THEN_elder_of_many_winters()
      {
         LifeStageDescriber.Describe(55).Should().Be("an elder of many winters");
      }

      // Interior pin for the elder band, well clear of the boundary.
      [Test]
      public void GIVEN_age_70_WHEN_describing_THEN_elder_of_many_winters()
      {
         LifeStageDescriber.Describe(70).Should().Be("an elder of many winters");
      }
   }
}

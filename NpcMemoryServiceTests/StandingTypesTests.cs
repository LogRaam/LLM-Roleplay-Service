// Code written by Gabriel Mailhot, 21/09/2026.
// What these pin: that the two scales stay two, and that their rungs keep the values the game already had.
//
// Gabriel, 21/09/2026: "il faut que chaque valeur soit un concept du systeme. Age n'est pas un INT, mais un type Age
// qui centralise ses propres regles." This is the first pair of those types, and they exist for a reason that cost
// the project real bugs: NpcProfile.ReputationWithPlayer and Hero.GetRelation are both ints from -100 to 100, both
// called "regard" or "relation" in different places, and a method taking an int cannot tell which one it was handed.
// Lord recruitment gated on the wrong ledger for months and nothing could have caught it.
//
// The numbers here are NOT new. They were gathered from the code on 21/09/2026, as it stood, and the tests below are
// deliberately written as "this rung is still where it was". A failure means a gate MOVED, which changes when a lord
// agrees to leave his house or when a secret reaches one more person: a tuning decision, never a side effect of a
// refactor. docs/maps/decisions-regard.md is the wider witness.

#region

using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Standing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class StandingTypesTests
   {
      // THE FOUR THAT WERE ONE. Rescue gratitude, secret propagation, a spouse's three closest friends and a rival
      // suitor's associates each minted "close enough to X to be told or to owe" at 20, under four names.
      [Test]
      public void GIVEN_the_trusted_rung_WHEN_read_THEN_it_is_still_twenty()
      {
         VanillaRelation.TrustedTier.Should().Be(20);

         new VanillaRelation(19).IsTrusted.Should().BeFalse();
         new VanillaRelation(20).IsTrusted.Should().BeTrue();
      }

      // The prompt calls a third party "a friend" from 10 and "a very close friend" from 30. Those are the words a
      // character reads, and they must keep saying what they said.
      [Test]
      public void GIVEN_the_spoken_rungs_WHEN_read_THEN_friend_is_still_ten_and_close_is_still_thirty()
      {
         VanillaRelation.FriendTier.Should().Be(10);
         VanillaRelation.CloseTier.Should().Be(30);

         new VanillaRelation(10).IsFriend.Should().BeTrue();
         new VanillaRelation(29).IsClose.Should().BeFalse();
         new VanillaRelation(30).IsClose.Should().BeTrue();
      }

      // A FRIEND IS NOT YET TRUSTED, and that gap is the finding this type makes visible rather than hides: a hero at
      // 12 is voiced as a friend, feels no debt for a rescue, and hears no secret.
      [Test]
      public void GIVEN_a_hero_between_the_rungs_WHEN_asked_THEN_they_are_a_friend_but_not_yet_trusted()
      {
         var hero = new VanillaRelation(12);

         hero.IsFriend.Should().BeTrue();
         hero.IsTrusted.Should().BeFalse();
      }

      [Test]
      public void GIVEN_the_cold_rungs_WHEN_read_THEN_enemy_and_bitter_are_where_they_were()
      {
         new VanillaRelation(-10).IsEnemy.Should().BeTrue();
         new VanillaRelation(-9).IsEnemy.Should().BeFalse();
         new VanillaRelation(-30).IsBitterEnemy.Should().BeTrue();
         new VanillaRelation(0).IsIndifferent.Should().BeTrue();
      }

      #region the mod's own ledger

      // THE MARK THREE RECRUITMENT GATES SHARE. Lord, notable and prisoner recruitment all ask "is the bond deep
      // enough to uproot a life", and the notable policy's doc had claimed for months that all three matched while
      // the lord one read the OTHER ledger entirely (fixed 21/09/2026).
      [Test]
      public void GIVEN_the_deep_bond_rung_WHEN_read_THEN_it_is_still_sixty()
      {
         Regard.DeepBondTier.Should().Be(60);

         new Regard(59).IsDeepBond.Should().BeFalse();
         new Regard(60).IsDeepBond.Should().BeTrue();
      }

      [Test]
      public void GIVEN_the_warm_rungs_WHEN_read_THEN_they_are_where_the_code_had_them()
      {
         Regard.CordialTier.Should().Be(5);
         Regard.WarmTier.Should().Be(20);
         Regard.ConfidingTier.Should().Be(30);
         Regard.DevotedTier.Should().Be(50);
      }

      // ONE VOCABULARY FOR THE SCALE: the words a character reads come from RegardBands, so the type cannot grow a
      // second set of adjectives that drifts from the first.
      [Test]
      public void GIVEN_a_regard_WHEN_described_THEN_it_speaks_with_the_bands_own_words()
      {
         new Regard(35).Described.Should().Be(RegardBands.Describe(35));
         new Regard(-40).Described.Should().Be(RegardBands.Describe(-40));
      }

      // WHERE THE TWO SCALES ALIGN, AND WHERE THEY DO NOT. Confiding and Devoted sit exactly on a RegardBands word
      // boundary; Warm and DeepBond do not (the bands turn at 15 and 70). That is recorded, not smoothed away: moving
      // a gate to make a table tidy would change when a lord leaves his house, and that is Gabriel's call, not a
      // refactor's. If somebody ever decides to align them, this test is where the decision gets written down.
      [Test]
      public void GIVEN_the_mod_rungs_WHEN_compared_with_the_spoken_bands_THEN_two_align_and_two_knowingly_do_not()
      {
         RegardBands.Describe(Regard.ConfidingTier).Should().Be("deep affection");
         RegardBands.Describe(Regard.DevotedTier).Should().Be("profound admiration");

         RegardBands.Describe(Regard.WarmTier).Should().Be("genuine regard", "the band turns at 15, the gate at 20");
         RegardBands.Describe(Regard.DeepBondTier).Should().Be("profound admiration", "the band turns at 70, the gate at 60");
      }

      // THE WHOLE POINT OF TWO TYPES: the compiler now refuses what an int allowed. This cannot be written as a
      // failing call (it would not compile, which IS the guarantee), so it is stated here as the contract in prose.
      [Test]
      public void GIVEN_the_two_ledgers_WHEN_held_as_types_THEN_neither_can_be_passed_where_the_other_is_meant()
      {
         typeof(Regard).Should().NotBe(typeof(VanillaRelation));
         new Regard(60).Points.Should().Be(new VanillaRelation(60).Points, "the same number, and still not the same fact");
      }

      #endregion
   }
}

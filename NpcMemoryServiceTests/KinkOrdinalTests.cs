// Code written by Gabriel Mailhot, 01/07/2026.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   <see cref="Kink" /> is persisted as a raw integer per NPC profile in save-game data
   ///   (never by name). Reordering, inserting, or removing a value shifts every later
   ///   ordinal and silently corrupts existing saves (an NPC's stored "Sadism" could read
   ///   back as "Masochism"). This test freezes the name-to-ordinal map so any such change
   ///   fails the build instead of a player's save.
   /// </summary>
   [TestFixture]
   public class KinkOrdinalTests
   {
      [TestCase(Kink.None, 0)]
      [TestCase(Kink.Dominance, 1)]
      [TestCase(Kink.Submission, 2)]
      [TestCase(Kink.SwitchTendencies, 3)]
      [TestCase(Kink.Sadism, 4)]
      [TestCase(Kink.Masochism, 5)]
      [TestCase(Kink.BondageGiving, 6)]
      [TestCase(Kink.BondageReceiving, 7)]
      [TestCase(Kink.Roleplay, 8)]
      [TestCase(Kink.PowerImbalance, 9)]
      [TestCase(Kink.Exhibitionism, 10)]
      [TestCase(Kink.Voyeurism, 11)]
      [TestCase(Kink.Possessiveness, 12)]
      [TestCase(Kink.PublicAffection, 13)]
      [TestCase(Kink.OrgasmControl, 14)]
      [TestCase(Kink.Chastity, 15)]
      [TestCase(Kink.FreeUse, 16)]
      [TestCase(Kink.Degradation, 17)]
      [TestCase(Kink.Objectification, 18)]
      [TestCase(Kink.PetPlay, 19)]
      [TestCase(Kink.Praise, 20)]
      [TestCase(Kink.ImpactPlay, 21)]
      [TestCase(Kink.SensoryDeprivation, 22)]
      [TestCase(Kink.FearPlay, 23)]
      [TestCase(Kink.MasterSlave, 24)]
      [TestCase(Kink.Breeding, 25)]
      [TestCase(Kink.Training, 26)]
      [TestCase(Kink.CorruptionKink, 27)]
      [TestCase(Kink.Prize, 28)]
      public void GIVEN_a_persisted_Kink_WHEN_cast_to_int_THEN_ordinal_matches_the_frozen_map(
         Kink value, int expectedOrdinal)
      {
         ((int) value).Should().Be(expectedOrdinal,
            "reordering this enum reinterprets every previously-saved kink of a later value");
      }

      [Test]
      public void GIVEN_the_Kink_enum_WHEN_counting_members_THEN_no_value_was_added_without_updating_this_test()
      {
         // If this fails, a new value was appended (or removed) — add (or remove) the
         // matching [TestCase] above with its ordinal, appended LAST, to keep this frozen.
         System.Enum.GetValues(typeof(Kink)).Length.Should().Be(29);
      }
   }
}

// Code written by Gabriel Mailhot, 01/07/2026.
// Kink (NpcMemoryService.Core.Models) is stored per NPC profile as a raw int inside the player's save.
// The enum has no explicit numeric values, so the C# compiler assigns them by DECLARATION ORDER: inserting,
// removing, or reordering a member reassigns every later ordinal, and a save written before the change
// deserializes into the WRONG kink after it (an NPC's persisted Sadism silently becomes Masochism). This
// test pins the whole name-to-ordinal map so that kind of edit fails the build, not a player's save file.
// The save-migration rule for this project is that new members are only ever APPENDED AT THE END.

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
      // One TestCase per member, each pinning its ordinal by hand: this is the actual save contract, not a
      // loop over Enum.GetValues (which would just restate whatever order the enum happens to be in today
      // and catch nothing). A future edit that moves, say, Sadism from 4 to 5 fails exactly here.
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

      // The companion guard: the count alone catches a member appended at the end without ANY corresponding
      // [TestCase] above (which the per-value assertions can't see, since they only check what's already
      // listed). A failure here means "go add the missing [TestCase], last, with the next free ordinal".
      [Test]
      public void GIVEN_the_Kink_enum_WHEN_counting_members_THEN_no_value_was_added_without_updating_this_test()
      {
         // If this fails, a new value was appended (or removed) — add (or remove) the
         // matching [TestCase] above with its ordinal, appended LAST, to keep this frozen.
         System.Enum.GetValues(typeof(Kink)).Length.Should().Be(29);
      }
   }
}

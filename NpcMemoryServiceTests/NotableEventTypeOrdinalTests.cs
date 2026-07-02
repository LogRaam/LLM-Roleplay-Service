// Code written by Gabriel Mailhot, 01/07/2026.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   <see cref="NotableEventType" /> is persisted as a raw integer in save-game NPC
   ///   profiles (never by name). Reordering, inserting, or removing a value shifts every
   ///   later ordinal and silently corrupts existing saves (an old "Betrayal" event could
   ///   read back as "Confrontation"). This test freezes the name-to-ordinal map so any
   ///   such change fails the build instead of a player's save.
   /// </summary>
   [TestFixture]
   public class NotableEventTypeOrdinalTests
   {
      [TestCase(NotableEventType.FirstMeeting, 0)]
      [TestCase(NotableEventType.Farewell, 1)]
      [TestCase(NotableEventType.Conflict, 2)]
      [TestCase(NotableEventType.Collaboration, 3)]
      [TestCase(NotableEventType.Agreement, 4)]
      [TestCase(NotableEventType.Flirt, 5)]
      [TestCase(NotableEventType.Intimacy, 6)]
      [TestCase(NotableEventType.Betrayal, 7)]
      [TestCase(NotableEventType.Confrontation, 8)]
      [TestCase(NotableEventType.Other, 9)]
      [TestCase(NotableEventType.Captivity, 10)]
      [TestCase(NotableEventType.Jealousy, 11)]
      public void GIVEN_a_persisted_NotableEventType_WHEN_cast_to_int_THEN_ordinal_matches_the_frozen_map(
         NotableEventType value, int expectedOrdinal)
      {
         ((int) value).Should().Be(expectedOrdinal,
            "reordering this enum reinterprets every previously-saved event of a later value");
      }

      [Test]
      public void GIVEN_the_NotableEventType_enum_WHEN_counting_members_THEN_no_value_was_added_without_updating_this_test()
      {
         // If this fails, a new value was appended (or removed) — add (or remove) the
         // matching [TestCase] above with its ordinal, appended LAST, to keep this frozen.
         System.Enum.GetValues(typeof(NotableEventType)).Length.Should().Be(12);
      }
   }
}

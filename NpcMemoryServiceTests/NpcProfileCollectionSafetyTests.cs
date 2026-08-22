// Code written by Gabriel Mailhot, 18/07/2026.
//
// A profile's collections must NEVER hand back null, whatever the stored save contains. Guards a
// NullReferenceException Gabriel hit while saving (2026-07-18): CourierService.RespawnInFlight walks
// profile.ReceivedPlayerLetters and profile.SentLetters directly, and its sibling TickCouriers guards the
// very same lists against null, so the codebase had already met this and defended only one of the two.
//
// The trap is specific to init-only properties: "{ get; init; } = new()" runs its initializer BEFORE
// deserialization, and an explicit null in the JSON then overwrites it through the init setter. The
// profile that comes back therefore carries a null where every caller expects a list, and the mod
// dereferences these in dozens of places. Absorbing it in the model is the one place it can be prevented
// for all of them at once.

#region

using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class NpcProfileCollectionSafetyTests
   {
      // The exact shape that produced the save-time crash: a stored profile whose collections are literal
      // nulls. Deserialization must absorb every one of them, because the caller that walks the list has no
      // reason to suspect a save file can disagree with the type.
      [Test]
      public void GIVEN_a_stored_profile_whose_collections_are_null_WHEN_loading_THEN_none_of_them_come_back_null()
      {
         const string json = """
                             {
                               "Id": "lord_mina",
                               "Name": "Mina",
                               "Clan": "Thais",
                               "Faction": "Battania",
                               "ActiveQuests": null,
                               "CourtActionCooldowns": null,
                               "DiscoveredTraits": null,
                               "Events": null,
                               "ReceivedPlayerLetters": null,
                               "SentLetters": null,
                               "ScheduledLetters": null,
                               "TroopLoans": null
                             }
                             """;

         NpcProfile profile = JsonSerializer.Deserialize<NpcProfile>(json)!;

         profile.Should().NotBeNull();
         profile!.ActiveQuests.Should().NotBeNull();
         profile.CourtActionCooldowns.Should().NotBeNull();
         profile.DiscoveredTraits.Should().NotBeNull();
         profile.Events.Should().NotBeNull();
         profile.ReceivedPlayerLetters.Should().NotBeNull();
         profile.SentLetters.Should().NotBeNull();
         profile.ScheduledLetters.Should().NotBeNull();
         profile.TroopLoans.Should().NotBeNull();
      }

      // The save-migration rule in its usual form: an OLDER save simply has no TroopLoans key at all, and
      // must load as a lord with nothing out on loan rather than needing a migration pass.
      [Test]
      public void GIVEN_a_save_written_before_troop_loans_existed_WHEN_loading_THEN_the_lord_has_none()
      {
         NpcProfile profile = JsonSerializer.Deserialize<NpcProfile>(
            """{"Id":"lord_mina","Name":"Mina","Clan":"Thais","Faction":"Battania"}""")!;

         profile!.TroopLoans.Should().NotBeNull().And.BeEmpty();
      }

      // The same guarantee stated where it actually bites: the caller iterates. An empty list is what makes
      // "foreach over the letters" a no-op instead of a crash mid-save.
      [Test]
      public void GIVEN_a_null_letter_list_in_the_save_WHEN_iterating_it_THEN_it_is_simply_empty()
      {
         NpcProfile profile = JsonSerializer.Deserialize<NpcProfile>(
            """{"Id":"a","Name":"A","Clan":"C","Faction":"F","SentLetters":null,"ReceivedPlayerLetters":null}""")!;

         profile!.SentLetters.Should().BeEmpty();
         profile.ReceivedPlayerLetters.Should().BeEmpty();
      }

      // The ordinary path must be untouched: absorbing null is not an excuse to drop real content.
      [Test]
      public void GIVEN_a_profile_carrying_real_entries_WHEN_round_tripping_THEN_they_all_survive()
      {
         var original = new NpcProfile {
            Id = "lord_mina",
            Name = "Mina",
            Clan = "Thais",
            Faction = "Battania",
            Events = new List<NotableEvent> {
               new(12, NotableEventType.Agreement, "Accepted the hideout task.")
            }
         };

         NpcProfile loaded = JsonSerializer.Deserialize<NpcProfile>(JsonSerializer.Serialize(original))!;

         loaded!.Events.Should().HaveCount(1);
         loaded.Events[0].summary.Should().Be("Accepted the hideout task.");
      }

      // A profile built in code, never persisted, is the other half of the contract: the collections are
      // usable immediately, so no caller has to construct them defensively.
      [Test]
      public void GIVEN_a_freshly_constructed_profile_WHEN_reading_its_collections_THEN_they_are_empty_not_null()
      {
         var profile = new NpcProfile {Id = "a", Name = "A", Clan = "C", Faction = "F"};

         profile.Events.Should().NotBeNull().And.BeEmpty();
         profile.SentLetters.Should().NotBeNull().And.BeEmpty();
         profile.CourtActionCooldowns.Should().NotBeNull().And.BeEmpty();
      }
   }
}

// Code written by Gabriel Mailhot, 01/07/2026.

#region

using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Documents <see cref="ProfileMutator" /> as the single authoritative path for
   ///   mutating an <see cref="NpcProfile" /> from a parsed LLM turn: the [-100, 100]
   ///   reputation clamp, the duplicate-FirstMeeting guard, the empty-summary guard, and
   ///   the romantic-arc state machine (<c>AdvanceRomanticStatus</c>).
   /// </summary>
   [TestFixture]
   public class ProfileMutatorTests
   {
      private static NpcProfile CreateProfile(int reputation = 0, RomanticProfile? romantic = null,
         string? spouseName = null)
         => new() {
            Id = "npc_1",
            Name = "Vesha",
            Clan = "clan_test",
            Faction = "faction_test",
            ReputationWithPlayer = reputation,
            Romantic = romantic,
            SpouseName = spouseName
         };

      private static ParsedResponse ResponseWithEvent(NotableEventType type, string summary, int? clanDelta = null)
         => new() {
            Dialogue = "",
            NewEventData = new ParsedEventData(type, summary),
            Reputation = clanDelta.HasValue ? new ReputationDelta(clanDelta) : null
         };

      // ---------- Reputation clamp ----------

      [Test]
      public void GIVEN_reputation_near_the_ceiling_WHEN_a_large_positive_delta_applies_THEN_it_clamps_to_100()
      {
         NpcProfile profile = CreateProfile(reputation: 90);
         var response = new ParsedResponse {Dialogue = "", Reputation = new ReputationDelta(50)};

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.ReputationWithPlayer.Should().Be(100);
      }

      [Test]
      public void GIVEN_reputation_near_the_floor_WHEN_a_large_negative_delta_applies_THEN_it_clamps_to_minus_100()
      {
         NpcProfile profile = CreateProfile(reputation: -90);
         var response = new ParsedResponse {Dialogue = "", Reputation = new ReputationDelta(-50)};

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.ReputationWithPlayer.Should().Be(-100);
      }

      [Test]
      public void GIVEN_a_delta_that_stays_in_range_WHEN_applied_THEN_reputation_is_unclamped()
      {
         NpcProfile profile = CreateProfile(reputation: 10);
         var response = new ParsedResponse {Dialogue = "", Reputation = new ReputationDelta(5)};

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.ReputationWithPlayer.Should().Be(15);
      }

      // ---------- Duplicate FirstMeeting guard ----------

      [Test]
      public void GIVEN_a_FirstMeeting_event_already_recorded_WHEN_another_FirstMeeting_arrives_THEN_it_is_not_duplicated()
      {
         NpcProfile profile = CreateProfile();
         profile.Events.Add(new NotableEvent(1, NotableEventType.FirstMeeting, "First hello."));
         ParsedResponse response = ResponseWithEvent(NotableEventType.FirstMeeting, "Second hello, ignored.");

         ProfileMutator.Apply(profile, response, gameDay: 5);

         profile.Events.Should().HaveCount(1);
         profile.Events[0].summary.Should().Be("First hello.");
      }

      [Test]
      public void GIVEN_no_prior_FirstMeeting_WHEN_one_arrives_THEN_it_is_recorded()
      {
         NpcProfile profile = CreateProfile();
         ParsedResponse response = ResponseWithEvent(NotableEventType.FirstMeeting, "They meet at last.");

         ProfileMutator.Apply(profile, response, gameDay: 3);

         profile.Events.Should().ContainSingle(e => e.type == NotableEventType.FirstMeeting);
      }

      // ---------- Empty-summary guard ----------

      [Test]
      public void GIVEN_an_event_with_whitespace_only_summary_WHEN_applied_THEN_no_event_is_recorded()
      {
         NpcProfile profile = CreateProfile();
         ParsedResponse response = ResponseWithEvent(NotableEventType.Conflict, "   ");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Events.Should().BeEmpty();
      }

      // ---------- Romantic arc: Standard progression ----------

      [Test]
      public void GIVEN_standard_preferences_and_None_status_WHEN_a_Flirt_event_arrives_THEN_status_becomes_Curious()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.None};
         NpcProfile profile = CreateProfile(romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Flirt, "A shared glance.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Curious);
      }

      [Test]
      public void GIVEN_Curious_status_and_high_relation_WHEN_another_Flirt_arrives_THEN_status_advances_to_Courting()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Curious};
         NpcProfile profile = CreateProfile(reputation: 10, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Flirt, "Lingering conversation.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Courting);
      }

      [Test]
      public void GIVEN_Courting_status_and_high_relation_WHEN_Intimacy_arrives_THEN_status_advances_to_Intimate()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Courting};
         NpcProfile profile = CreateProfile(reputation: 20, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Intimacy, "They spend the night.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Intimate);
      }

      // ---------- Romantic arc: Casual preference ----------

      [Test]
      public void GIVEN_a_Casual_preference_WHEN_Intimacy_arrives_at_relation_5_THEN_status_jumps_straight_to_Intimate()
      {
         var romantic = new RomanticProfile {
            Status = RomanticStatus.None,
            Preferences = {RomanticPreference.Casual}
         };
         NpcProfile profile = CreateProfile(reputation: 5, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Intimacy, "A brief, unattached night.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Intimate);
      }

      // ---------- Romantic arc: Intense preference ----------

      [Test]
      public void GIVEN_an_Intense_preference_WHEN_Flirt_arrives_from_None_THEN_status_skips_Curious_straight_to_Courting()
      {
         var romantic = new RomanticProfile {
            Status = RomanticStatus.None,
            Preferences = {RomanticPreference.Intense}
         };
         NpcProfile profile = CreateProfile(romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Flirt, "An intense first spark.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Courting);
      }

      [Test]
      public void GIVEN_an_Intense_preference_and_Courting_status_WHEN_Intimacy_arrives_at_relation_10_THEN_status_advances_to_Intimate()
      {
         var romantic = new RomanticProfile {
            Status = RomanticStatus.Courting,
            Preferences = {RomanticPreference.Intense}
         };
         NpcProfile profile = CreateProfile(reputation: 10, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Intimacy, "All-consuming.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Intimate);
      }

      // ---------- Romantic arc: Married ----------

      [Test]
      public void GIVEN_the_NPC_is_married_WHEN_Intimacy_arrives_THEN_status_becomes_SecretLover()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Courting};
         NpcProfile profile = CreateProfile(romantic: romantic, spouseName: "Harek");
         ParsedResponse response = ResponseWithEvent(NotableEventType.Intimacy, "A secret meeting.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.SecretLover);
      }

      // ---------- Romantic arc: negative events degrade the arc ----------

      [Test]
      public void GIVEN_Courting_status_WHEN_a_Betrayal_event_arrives_THEN_status_becomes_Broken()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Courting};
         NpcProfile profile = CreateProfile(romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Betrayal, "She discovers the lie.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Broken);
      }

      [Test]
      public void GIVEN_Intimate_status_and_deeply_negative_relation_WHEN_Conflict_arrives_THEN_status_becomes_Broken()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Intimate};
         NpcProfile profile = CreateProfile(reputation: -35, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Conflict, "A bitter fight.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Broken);
      }

      [Test]
      public void GIVEN_Intimate_status_and_moderately_negative_relation_WHEN_Conflict_arrives_THEN_status_becomes_Estranged()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Intimate};
         NpcProfile profile = CreateProfile(reputation: -5, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Conflict, "A sharp disagreement.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Estranged);
      }

      [Test]
      public void GIVEN_Estranged_status_WHEN_another_Conflict_arrives_THEN_status_becomes_Broken()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Estranged};
         NpcProfile profile = CreateProfile(romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Conflict, "The final straw.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Broken);
      }

      [Test]
      public void GIVEN_no_Romantic_profile_WHEN_an_Intimacy_event_arrives_THEN_nothing_throws_and_events_still_record()
      {
         NpcProfile profile = CreateProfile(); // Romantic left null
         ParsedResponse response = ResponseWithEvent(NotableEventType.Intimacy, "A quiet moment.");

         var act = () => ProfileMutator.Apply(profile, response, gameDay: 1);

         act.Should().NotThrow();
         profile.Events.Should().ContainSingle(e => e.type == NotableEventType.Intimacy);
      }

      // ---------- Discovery dedup (bonus coverage of the same Apply path) ----------

      [Test]
      public void GIVEN_a_trait_key_already_discovered_WHEN_the_same_key_arrives_again_THEN_it_is_not_duplicated()
      {
         NpcProfile profile = CreateProfile();
         profile.DiscoveredTraits.Add(new DiscoveredTrait {Key = "orientation", Description = "Original.", GameDay = 1});
         var response = new ParsedResponse {
            Dialogue = "",
            Discovery = new DiscoveredTrait {Key = "orientation", Description = "Restated, ignored.", GameDay = 0}
         };

         ProfileMutator.Apply(profile, response, gameDay: 9);

         profile.DiscoveredTraits.Should().ContainSingle();
         profile.DiscoveredTraits.Single().Description.Should().Be("Original.");
      }
   }
}

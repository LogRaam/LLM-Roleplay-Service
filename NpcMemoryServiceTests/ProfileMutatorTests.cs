// Code written by Gabriel Mailhot, 01/07/2026.
// ProfileMutator.Apply is the ONE authoritative path from a parsed LLM turn (or a mod-side system emitting
// its own NotableEvent, like captivity or jealousy) into a persisted NpcProfile: the mod, the ConsoleRunner,
// and this test project all share it. A bug here does not stay local, it reaches every caller at once: a
// broken clamp lets ReputationWithPlayer escape [-100, 100] and desync every band/threshold that reads it,
// a broken FirstMeeting guard duplicates a save's history, and a wrong romantic-arc transition silently
// jumps (or stalls) an NPC's relationship status a player is actively courting.

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

      // ReputationWithPlayer is the number every band table (RegardBands), threshold gate (DivorcePolicy,
      // this class's own romantic-arc thresholds) and consumer downstream reads. Left unclamped, one big
      // LLM-emitted delta on an already-near-ceiling NPC could push it past 100, which nothing downstream
      // was ever written to expect.
      [Test]
      public void GIVEN_reputation_near_the_ceiling_WHEN_a_large_positive_delta_applies_THEN_it_clamps_to_100()
      {
         NpcProfile profile = CreateProfile(reputation: 90);
         var response = new ParsedResponse {Dialogue = "", Reputation = new ReputationDelta(50)};

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.ReputationWithPlayer.Should().Be(100);
      }

      // The floor's mirror of the ceiling case above: a large negative delta must not carry an already
      // near-floor NPC below -100, the documented lower bound of the range every threshold assumes.
      [Test]
      public void GIVEN_reputation_near_the_floor_WHEN_a_large_negative_delta_applies_THEN_it_clamps_to_minus_100()
      {
         NpcProfile profile = CreateProfile(reputation: -90);
         var response = new ParsedResponse {Dialogue = "", Reputation = new ReputationDelta(-50)};

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.ReputationWithPlayer.Should().Be(-100);
      }

      // Guards the clamp from over-firing: a delta that never leaves [-100, 100] must apply exactly, not
      // get snapped to a bound by a clamp implemented with the wrong comparison.
      [Test]
      public void GIVEN_a_delta_that_stays_in_range_WHEN_applied_THEN_reputation_is_unclamped()
      {
         NpcProfile profile = CreateProfile(reputation: 10);
         var response = new ParsedResponse {Dialogue = "", Reputation = new ReputationDelta(5)};

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.ReputationWithPlayer.Should().Be(15);
      }

      // ---------- Duplicate FirstMeeting guard ----------

      // An LLM can emit [EVENT FirstMeeting] more than once across a long relationship (it has no memory of
      // its own past emissions beyond the prompt). Without this guard, an NPC's history would accumulate a
      // "First Meeting" line every few conversations, diluting the one event meant to be unique per NPC.
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

      // The guard's counterpart: it must not be so eager it blocks the FIRST legitimate FirstMeeting too.
      [Test]
      public void GIVEN_no_prior_FirstMeeting_WHEN_one_arrives_THEN_it_is_recorded()
      {
         NpcProfile profile = CreateProfile();
         ParsedResponse response = ResponseWithEvent(NotableEventType.FirstMeeting, "They meet at last.");

         ProfileMutator.Apply(profile, response, gameDay: 3);

         profile.Events.Should().ContainSingle(e => e.type == NotableEventType.FirstMeeting);
      }

      // ---------- Empty-summary guard ----------

      // LLMs occasionally emit a syntactically valid but semantically empty [EVENT] block. Recording it
      // anyway would pollute an NPC's history with blank lines like "Day N (Other):" that carry no
      // information for any future prompt that reads that history back.
      [Test]
      public void GIVEN_an_event_with_whitespace_only_summary_WHEN_applied_THEN_no_event_is_recorded()
      {
         NpcProfile profile = CreateProfile();
         ParsedResponse response = ResponseWithEvent(NotableEventType.Conflict, "   ");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Events.Should().BeEmpty();
      }

      // ---------- Romantic arc: Standard progression ----------

      // RomanticStatus is what the prompt's consent section (PromptBuilder) reads to decide how an NPC may
      // react to advances, so the arc must not race ahead of trust: a bare Flirt from a stranger opens
      // Curious, not Courting.
      [Test]
      public void GIVEN_standard_preferences_and_None_status_WHEN_a_Flirt_event_arrives_THEN_status_becomes_Curious()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.None};
         NpcProfile profile = CreateProfile(romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Flirt, "A shared glance.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Curious);
      }

      // Pins the relation-≥10 gate documented on AdvanceRomanticStatus: Courting is reached by trust, not
      // merely by a second Flirt landing regardless of how the NPC actually feels about the player.
      [Test]
      public void GIVEN_Curious_status_and_high_relation_WHEN_another_Flirt_arrives_THEN_status_advances_to_Courting()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Curious};
         NpcProfile profile = CreateProfile(reputation: 10, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Flirt, "Lingering conversation.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Courting);
      }

      // The final rung of the standard arc (relation ≥ 20): reaching Intimate is what the prompt reads to
      // finally allow physical intimacy language for a Standard-preference NPC.
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

      // A Casual NPC (RomanticPreference.Casual) is written to skip the courtship ladder entirely: at
      // relation ≥ 5 the SAME Intimacy event that only reaches Curious/Courting for a Standard NPC (see
      // above) jumps straight to Intimate. Getting this wrong makes every NPC behave identically regardless
      // of their authored preference.
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

      // An Intense NPC skips Curious outright: their first Flirt from None goes straight to Courting. Miss
      // this and an Intense-preference NPC reads identically to a Standard one, defeating the point of the
      // preference existing at all.
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

      // Pins the Intense-specific relation-≥10 gate (half the Standard arc's ≥20): a lower bar for a
      // character explicitly authored to move fast.
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

      // A married NPC must never land on plain "Intimate" (that status implies an unattached romance): the
      // mod's jealousy system (JealousyBehavior, EncounterContextBuilder) reads RomanticStatus.SecretLover
      // specifically to know a discreet affair exists and route the jealousy-plausibility checks
      // accordingly. Landing on the wrong status here would make an affair invisible to that system.
      [Test]
      public void GIVEN_the_NPC_is_married_WHEN_Intimacy_arrives_THEN_status_becomes_SecretLover()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Courting};
         NpcProfile profile = CreateProfile(romantic: romantic, spouseName: "Harek");
         ParsedResponse response = ResponseWithEvent(NotableEventType.Intimacy, "A secret meeting.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.SecretLover);
      }

      // Romance audit C3: a Committed bond ALWAYS means committed to the PLAYER (legal marriage or consort),
      // never a third party, so intimacy with the player is open and legitimate. Marriage now sets Committed, and
      // this must survive the "married NPC -> SecretLover" rule: without the guard a real player-spouse flipped to
      // SecretLover ("married to another, clandestine") on the first post-wedding intimacy and froze there for
      // life, poisoning the prompt and making jealousy read the marriage itself as an affair.
      [Test]
      public void GIVEN_a_Committed_player_spouse_WHEN_Intimacy_arrives_THEN_status_stays_Committed()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Committed};
         NpcProfile profile = CreateProfile(romantic: romantic, spouseName: "Player");
         ParsedResponse response = ResponseWithEvent(NotableEventType.Intimacy, "A night together, as married folk.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Committed);
      }

      // Romance audit C3 (save migration): the host's one-shot backfill heals a player's spouse whose status
      // predates the marriage-sets-Committed fix. A pre-marriage leftover or the SecretLover corruption must be
      // healed; a deliberately damaged bond (Estranged/Broken) or an already-correct Committed must be left as
      // the marriage earned it, so the heal never erases a real emotional state or thrashes a good one.
      [Test]
      public void GIVEN_a_spouse_status_WHEN_asking_if_it_needs_healing_THEN_only_the_corrupt_or_leftover_states_do()
      {
         ProfileMutator.SpouseStatusNeedsHeal(RomanticStatus.SecretLover).Should().BeTrue();
         ProfileMutator.SpouseStatusNeedsHeal(RomanticStatus.Intimate).Should().BeTrue();
         ProfileMutator.SpouseStatusNeedsHeal(RomanticStatus.None).Should().BeTrue();

         ProfileMutator.SpouseStatusNeedsHeal(RomanticStatus.Committed).Should().BeFalse();
         ProfileMutator.SpouseStatusNeedsHeal(RomanticStatus.Estranged).Should().BeFalse();
         ProfileMutator.SpouseStatusNeedsHeal(RomanticStatus.Broken).Should().BeFalse();
      }

      // ---------- Romantic arc: negative events degrade the arc ----------

      // A Betrayal must be able to break ANY arc already past mere Curiosity (Courting, Intimate,
      // SecretLover), immediately, in one step. Without this, a betrayed relationship would keep reading as
      // an active romance to every prompt section that checks RomanticStatus.
      [Test]
      public void GIVEN_Courting_status_WHEN_a_Betrayal_event_arrives_THEN_status_becomes_Broken()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Courting};
         NpcProfile profile = CreateProfile(romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Betrayal, "She discovers the lie.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Broken);
      }

      // A Conflict does NOT always break the arc: the outcome depends on how deep the relation has soured
      // (the ≤ -30 gate). Deeply negative relation at Intimate/SecretLover means the bond is beyond repair,
      // so it must resolve straight to Broken rather than the milder Estranged.
      [Test]
      public void GIVEN_Intimate_status_and_deeply_negative_relation_WHEN_Conflict_arrives_THEN_status_becomes_Broken()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Intimate};
         NpcProfile profile = CreateProfile(reputation: -35, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Conflict, "A bitter fight.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Broken);
      }

      // The other side of the same gate: a Conflict that has NOT yet dropped relation past -30 is a
      // recoverable rift (Estranged), not a broken one. Paired with the test above, this fixes the exact
      // line a future retune of that threshold could otherwise silently move.
      [Test]
      public void GIVEN_Intimate_status_and_moderately_negative_relation_WHEN_Conflict_arrives_THEN_status_becomes_Estranged()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Intimate};
         NpcProfile profile = CreateProfile(reputation: -5, romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Conflict, "A sharp disagreement.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Estranged);
      }

      // Estranged is not a resting state: a SECOND Conflict on top of it means the recoverable rift was not
      // recovered, and the arc must finish degrading to Broken rather than staying stuck at Estranged.
      [Test]
      public void GIVEN_Estranged_status_WHEN_another_Conflict_arrives_THEN_status_becomes_Broken()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Estranged};
         NpcProfile profile = CreateProfile(romantic: romantic);
         ParsedResponse response = ResponseWithEvent(NotableEventType.Conflict, "The final straw.");

         ProfileMutator.Apply(profile, response, gameDay: 1);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Broken);
      }

      // Not every NPC has a Romantic profile attached. AdvanceRomanticStatus must no-op cleanly (an early
      // null-check) rather than throw, since a crash here would take down the whole Apply pipeline,
      // including the reputation and event-history updates that have nothing to do with romance.
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

      // DiscoveredTraits back the encyclopedia page's discovery section (EncyclopediaHeroPageDiscoveryMixin):
      // an LLM restating an already-known fact (the same Key, e.g. "orientation") in a later conversation
      // must not overwrite or duplicate the ORIGINAL discovery, or the player's dossier would churn every
      // time the model happens to re-mention something it already revealed.
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

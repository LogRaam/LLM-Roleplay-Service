// Code written by Gabriel Mailhot, 21/07/2026.
// Player report (Nexus): the Encyclopedia's discovery section, meant to hold ONLY what the player learned
// ABOUT the NPC, was filling up with entries about the PLAYER instead. DiscoverySubjectGuard is the safety
// net ProfileMutator consults before storing a [DISCOVERY] (see ProfileMutatorTests for the end-to-end
// drop). These tests pin the three signals it catches (player-namespaced key, "the player" as the literal
// subject, the player's own name opening the sentence), the false-positive bar (a discovery that merely
// MENTIONS the player mid-sentence, or names a third party, must still pass), and that blank/null input
// never throws.

#region

using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class DiscoverySubjectGuardTests
   {
      // ── Clear signals worth catching ────────────────────────────────────

      // The reported leak: a key namespaced to the player (the model's own attempt to label what it wrote)
      // names the PLAYER as the discovery's subject regardless of what the description says.
      [TestCase("player_orientation")]
      [TestCase("player")]
      [TestCase("preference_player_dominant")]
      public void GIVEN_a_key_namespaced_to_player_WHEN_checked_THEN_it_is_flagged(string key)
         => DiscoverySubjectGuard.IsAboutPlayer(key, "A harmless description.").Should().BeTrue();

      // A description whose grammatical subject is literally "the player" is the plainest possible tell
      // that the model wrote about the player instead of the NPC, independent of whether a name was ever
      // threaded through.
      [Test]
      public void GIVEN_a_description_opening_with_the_player_WHEN_checked_THEN_it_is_flagged()
         => DiscoverySubjectGuard.IsAboutPlayer("orientation", "The player prefers to lead.").Should().BeTrue();

      // The other reported shape: the description opens with the PLAYER's own name as its subject
      // ("Huan Yi prefers to lead"), recorded on the NPC's profile as if it were a fact about the NPC.
      [Test]
      public void GIVEN_a_description_opening_with_the_players_name_WHEN_checked_THEN_it_is_flagged()
         => DiscoverySubjectGuard.IsAboutPlayer("orientation", "Huan Yi prefers to lead.", "Huan Yi").Should().BeTrue();

      // A single-token player name must match too (the tell is the OPENING word, not the full titled name).
      [Test]
      public void GIVEN_a_description_opening_with_one_token_of_the_players_name_WHEN_checked_THEN_it_is_flagged()
         => DiscoverySubjectGuard.IsAboutPlayer("orientation", "Huan seems drawn to command.", "Huan Yi").Should().BeTrue();

      // ── The false-positive bar (legitimate discoveries must survive) ────

      // Baseline: this is the shape a genuine discovery already takes (DiscoveryParsingTests /
      // DiscoveredTrait's own doc example), third-person, about the NPC. Must never be flagged.
      [Test]
      public void GIVEN_a_genuine_third_person_description_about_the_npc_WHEN_checked_THEN_it_is_not_flagged()
         => DiscoverySubjectGuard.IsAboutPlayer("orientation", "She seems drawn to men.").Should().BeFalse();

      // The player is the OBJECT of the sentence, not its subject: the discovery is about the NPC's own
      // admiration, and dropping it would cost the Encyclopedia every legitimate trait that references the
      // player at all (prefer false negatives over false positives, per the guard's own design brief).
      [Test]
      public void GIVEN_a_description_merely_mentioning_the_player_mid_sentence_WHEN_checked_THEN_it_is_not_flagged()
         => DiscoverySubjectGuard.IsAboutPlayer("archetype", "She admires how Huan Yi handles a blade.", "Huan Yi").Should().BeFalse();

      // A key that merely CONTAINS the substring "player" inside another word (not a namespaced segment)
      // must not misfire; only a whole "_"-delimited segment equal to "player" counts.
      [Test]
      public void GIVEN_a_key_containing_player_as_a_substring_only_WHEN_checked_THEN_it_is_not_flagged()
         => DiscoverySubjectGuard.IsAboutPlayer("playerish_trait", "She seems drawn to men.").Should().BeFalse();

      // A description opening with a THIRD PARTY's name that happens to share no tokens with the player's
      // name must not be flagged just because a player name was supplied.
      [Test]
      public void GIVEN_a_description_opening_with_a_third_partys_name_WHEN_checked_THEN_it_is_not_flagged()
         => DiscoverySubjectGuard.IsAboutPlayer("orientation", "Alympia once courted her too.", "Huan Yi").Should().BeFalse();

      // ── Null/blank safety ────────────────────────────────────────────────

      // ProfileMutator.Apply calls this guard unconditionally whenever a Discovery is present; a null or
      // blank field must never throw, or a single malformed [DISCOVERY] would take down the whole Apply
      // pipeline (events, reputation, romantic arc) for that turn.
      [TestCase(null, null)]
      [TestCase("", "")]
      [TestCase("   ", "   ")]
      [TestCase("orientation", null)]
      [TestCase(null, "She seems drawn to men.")]
      public void GIVEN_null_or_blank_fields_WHEN_checked_THEN_it_does_not_throw_and_is_not_flagged(string? key, string? description)
      {
         var act = () => DiscoverySubjectGuard.IsAboutPlayer(key, description);

         act.Should().NotThrow();
         DiscoverySubjectGuard.IsAboutPlayer(key, description).Should().BeFalse();
      }

      // playerName itself is optional (older callers, e.g. ConsoleRunner's 3-arg Apply, never supply one);
      // a null playerName must not throw and simply disables the name-opening signal, leaving the other two.
      [Test]
      public void GIVEN_a_null_player_name_WHEN_checked_THEN_it_does_not_throw()
      {
         var act = () => DiscoverySubjectGuard.IsAboutPlayer("orientation", "She seems drawn to men.", null);

         act.Should().NotThrow();
      }
   }
}

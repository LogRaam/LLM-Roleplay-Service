// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 9. What these pin: that a wanted face is recognised, that only the trade
// knows whose ground this is, and that neither fact is invented for anybody else.
//
// Why it exists. Gabriel, 2026-09-12: "j'aimerais aussi qu'on s'intéresse aux gangs, aux bandits, etc. car
// plusieurs joueurs jouent des malfrats avec le mod Fourberie."
//
// The audit found something precise, and it shaped the pack: the underworld's VOICE already existed.
// PromptBuilder has matched behaviour guidelines by station for a long time - a gang leader is already told
// "you are an outlaw and a cutthroat, not a nobleman. Speak coarse." So a criminal campaign already met
// characters who SOUNDED right and knew nothing at all. The gap was knowledge, not persona, which is the kind
// of thing that stays invisible until somebody greps for it.
//
// And it is the first pack whose two DEPTHS carry genuinely different facts rather than the same fact at two
// lengths. The player's notoriety is public - a guard, a headman, a merchant and a count all know a wanted
// face. Who holds the criminal trade is not, and a count does not have it.
//
// If these fail, either a wanted player has become invisible to the law, or a nobleman has started knowing
// which gang runs his gutters.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;
using System.Text;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class UnderworldKnowledgeTests
   {
      private static readonly UnderworldPack Pack = new();

      private static KnowledgeBearer GangLeader() => new() {IsNotable = true, IsUnderworld = true};
      private static KnowledgeBearer Count() => new() {IsLord = true, HasHouse = true, HoldsASeat = true};

      private static string Render(UnderworldFacts streets, KnowledgeBearer bearer, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {Underworld = streets, Bearer = bearer}, lean);

         return sb.ToString();
      }

      // ── what everyone knows ──────────────────────────────────────────────

      // A wanted face is recognised by anyone of that realm. Without this a player who has been robbing his
      // way across Calradia is met with perfect courtesy by everyone, which is the immersion break the whole
      // criminal campaign runs into.
      [Test]
      public void GIVEN_a_badly_wanted_player_WHEN_any_local_speaks_THEN_they_know_it()
      {
         Render(new UnderworldFacts {PlayerNotoriety = Notoriety.Severe}, Count())
            .Should().Contain("badly wanted");

         Render(new UnderworldFacts {PlayerNotoriety = Notoriety.Moderate}, Count())
            .Should().Contain("known offender");
      }

      // And the restraint that makes it affordable: this pack is carried by every character in the game, and
      // most players are wanted nowhere. A clean record is not news and must cost nothing.
      [Test]
      public void GIVEN_a_player_with_clean_hands_WHEN_rendered_THEN_no_words_are_spent_vouching_for_them()
      {
         Render(new UnderworldFacts {PlayerNotoriety = Notoriety.Clean}, Count()).Should().BeEmpty();
         Render(new UnderworldFacts {PlayerNotoriety = Notoriety.Clean}, GangLeader()).Should().BeEmpty();
      }

      // A lord being told is not the same as a lord being made to act. Knowing and doing are different, and
      // the mod has never let a prompt decide an outcome.
      [Test]
      public void GIVEN_a_lawful_lord_who_knows_WHEN_told_THEN_what_he_does_about_it_is_left_to_him()
      {
         Render(new UnderworldFacts {PlayerNotoriety = Notoriety.Severe}, Count())
            .Should().Contain("yours to decide");
      }

      // ── what only the trade knows ────────────────────────────────────────

      // THE DEPTH DISTINCTION, and the first one carrying different facts rather than the same fact at two
      // lengths. Whose ground this is, is underworld business.
      [Test]
      public void GIVEN_a_town_with_a_gang_WHEN_a_thief_speaks_THEN_he_knows_whose_ground_it_is()
      {
         var streets = new UnderworldFacts {
            PlayerNotoriety = Notoriety.Clean, PlaceName = "Pravend", StreetsRunBy = "Hulf the Knife"
         };

         Render(streets, GangLeader()).Should().Contain("Hulf the Knife");
      }

      // And a count of that same town does NOT. He owns the walls; he does not know who owns the gutters.
      [Test]
      public void GIVEN_the_same_town_WHEN_its_count_speaks_THEN_he_does_not_know_who_runs_the_gutters()
      {
         var streets = new UnderworldFacts {
            PlayerNotoriety = Notoriety.Clean, PlaceName = "Pravend", StreetsRunBy = "Hulf the Knife"
         };

         Render(streets, Count()).Should().NotContain("Hulf the Knife");
      }

      // The character who IS the gang. It must read as an ordinary fact of his life, never as a boast - the
      // same discipline that stops a governor receiving his own post as news.
      [Test]
      public void GIVEN_the_man_who_holds_the_town_WHEN_he_speaks_THEN_it_is_not_news_and_not_a_boast()
      {
         var streets = new UnderworldFacts {
            PlayerNotoriety = Notoriety.Clean, PlaceName = "Pravend", YouRunTheseStreets = true
         };

         string said = Render(streets, GangLeader());

         said.Should().Contain("is YOURS");
         said.Should().Contain("never speak of that as though it were news or a boast");
         said.Should().NotContain("Hulf");
      }

      // The payoff Gabriel actually asked for: a criminal player should find the underworld easier, not
      // harder. A cutthroat who lectures you about the law has not understood who he is.
      [Test]
      public void GIVEN_a_wanted_player_and_a_thief_WHEN_they_meet_THEN_the_thief_is_not_scandalised_by_them()
      {
         string said = Render(new UnderworldFacts {PlayerNotoriety = Notoriety.Severe}, GangLeader());

         said.Should().Contain("no one to be scandalised");
         said.Should().Contain("never lecture them about the law");
      }

      // And that conduct is for the trade only: a count reading the same line would be a count who shrugs at
      // crime, which is a different character entirely.
      [Test]
      public void GIVEN_a_lawful_lord_WHEN_rendered_THEN_he_is_never_told_to_shrug_at_it()
      {
         Render(new UnderworldFacts {PlayerNotoriety = Notoriety.Severe}, Count())
            .Should().NotContain("scandalised");
      }

      // ── cost and composition ─────────────────────────────────────────────

      // Compact keeps who is wanted and whose ground it is, and drops the conduct: a small model has no room
      // for register, and the facts are what change what a character can say.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_the_facts_survive_and_the_manner_does_not()
      {
         var streets = new UnderworldFacts {
            PlayerNotoriety = Notoriety.Severe, PlaceName = "Pravend", StreetsRunBy = "Hulf the Knife"
         };

         string lean = Render(streets, GangLeader(), true);

         lean.Should().Contain("badly wanted").And.Contain("Hulf the Knife");
         lean.Should().NotContain("scandalised");
      }

      // Everyone of a realm knows a wanted face; only the trade knows the trade. Both halves asserted, because
      // getting either wrong is a different bug: a silent law, or a nobleman with a fence's address book.
      [Test]
      public void GIVEN_who_is_asking_WHEN_composing_THEN_the_trade_knows_more_than_the_town_does()
      {
         Pack.DepthFor(GangLeader()).Should().Be(KnowledgeDepth.Deep);
         Pack.DepthFor(Count()).Should().Be(KnowledgeDepth.Ordinary);
         Pack.DepthFor(new KnowledgeBearer()).Should().Be(KnowledgeDepth.Ordinary);
         Pack.DepthFor(null).Should().Be(KnowledgeDepth.None);
      }

      // Supplied means the host LOOKED, not that it found a criminal. A peaceful town with a clean-handed
      // player is a successful read, and reporting it as a missing fact would teach everyone to ignore the
      // sweep - the personal_bonds lesson, now applied by default.
      [Test]
      public void GIVEN_a_clean_realm_WHEN_swept_THEN_looking_and_finding_nothing_is_not_a_fault()
      {
         Pack.IsSupplied(new EncounterContext {
            Underworld = new UnderworldFacts {PlayerNotoriety = Notoriety.Clean}
         }).Should().BeTrue();

         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {Underworld = new UnderworldFacts()}).Should().BeFalse();
      }
   }
}

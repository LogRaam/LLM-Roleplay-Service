// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 10. What these pin: that word reaches a lord further than it reaches a
// villager, and that the prose which says so is exactly the prose that was already tuned in play.
//
// THE AUDIT FINDING, and it is why this pack replaces something rather than adding something. Realm news
// already reached the prompt by two routes: RealmNewsLine (the one great happening) and WorldRumorsBlock (a
// pre-formatted "what you've heard" list drawn from WorldEventStore, with an awareness filter and a
// confidence rating). Both good, both tuned.
//
// What was missing is precisely what the depth axis names. The host asked the store for six events and then
// kept FOUR, for everybody alike. The filter underneath is geographic and temporal - their faction, their
// region, recency - and never once asks who is listening. So a village headman and a marshal of the realm
// heard exactly the same amount, from exactly as far away. A lord keeps couriers, a court and men whose
// business is knowing things; a villager has the market.
//
// So the pack took ownership of a decision that had been living as a magic number, and turned it into a rule
// about people that can be read and argued with.
//
// ON VERIFYING THE MOVE: the prose below is copied verbatim from the block it replaces (recovered from git),
// and these tests pin it. A byte-diff harness was also run, and I destroyed my own baseline by re-running the
// capture with the output path still pointing at it - so the exact-string assertions here are the evidence,
// not a diff. They are the better guard anyway: a diff is thrown away, and these stay.
//
// If these fail, either the reach has stopped depending on who is listening, or somebody has quietly reworded
// a block that was tuned against real conversations.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class RealmNewsKnowledgeTests
   {
      private static readonly RealmNewsPack Pack = new();

      private static KnowledgeBearer Lord() => new() {IsLord = true, HasHouse = true};
      private static KnowledgeBearer Headman() => new() {IsNotable = true, HoldsASeat = true};
      private static KnowledgeBearer Villager() => new();

      private static RealmNewsFacts Word()
         => new() {
            Headline = "Vlandia has declared war on Sturgia.",
            Tidings = new List<Tiding> {
               new() {Text = "A siege was broken at Ocs Hall."},
               new() {Text = "The count of Pravend has taken a wife.", Confidence = TidingConfidence.Secondhand},
               new() {Text = "Grain has failed around Charas."},
               new() {Text = "A lord was executed in the east.", Confidence = TidingConfidence.Distant},
               new() {Text = "A caravan was taken on the south road."},
               new() {Text = "A tourney is called at Jaculan."}
            }
         };

      private static string Render(KnowledgeBearer bearer, bool lean = false, RealmNewsFacts? news = null)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {RealmNews = news ?? Word(), Bearer = bearer}, lean);

         return sb.ToString();
      }

      private static int Items(string rendered)
         => rendered.Split('\n').Count(l => l.StartsWith("- "));

      // ── the gap this pack was built to close ─────────────────────────────

      // THE FINDING, as a player would meet it: a lord and a villager used to hear the same four things. A man
      // who keeps couriers and a court should simply know more of the world than a man who hears what comes
      // through the market.
      [Test]
      public void GIVEN_the_same_world_WHEN_a_lord_and_a_villager_are_asked_THEN_the_lord_has_heard_more()
      {
         Items(Render(Lord())).Should().BeGreaterThan(Items(Render(Headman())));
         Items(Render(Headman())).Should().BeGreaterThan(Items(Render(Villager())));
      }

      // A distant, unverified rumour has travelled a long way to arrive, and it arrives where people keep
      // couriers. Somebody who hears what the market hears does not get it at all.
      [Test]
      public void GIVEN_a_rumour_from_far_away_WHEN_word_is_shared_THEN_only_the_far_reaching_hear_it()
      {
         Render(Lord()).Should().Contain("executed in the east");
         Render(Headman()).Should().NotContain("executed in the east");
         Render(Villager()).Should().NotContain("executed in the east");
      }

      // THE CONSTRAINT the depth axis is held to, in its most testable form: a shallower reach removes ITEMS.
      // It never hands somebody a vaguer version of the same news, because vagueness is an invitation to
      // invent the details - the failure the whole pillar exists to close.
      [Test]
      public void GIVEN_the_shallowest_reach_WHEN_rendered_THEN_what_survives_is_stated_as_plainly_as_ever()
      {
         string said = Render(Villager());

         said.Should().Contain("A siege was broken at Ocs Hall.");
         said.ToLowerInvariant().Should().NotContain("vaguely").And.NotContain("something about")
             .And.NotContain("some sort of");
      }

      // The great news reaches a village square as surely as a council chamber. Rank changes how much of the
      // WORLD you hear, never whether you heard that the realm went to war.
      [Test]
      public void GIVEN_a_war_declared_WHEN_anyone_at_all_is_asked_THEN_they_have_heard_of_it()
      {
         foreach (KnowledgeBearer person in new[] {Lord(), Headman(), Villager()})
            Render(person).Should().Contain("THE TALK OF ALL THE REALM")
                          .And.Contain("Vlandia has declared war on Sturgia.");
      }

      // ── the prose the migration promised to preserve ─────────────────────

      // Copied verbatim from the block this pack replaced. If somebody rewords it, that should be a decision
      // taken on purpose rather than a side effect of moving code.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_rendered_THEN_the_tuned_wording_is_word_for_word_what_it_was()
      {
         string said = Render(Lord());

         said.Should().Contain("WHAT YOU'VE HEARD (news that has reached you):");
         said.Should().Contain("These are things word has brought to you: battles, sieges, marriages, deaths,");
         said.Should().Contain("heard', 'word came that'), never as a list. This is what you KNOW, not a line to");
         said.Should().Contain("NEVER repeat its wording word for word.");
         said.Should().Contain("This is the great news of the moment");
      }

      // The confidence tags are what let a character repeat a rumour AS a rumour, which the boundary policy
      // calls worth more than a fact invented and told as a fact.
      [Test]
      public void GIVEN_news_of_differing_reliability_WHEN_rendered_THEN_each_carries_how_far_it_travelled()
      {
         string said = Render(Lord());

         said.Should().Contain("(heard secondhand) The count of Pravend has taken a wife.");
         said.Should().Contain("(a distant, unverified rumour) A lord was executed in the east.");
         said.Should().Contain("- A siege was broken at Ocs Hall.");
      }

      // Compact keeps the news and drops the coaching, like every pack.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_the_news_survives_and_the_instructions_do_not()
      {
         string lean = Render(Lord(), true);

         lean.Should().Contain("Vlandia has declared war").And.Contain("A siege was broken at Ocs Hall.");
         lean.Should().NotContain("NEVER repeat its wording");
         lean.Should().NotContain("This is the great news of the moment");
      }

      // ── cost, composition and the cache ──────────────────────────────────

      // News moves, so it belongs below the encounter marker with the rest of what cannot be cached. Rendering
      // it up in the identity block would cost every later section its prefix every time word arrived - the
      // regression here_and_now caused earlier the same day.
      [Test]
      public void GIVEN_this_pack_WHEN_placed_THEN_it_sits_with_the_things_that_change()
      {
         Pack.Volatility.Should().Be(PackVolatility.PerEncounter);
      }

      // A quiet season is real. Nobody having heard anything is not a host that failed to look, so the sweep
      // must not report it as a missing fact.
      [Test]
      public void GIVEN_a_quiet_season_WHEN_swept_THEN_hearing_nothing_is_not_a_fault()
      {
         Pack.RequiresContent.Should().BeFalse();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {RealmNews = new RealmNewsFacts()}).Should().BeFalse();
      }

      // Everyone hears something, so nobody is refused outright - what differs is how much.
      [Test]
      public void GIVEN_anyone_alive_WHEN_composing_THEN_they_hear_something_and_a_lord_hears_furthest()
      {
         Pack.DepthFor(Lord()).Should().Be(KnowledgeDepth.Knowing);
         Pack.DepthFor(Headman()).Should().Be(KnowledgeDepth.Ordinary);
         Pack.DepthFor(Villager()).Should().Be(KnowledgeDepth.Slight);
         Pack.DepthFor(null).Should().Be(KnowledgeDepth.None);
      }

      // A companion of the player's house hears as a lord does: their house IS the player's, and it keeps the
      // same couriers. The same reasoning that makes fkasad's companion know the clan treasury.
      [Test]
      public void GIVEN_a_companion_of_the_players_house_WHEN_composing_THEN_they_hear_as_the_house_hears()
      {
         Pack.DepthFor(new KnowledgeBearer {IsPlayerCompanion = true}).Should().Be(KnowledgeDepth.Knowing);
      }
   }
}

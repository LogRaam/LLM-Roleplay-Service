// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 11. What these pin: that the people riding with the player know what the
// company they are in is like, that whoever answers for a thing knows it more closely, and that a lord met on
// the road knows none of it.
//
// THE PATTERN IT CLOSES, from the role audit Gabriel asked for. EncounterContext carried three OFFER gates -
// NpcCanBeAppointedGovernor, NpcCanBeAssignedPartyRole, NpcCanBeDispatchedOnMission - and exactly ONE of them
// had a matching "post actually held", added the day fkasad reported that his governor did not know he
// governed. The context knew whether a post could be OFFERED and mostly not whether one was HELD, so the mod
// appoints a companion QUARTERMASTER through its own verb and never told him.
//
// AND WHY THE POST ALONE WOULD HAVE BEEN HALF A FIX: telling a man he is quartermaster and nothing else does
// not stop him inventing the stores, it gives him a reason to. That is the granary lesson, applied before it
// could bite a second time - so the post goes in station and what the post KNOWS lives here.
//
// If these fail, either a companion has stopped noticing the camp is starving, or a stranger has started
// knowing how the player's waggons are stocked.

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
   public sealed class PartyStandingKnowledgeTests
   {
      private static readonly PartyStandingPack Pack = new();

      private static KnowledgeBearer Rider(PartyPost post = PartyPost.None)
         => new() {IsPlayerCompanion = true, RidesWithThePlayer = true, PartyPost = post};

      private static KnowledgeBearer LordOnTheRoad() => new() {IsLord = true, HasHouse = true};

      private static string Render(PartyStandingFacts party, KnowledgeBearer bearer, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {PartyStanding = party, Bearer = bearer}, lean);

         return sb.ToString();
      }

      private static PartyStandingFacts HungryAndSullen()
         => new() {Provisions = ProvisionBand.Short, Mood = CampMood.Sullen, Wounded = WoundedBand.Many};

      // ── what anyone in the camp knows ────────────────────────────────────

      // Everybody who marches with a hungry company knows it is hungry. Without this a companion answers
      // questions about the march as though he had joined that morning.
      [Test]
      public void GIVEN_a_hungry_sullen_company_WHEN_a_companion_speaks_THEN_he_knows_what_he_is_riding_in()
      {
         string said = Render(HungryAndSullen(), Rider());

         said.Should().Contain("provisions will not last the week");
         said.Should().Contain("camp is sullen");
         said.Should().Contain("carrying wounds");
      }

      // THE LEAK THIS MUST NOT SPRING. A lord met on the road has no business knowing how the player's waggons
      // are stocked, and handing him that is the failure this whole pillar spends its time preventing.
      [Test]
      public void GIVEN_the_same_company_WHEN_a_lord_on_the_road_speaks_THEN_he_knows_none_of_it()
      {
         Pack.CarriedBy(LordOnTheRoad()).Should().BeFalse();
         Render(HungryAndSullen(), LordOnTheRoad()).Should().BeEmpty();
      }

      // A company that is well found and in good heart earns almost nothing: this pack is read in every
      // conversation with every companion, and most of the time there is nothing wrong.
      [Test]
      public void GIVEN_a_company_with_nothing_wrong_WHEN_rendered_THEN_almost_no_words_are_spent_on_it()
      {
         string said = Render(new PartyStandingFacts {
            Provisions = ProvisionBand.Sufficient, Mood = CampMood.Steady, Wounded = WoundedBand.Few
         }, Rider());

         said.Should().BeEmpty();
      }

      // ── what the post-holder knows ───────────────────────────────────────

      // The depth axis doing the work it was built for: whoever answers for a thing knows it more closely than
      // the men who merely notice it.
      [Test]
      public void GIVEN_the_man_who_answers_for_the_stores_WHEN_composing_THEN_he_knows_it_more_closely()
      {
         Pack.DepthFor(Rider(PartyPost.Quartermaster)).Should().Be(KnowledgeDepth.Deep);
         Pack.DepthFor(Rider(PartyPost.Surgeon)).Should().Be(KnowledgeDepth.Deep);
         Pack.DepthFor(Rider()).Should().Be(KnowledgeDepth.Ordinary);
         Pack.DepthFor(LordOnTheRoad()).Should().Be(KnowledgeDepth.None);
      }

      // THE GRANARY LESSON, applied before it bit again: a man told he is quartermaster and nothing else has a
      // reason to invent the tally. He is told to speak as somebody who checks it - and still not to count.
      [Test]
      public void GIVEN_the_quartermaster_WHEN_the_full_prompt_is_built_THEN_he_is_told_to_know_it_and_not_to_count_it()
      {
         string said = Render(HungryAndSullen(), Rider(PartyPost.Quartermaster));

         said.Should().Contain("your post to answer for this");
         said.Should().Contain("Still no figures");
      }

      // And an ordinary rider gets the facts without the post-holder's charge, or every companion would answer
      // as though the ledger were theirs.
      [Test]
      public void GIVEN_an_ordinary_rider_WHEN_rendered_THEN_he_is_not_handed_somebody_elses_post()
      {
         Render(HungryAndSullen(), Rider()).Should().NotContain("your post to answer for this");
      }

      // ── cost and completeness ────────────────────────────────────────────

      // Compact keeps the state of the company and drops the coaching, like every pack.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_the_camp_survives_and_the_charge_does_not()
      {
         string lean = Render(HungryAndSullen(), Rider(PartyPost.Quartermaster), true);

         lean.Should().Contain("provisions will not last the week");
         lean.Should().NotContain("Still no figures");
      }

      // A company always has a temper and always has stores, so an empty reading on somebody riding in it
      // means nobody read the party - the fkasad class of fault, one more time.
      [Test]
      public void GIVEN_a_rider_with_nothing_supplied_WHEN_swept_THEN_it_counts_as_a_fault()
      {
         Pack.RequiresContent.Should().BeTrue();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {PartyStanding = new PartyStandingFacts()}).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {PartyStanding = HungryAndSullen()}).Should().BeTrue();
      }

      // No figures anywhere, like every band in the pillar: what a player can check is what a character must
      // not invent.
      [Test]
      public void GIVEN_the_worst_the_company_can_be_WHEN_rendered_THEN_not_one_figure_appears()
      {
         string said = Render(new PartyStandingFacts {
            Provisions = ProvisionBand.Starving, Mood = CampMood.Mutinous, Wounded = WoundedBand.Most
         }, Rider(PartyPost.Quartermaster));

         said.Should().NotBeEmpty();
         said.Should().NotContainAny("0", "1", "2", "3", "4", "5", "6", "7", "8", "9");
      }
   }
}

// Code written by Gabriel Mailhot, 12/09/2026.
// Knowledge composition, increment 6. What these pin: that a character knows the state of their own body, and
// that being well costs the prompt nothing.
//
// Why it exists, and it is the first pack found by the AUDIT rather than by a player: grepping EncounterContext
// returned zero for wound, injury and health, while the engine had kept Hero.IsWounded, HitPoints,
// MaxHitPoints and IsDisabled all along. So a lord carried off a field with a spear through his shoulder
// talked as though nothing had happened, and a man the game had permanently maimed never mentioned it.
//
// What the same audit REMOVED from this increment, which is worth as much: age. The first draft proposed it;
// checking the prompt before writing the code found PromptBuilder already states the character's years and
// life stage and already forbids affecting a youth they have outgrown. A second source for one fact is how two
// sources come to disagree.
//
// If these fail, either a wounded character has stopped knowing it, or a healthy one has started spending
// prompt on being fine.

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
   public sealed class OwnBodyKnowledgeTests
   {
      private static readonly OwnBodyPack Pack = new();

      private static string Render(OwnBodyFacts body, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {OwnBody = body}, lean);

         return sb.ToString();
      }

      // The gap, as a player would meet it: a lord who has just been carried off a field.
      [Test]
      public void GIVEN_a_badly_wounded_lord_WHEN_he_speaks_THEN_he_knows_he_is_wounded()
      {
         Render(new OwnBodyFacts {Hurt = HurtBand.Wounded}).Should().Contain("wounded");
         Render(new OwnBodyFacts {Hurt = HurtBand.Grievous}).Should().Contain("barely on your feet");
      }

      // THE COST RULE, and it is why this pack is affordable at all: it is carried by every character in the
      // game, and almost all of them are fine. A sentence saying somebody is not injured is a sentence wasted
      // in a Compact prompt that has no room to spare.
      [Test]
      public void GIVEN_a_character_who_is_perfectly_well_WHEN_rendered_THEN_it_costs_nothing_at_all()
      {
         Render(new OwnBodyFacts {Hurt = HurtBand.Unhurt}).Should().BeEmpty();
         Render(new OwnBodyFacts {Hurt = HurtBand.Unhurt}, true).Should().BeEmpty();
      }

      // A maiming and a wound say completely different things about a man, and he can have one without the
      // other: the game records a permanent disability separately from today's hit points.
      [Test]
      public void GIVEN_a_man_maimed_long_ago_but_otherwise_well_WHEN_he_speaks_THEN_he_still_carries_it()
      {
         string said = Render(new OwnBodyFacts {Hurt = HurtBand.Unhurt, IsMaimed = true});

         said.Should().Contain("never healed");
         said.Should().NotContain("wounded");
      }

      // The half that makes it matter beyond flavour: a man barely on his feet must not agree to ride out with
      // you. This is the anti-empty-promise rule reaching a fact it never had.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_rendered_THEN_he_is_told_not_to_promise_what_his_body_cannot_do()
      {
         Render(new OwnBodyFacts {Hurt = HurtBand.Grievous}).Should().Contain("could not do today");
      }

      // And Compact keeps the fact while dropping the manner, like every pack: a character told he is bleeding
      // rarely needs telling to act like it.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_the_wound_survives_and_the_coaching_does_not()
      {
         string lean = Render(new OwnBodyFacts {Hurt = HurtBand.Wounded}, true);

         lean.Should().Contain("wounded");
         lean.Should().NotContain("could not do today");
      }

      // ── composition and completeness ─────────────────────────────────────

      // A beggar knows he is bleeding as surely as a king does.
      [Test]
      public void GIVEN_anyone_at_all_WHEN_composing_THEN_they_know_their_own_body()
      {
         Pack.CarriedBy(new KnowledgeBearer()).Should().BeTrue();
         Pack.CarriedBy(new KnowledgeBearer {IsNotable = true}).Should().BeTrue();
         Pack.CarriedBy(null).Should().BeFalse();
      }

      // The distinction that keeps the live sweep honest: SUPPLIED means the host read the body, not that the
      // body is interesting. A hero who is perfectly well is a successful read, and reporting him as a missing
      // fact would teach everyone to ignore the sweep - the personal_bonds lesson.
      [Test]
      public void GIVEN_a_healthy_hero_WHEN_swept_THEN_a_successful_read_is_not_a_missing_fact()
      {
         Pack.RequiresContent.Should().BeTrue();
         Pack.IsSupplied(new EncounterContext {OwnBody = new OwnBodyFacts {Hurt = HurtBand.Unhurt}})
             .Should().BeTrue();

         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {OwnBody = new OwnBodyFacts()}).Should().BeFalse();
      }

      // Age belongs to the identity block and must not be duplicated here. Asserted rather than trusted,
      // because the first draft of this increment DID propose it.
      [Test]
      public void GIVEN_this_pack_WHEN_read_THEN_it_never_speaks_about_years_which_identity_already_owns()
      {
         Pack.Covers.Should().Contain("Age is NOT here");

         foreach (HurtBand band in new[] {HurtBand.Scratched, HurtBand.Wounded, HurtBand.Grievous})
            Render(new OwnBodyFacts {Hurt = band, IsMaimed = true}).ToLowerInvariant()
               .Should().NotContain("years").And.NotContain("old man").And.NotContain("young");
      }

      // A MAIMING IS A READING OF THE BODY. IsSupplied asked about the hurt band alone while HasAnythingToSay
      // counts a maiming on its own, so a hero carrying an old injury and no current wound reported himself
      // unsupplied — ComposeKnowledge never called Render, and the maiming was dropped without a word (audit,
      // 15/09/2026). Two predicates about one thing must ask the same question.
      [Test]
      public void GIVEN_an_old_injury_and_no_fresh_wound_WHEN_swept_THEN_the_body_counts_as_read()
      {
         var facts = new OwnBodyFacts {Hurt = HurtBand.Unknown, IsMaimed = true};

         facts.HasAnythingToSay.Should().BeTrue("the maiming is the thing worth saying");
         Pack.IsSupplied(new EncounterContext {OwnBody = facts}).Should().BeTrue();
      }

      // And it must actually reach the prose, which is the half a player would notice. The mood design turns
      // on this distinction — a fresh wound and an old one are different men — and it cannot land if the
      // maiming never reaches the prompt at all.
      [Test]
      public void GIVEN_an_old_injury_and_no_fresh_wound_WHEN_rendered_THEN_it_is_still_spoken_of()
      {
         Render(new OwnBodyFacts {Hurt = HurtBand.Unknown, IsMaimed = true})
            .Should().Contain("never healed");
      }

      // The control: a body nobody read at all is still NOT supplied, or the fix would have turned the sweep
      // off for this pack rather than correcting it.
      [Test]
      public void GIVEN_a_body_nobody_read_WHEN_swept_THEN_it_is_still_reported_unsupplied()
      {
         Pack.IsSupplied(new EncounterContext {OwnBody = new OwnBodyFacts {Hurt = HurtBand.Unknown}})
             .Should().BeFalse();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
      }
   }
}

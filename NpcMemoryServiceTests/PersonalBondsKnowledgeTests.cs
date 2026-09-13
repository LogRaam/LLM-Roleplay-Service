// Code written by Gabriel Mailhot, 11/09/2026.
// Knowledge composition, increment 2. What these pin: that a character carries the people in their OWN life,
// and says as much of it as they actually would to the person in front of them.
//
// Why it exists. Gabriel: "la mémoire d'un NPC aujourd'hui est relative au joueur car elle s'inscrit pendant un
// chat. Mais le NPC devrait aussi avoir une mémoire envers d'autres afin qu'il puisse en parler."
//
// He was right, and checking made it exact: GrudgeNote is documented as the grievances an NPC nurses AGAINST
// THE PLAYER, and every romance field is about the player or the player's partner. The simulation meanwhile
// runs NPC-to-NPC lives - one cr.testall pass had a lady take a secret lover, another bear a lord's child, and
// a companion nurse a grudge against his captor. All of it happened; none of those people could mention it.
//
// The second half is the one that earns its keep. A feud is something a man says. A love, especially a secret
// one, is something he keeps. If these tests fail, either a character has stopped having a life of his own, or
// he has started telling strangers about it.

#region

using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;
using System.Linq;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PersonalBondsKnowledgeTests
   {
      private static readonly PersonalBondsPack Pack = new();

      private static ListeningAudience Confidant() => new() {Regard = 60, InPrivate = true};
      private static ListeningAudience Acquaintance() => new() {Regard = 0, InPrivate = true};
      private static ListeningAudience RoomFullOfPeople() => new() {Regard = 60, InPrivate = false};

      private static string Render(IEnumerable<PersonalBond> bonds, ListeningAudience audience, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {
            PersonalBonds = new PersonalBondsFacts {Bonds = new List<PersonalBond>(bonds)},
            Audience = audience
         }, lean);

         return sb.ToString();
      }

      private static PersonalBond Feud(string who) =>
         new() {PersonName = who, Kind = BondKind.Enemy, Note = "who took his brother at Pravend", Discretion = Discretion.Open};

      private static PersonalBond Affair(string who) =>
         new() {PersonName = who, Kind = BondKind.Beloved, Discretion = Discretion.Secret};

      private static PersonalBond OldLove(string who) =>
         new() {PersonName = who, Kind = BondKind.LostLove, Note = "who left him for a Vlandian", Discretion = Discretion.Guarded};

      // THE REQUEST. A character has people of his own, and can speak of them.
      [Test]
      public void GIVEN_a_feud_with_someone_who_is_not_the_player_WHEN_rendered_THEN_he_can_speak_of_it()
      {
         string said = Render(new[] {Feud("Zerosica")}, Acquaintance());

         said.Should().Contain("Zerosica").And.Contain("Pravend");
      }

      // A bond with no reason behind it gives a character a name to drop and nothing to feel, so the clause of
      // WHY is part of the fact rather than decoration.
      [Test]
      public void GIVEN_a_bond_WHEN_rendered_THEN_the_reason_travels_with_the_name()
      {
         Render(new[] {OldLove("Ira")}, Confidant()).Should().Contain("who left him for a Vlandian");
      }

      // ── discretion: the axis this increment exists for ───────────────────

      // A grievance is ordinary talk. He says it to anyone, in any company.
      [Test]
      public void GIVEN_an_open_grievance_WHEN_a_stranger_asks_in_a_crowded_room_THEN_he_still_says_it()
      {
         Render(new[] {Feud("Zerosica")}, RoomFullOfPeople()).Should().Contain("Zerosica");
      }

      // A private matter is for someone he has reason to trust, alone. Both halves are required, and each has
      // its own test below so a regression cannot hide behind the other.
      [Test]
      public void GIVEN_a_guarded_matter_WHEN_told_to_a_confidant_in_private_THEN_he_speaks_of_it()
      {
         Render(new[] {OldLove("Ira")}, Confidant()).Should().Contain("Ira");
      }

      [Test]
      public void GIVEN_a_guarded_matter_WHEN_the_listener_is_barely_known_THEN_he_keeps_it()
      {
         Render(new[] {OldLove("Ira")}, Acquaintance()).Should().NotContain("Ira");
      }

      [Test]
      public void GIVEN_a_guarded_matter_WHEN_others_are_listening_THEN_he_keeps_it_however_much_he_trusts_you()
      {
         Render(new[] {OldLove("Ira")}, RoomFullOfPeople()).Should().NotContain("Ira");
      }

      // THE ONE THAT PROTECTS AN EXISTING PILLAR. A secret is never rendered - not even as a thing being
      // withheld, because naming a secret in the prompt is how a secret gets told. Whether a held secret may
      // ever surface belongs to the mystery-lover pillar, which already owns that decision and its cost.
      [Test]
      public void GIVEN_a_secret_love_WHEN_rendered_to_his_closest_confidant_alone_THEN_it_never_appears_at_all()
      {
         string said = Render(new[] {Affair("Tulag"), Feud("Zerosica")}, Confidant());

         said.Should().NotContain("Tulag");
         said.Should().Contain("Zerosica"); // and the rest is unaffected
      }

      // A thing the listener already watched happen is not being disclosed by mentioning it, and a character who
      // acts cagey about it reads as broken rather than discreet.
      [Test]
      public void GIVEN_something_the_player_already_knows_WHEN_rendered_THEN_he_does_not_pretend_to_hide_it()
      {
         var audience = new ListeningAudience {Regard = 0, InPrivate = false, AlreadyKnownToListener = true};

         Render(new[] {OldLove("Ira")}, audience).Should().Contain("Ira");
      }

      // Refusing on an unknown audience is deliberate: guessing wrong in this one place costs a secret.
      [Test]
      public void GIVEN_no_idea_who_is_listening_WHEN_rendered_THEN_only_open_matters_are_spoken()
      {
         string said = Render(new[] {Feud("Zerosica"), OldLove("Ira"), Affair("Tulag")}, null);

         said.Should().Contain("Zerosica");
         said.Should().NotContain("Ira").And.NotContain("Tulag");
      }

      // ── the bound, so this does not become a gossip engine ───────────────

      // A character who can discuss everyone becomes a rumour mill, and CR already has a rumour path that would
      // then be doubled. These are the few he would actually raise.
      [Test]
      public void GIVEN_more_people_than_anyone_would_raise_WHEN_rendered_THEN_only_a_handful_are_named()
      {
         var many = new List<PersonalBond>();
         for (var i = 0; i < 9; i++) many.Add(Feud("Person" + i));

         string said = Render(many, Confidant());

         // Counts the PEOPLE, not the lines. It used to count lines as a proxy, and a line of coaching added
         // for an unrelated reason broke it - a test bound to the shape of the block rather than to the rule
         // it guards, which is the thing this repository keeps learning not to write.
         said.Split('\n').Count(line => line.StartsWith("- "))
             .Should().Be(PersonalBondsFacts.MaxNamed);
      }

      // Nothing tellable must produce no heading, rather than a title over an empty list.
      [Test]
      public void GIVEN_nothing_he_would_say_WHEN_rendered_THEN_no_empty_heading_is_printed()
      {
         Render(new[] {Affair("Tulag")}, Confidant()).Should().BeEmpty();
         Render(new List<PersonalBond>(), Confidant()).Should().BeEmpty();
      }

      // ── composition and completeness ─────────────────────────────────────

      // Everyone has a life. Saying so plainly is better than inventing a condition to make the rule look
      // substantial; the interesting rule for this pack is discretion, not carriage.
      [Test]
      public void GIVEN_any_person_at_all_WHEN_composing_THEN_they_carry_their_own_people()
      {
         Pack.CarriedBy(new KnowledgeBearer()).Should().BeTrue();
         Pack.CarriedBy(new KnowledgeBearer {IsNotable = true}).Should().BeTrue();
         Pack.CarriedBy(null).Should().BeFalse();
      }

      // An empty life is not a missing fact. The live sweep's FIRST run in a real game reported four lords as
      // carried-but-not-supplied here, simply because none had a living feud or a lover; failing on that would
      // train everyone to ignore the report. house_standing is the other kind: a man with a house has a house
      // with a name, so an empty one means nobody read the engine.
      [Test]
      public void GIVEN_a_lord_with_no_feud_and_no_lover_WHEN_swept_THEN_his_empty_life_is_not_a_missing_fact()
      {
         Pack.RequiresContent.Should().BeFalse();
         new HouseStandingPack().RequiresContent.Should().BeTrue();
      }

      // Carried and supplied stay separate questions, so the live sweep can tell "he would know" from "anyone
      // told him" - the gap fkasad's governor lived in.
      [Test]
      public void GIVEN_a_context_with_no_bonds_supplied_WHEN_asked_THEN_the_pack_reports_it_as_unsupplied()
      {
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {PersonalBonds = new PersonalBondsFacts()}).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext {
            PersonalBonds = new PersonalBondsFacts {Bonds = new List<PersonalBond> {Feud("Zerosica")}}
         }).Should().BeTrue();
      }

      // Lean carries the people and drops the coaching, the same discipline the house pack keeps.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_rendered_THEN_the_people_survive_and_the_coaching_does_not()
      {
         var bonds = new[] {Feud("Zerosica")};

         Render(bonds, Confidant(), true).Should().Contain("Zerosica");
         Render(bonds, Confidant(), true).Length.Should().BeLessThan(Render(bonds, Confidant()).Length);
      }

      // Found by a FLAKY live probe, which is a finding rather than a nuisance. Asked point blank whom she
      // could not forgive, a lord with a live grievance named him once and then, on the identical seed and the
      // identical question, answered "grudges are a currency in this land... but forgive is a heavier word" -
      // a musing somebody with NO grudge would have given word for word.
      //
      // The prompt had bought that coin flip: "yours to raise or not", and "bring one up when it bears on what
      // is being said", are both unqualified permissions to stay silent, and a direct question is not an
      // opening. The restraint stays - it is what stops recitation - and now it says what to do when asked.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_read_THEN_a_plain_question_is_owed_a_plain_answer()
      {
         string said = Render(new[] {
            new PersonalBond {PersonName = "Manteos", Kind = BondKind.Enemy, Discretion = Discretion.Open}
         }, new ListeningAudience {Regard = 20, InPrivate = false});

         said.Should().Contain("asked about them PLAINLY");
         said.Should().Contain("rather than in generalities");

         // And the restraint it qualifies must survive: without it every conversation opens with a roll-call.
         said.Should().Contain("never recite them");
      }
   }
}

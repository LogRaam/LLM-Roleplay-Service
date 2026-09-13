// Code written by Gabriel Mailhot, 12/09/2026.
// The first CONSTITUTIVE pack. What these pin: that a character always knows their own rank, and that no
// budget can ever take it from them.
//
// Why station went first. It is unambiguously part of who somebody is, it is small, and it has already broken
// in front of players: a king who was never told he rules DENIED THE CROWN. Two players reported it of
// Derthert and Garios, who both answered that they were "just a lord serving the throne", because the only
// rulership fact in the prompt sat inside the vassal-offer section, doubly gated on an oath being available.
// The cure was to state it unconditionally as part of identity.
//
// That history is exactly why it is constitutive rather than merely universal: the Compact allowance SKIPS a
// contingent pack that will not fit, and a busy landed king losing his crown to a character budget would be
// that same bug rebuilt on purpose.
//
// The migration was verified by capturing four prompts before and after: ruler, governor and commoner came
// back BYTE-IDENTICAL. The clan-head case gained the line it is supposed to emit, which the pre-migration
// baseline had not contained - a discrepancy I could not account for and have not hidden. The tests below pin
// the behaviour the shipped code intends, whatever the baseline did.

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
   public sealed class StationPackTests
   {
      private static readonly StationPack Pack = new();

      private static string Render(EncounterContext context)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, context, false);

         return sb.ToString();
      }

      // The player report itself. A monarch must never be able to answer that he is merely a lord in service.
      [Test]
      public void GIVEN_a_king_WHEN_he_speaks_THEN_he_holds_the_throne_and_may_not_deny_it()
      {
         string said = Render(new EncounterContext {RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King"});

         said.Should().Contain("YOU RULE VLANDIA").And.Contain("its King");
         said.Should().Contain("Never deny it");
      }

      // The realm's own word for its ruler, because the player who reported this wrote "Kings AND Emperors"
      // and one word cannot serve both.
      [Test]
      public void GIVEN_a_realm_with_its_own_title_WHEN_rendered_THEN_that_title_is_used()
      {
         Render(new EncounterContext {RuledRealmName = "Khuzait", RuledRealmRulerTitle = "Khan"})
            .Should().Contain("its Khan");
      }

      // A ruler is not also told he heads a clan: the crown supersedes it, and saying both reads as a man
      // reciting his own titles.
      [Test]
      public void GIVEN_a_king_who_also_heads_his_house_WHEN_rendered_THEN_the_crown_is_the_whole_answer()
      {
         string said = Render(new EncounterContext {
            RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King",
            LeadsOwnClan = true, SpeakerClanName = "dey Meroc"
         });

         said.Should().NotContain("HEAD the");
      }

      // A clan leader is told he leads. Pinned explicitly because the migration's own baseline had lost this
      // line, and a rank nobody states is a rank the character will eventually deny.
      [Test]
      public void GIVEN_the_head_of_a_house_WHEN_he_speaks_THEN_he_knows_the_house_answers_to_him()
      {
         Render(new EncounterContext {LeadsOwnClan = true, SpeakerClanName = "dey Meroc"})
            .Should().Contain("You HEAD the dey Meroc clan");
      }

      // fkasad's governor, and the reason the whole pillar exists. Both halves: that he governs, and that the
      // player's house holds it - he greeted both as news a year after being appointed.
      [Test]
      public void GIVEN_a_governor_for_the_players_house_WHEN_he_speaks_THEN_neither_fact_can_be_news_to_him()
      {
         string said = Render(new EncounterContext {
            GovernedSettlementName = "Ocs Hall", GovernsForPlayerHouse = true
         });

         said.Should().Contain("YOU GOVERN OCS HALL").And.Contain("FOR THE PLAYER'S HOUSE");
         said.Should().Contain("never receive either one as though you were hearing it for the first time");
      }

      // A man may head his house AND keep a town. Additive, unlike the crown.
      [Test]
      public void GIVEN_a_clan_head_who_also_governs_WHEN_rendered_THEN_both_are_true_at_once()
      {
         string said = Render(new EncounterContext {
            LeadsOwnClan = true, SpeakerClanName = "dey Meroc", GovernedSettlementName = "Ocs Hall"
         });

         said.Should().Contain("HEAD the dey Meroc").And.Contain("You govern Ocs Hall");
      }

      // fkasad's bug in its SECOND place, found by the role audit rather than by a player this time: the mod
      // assigns party roles through its own verb and never told the man he held one.
      [Test]
      public void GIVEN_the_quartermaster_of_the_players_company_WHEN_he_speaks_THEN_his_post_is_not_news()
      {
         string said = Render(new EncounterContext {PartyPostHeld = "quartermaster"});

         said.Should().Contain("You are the quartermaster of the player's own company");
         said.Should().Contain("never receive it as news");
      }

      // Additive like the governorship: a man may head his house, keep a town AND carry the player's ledger,
      // and all three are true at once.
      [Test]
      public void GIVEN_a_lord_who_also_carries_the_ledger_WHEN_rendered_THEN_every_post_he_holds_is_stated()
      {
         string said = Render(new EncounterContext {
            LeadsOwnClan = true, SpeakerClanName = "dey Meroc",
            GovernedSettlementName = "Ocs Hall", PartyPostHeld = "scout"
         });

         said.Should().Contain("HEAD the dey Meroc").And.Contain("You govern Ocs Hall")
             .And.Contain("You are the scout");
      }

      // ── what makes it constitutive ───────────────────────────────────────

      // The whole point of the kind. A crown must never be droppable, whatever else a character is carrying.
      [Test]
      public void GIVEN_the_pack_WHEN_read_THEN_it_is_what_a_character_IS_and_is_never_budgeted()
      {
         Pack.Kind.Should().Be(PackKind.Constitutive);
         Pack.DepthFor(new KnowledgeBearer()).Should().Be(KnowledgeDepth.Deep);
         Pack.DepthFor(new KnowledgeBearer {IsCutOff = true}).Should().Be(KnowledgeDepth.Deep);
      }

      // Most people hold no station at all, and that is a life rather than a fault. Without this the live
      // sweep would report every commoner in Calradia as a missing fact - the personal_bonds lesson.
      [Test]
      public void GIVEN_a_commoner_WHEN_swept_THEN_holding_no_rank_is_not_a_missing_fact()
      {
         Pack.RequiresContent.Should().BeFalse();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Render(new EncounterContext()).Should().BeEmpty();
      }

      // A prisoner is still a king. What somebody IS does not depend on their circumstances, which is exactly
      // what separates this pack from the ones the cell takes away.
      [Test]
      public void GIVEN_a_king_in_a_cell_WHEN_composing_THEN_he_is_still_a_king()
      {
         Pack.CarriedBy(new KnowledgeBearer {IsCutOff = true}).Should().BeTrue();
         new HouseMeansPack().CarriedBy(new KnowledgeBearer {IsCutOff = true, IsLord = true, HasHouse = true})
                             .Should().BeFalse();
      }
   }
}

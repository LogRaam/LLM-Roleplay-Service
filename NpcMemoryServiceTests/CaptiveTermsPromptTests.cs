// Code written by Gabriel Mailhot, 14/10/2026.
// What this pins: a captive of the player is told what she can offer for her freedom, and how readily, before she speaks.
//
// Maifailun (Nexus, 14/10/2026): Adrichea (Generosity, Honor and Mercy all -1), held by the player and asked to swear
// fealty to Rhagaea or spend a night with him, answered "kill me if you must", breaking her character. Gabriel
// (14/10/2026): the captive proposes terms herself; in Mature content and above, a manipulative, honorless one uses her
// body without scruple. The host decides the terms (only what the game can carry out) and her disposition; this section
// says them. If these fail, an honorless captive is left to play the martyr, or is told she can offer what she cannot.

#region

using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CaptiveTermsPromptTests
   {
      private static string Prompt(EncounterContext context)
         => LeanRealPromptBudgetTests.RealBuilder().BuildSystemPrompt(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, context);

      private static EncounterContext Captive(string disposition, params string[] terms) => new() {
         PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive, CaptiveDisposition = disposition, CaptiveTerms = terms,
         CaptiveRansomCeiling = 5000, CaptiveFealtyRealm = "the Northern Empire"
      };

      // THE REPORTED CASE: an honorless captive makes the first offer and plays her strongest card, her body included
      // when it is a term, instead of daring the player to kill her.
      [Test]
      public void GIVEN_an_honorless_captive_with_every_term_WHEN_her_prompt_is_built_THEN_she_is_told_she_offers_first_and_what_she_can_offer()
      {
         string p = Prompt(Captive("offers_first", "ransom", "fealty", "intimacy"));

         p.Should().Contain("WHAT YOU CAN OFFER FOR YOUR FREEDOM");
         p.Should().Contain("up to 5,000 denars");
         p.Should().Contain("swear your house to the Northern Empire");
         p.Should().Contain("without scruple");
         p.Should().Contain("no taste for martyrdom");
      }

      // Only the terms the host opened are listed: no intimacy where the player set none or she would not, no fealty
      // where the game cannot take it.
      [Test]
      public void GIVEN_only_a_ransom_open_WHEN_built_THEN_no_other_term_is_offered()
      {
         string p = Prompt(Captive("bargains", "ransom"));

         p.Should().Contain("up to 5,000 denars");
         p.Should().NotContain("- fealty:");
         p.Should().NotContain("- yourself:");
         p.Should().NotContain("- service:");
      }

      // A principled captive is told her honor is real: she may wait for ransom and never sells herself.
      [Test]
      public void GIVEN_an_honor_bound_captive_WHEN_built_THEN_she_may_stand_on_pride()
      {
         Prompt(Captive("stands_on_pride", "ransom")).Should().Contain("would never sell yourself");
      }

      // Nobody else is told any of it: a free character, or a captive the host decided nothing for, gets no section.
      [Test]
      public void GIVEN_no_captive_of_the_player_WHEN_built_THEN_there_is_no_offer_section()
      {
         Prompt(new EncounterContext {CaptiveDisposition = "offers_first", CaptiveTerms = new[] {"ransom"}}).Should().NotContain("WHAT YOU CAN OFFER FOR YOUR FREEDOM (");
         Prompt(new EncounterContext {PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive}).Should().NotContain("WHAT YOU CAN OFFER FOR YOUR FREEDOM (");
      }
   }
}

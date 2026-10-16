// Code written by Gabriel Mailhot, 15/10/2026.
// What this pins: a liege the player owes for sitting out his war is told, in the conversation itself, what is owed,
// how it may be paid there, and the one consequence the game will carry out.
//
// Maifailun (Nexus, 15/10/2026) asked for the vassal's duty to ride to war; Gabriel (15/10/2026): "La lettre devrait
// etre une convocation ou le joueur doit aller rencontrer le Lord pour rendre des comptes. Si le joueur ne participe
// pas, le Lord pourrait alors demander compensation en taxes." The host words the terms from the reckoning; this
// section frames them. If these fail, the summoned vassal stands before a king who has no idea why he came, or a king
// who threatens a punishment the game will never carry out, or one who thanks the player for a levy never paid.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class WarDutyTermsPromptTests
   {
      private const string Terms = "You demand a levy of 4,000 denars for their absence from the front; 4,000 are still owed, within 20 days.";

      private static string Prompt(EncounterContext context)
         => LeanRealPromptBudgetTests.RealBuilder().BuildSystemPrompt(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, context);

      // THE DESIGN: the summoned vassal came to answer for it, and the liege knows what he demands, face to face.
      [Test]
      public void GIVEN_a_vassal_who_owes_the_levy_WHEN_the_liege_prompt_is_built_THEN_he_knows_what_is_owed()
      {
         string p = Prompt(new EncounterContext {WarDutyTerms = Terms});

         p.Should().Contain("THE PLAYER'S RECKONING BEFORE YOU, THEIR LIEGE:");
         p.Should().Contain(Terms);
      }

      // Gating before declaration (Gabriel, 09/09/2026): the liege threatens only what the game will do, and never
      // thanks the player for a levy that has not changed hands.
      [Test]
      public void GIVEN_the_terms_WHEN_built_THEN_he_threatens_nothing_else_and_never_calls_an_unpaid_levy_paid()
      {
         string p = Prompt(new EncounterContext {WarDutyTerms = Terms});

         p.Should().Contain("threaten nothing it does not");
         p.Should().Contain("Never call the levy paid or settled until the game confirms");
      }

      // What is still owed moves with every hand-over in the same conversation, so the terms ride the per-turn tail.
      [Test]
      public void GIVEN_the_terms_WHEN_built_THEN_they_sit_below_the_cached_prefix()
      {
         string p = Prompt(new EncounterContext {WarDutyTerms = Terms});

         p.IndexOf(Terms, System.StringComparison.Ordinal).Should().BeGreaterThan(p.IndexOf("CURRENT ENCOUNTER:", System.StringComparison.Ordinal));
      }

      // Nobody else hears of it: a character the player owes nothing gets no reckoning.
      [Test]
      public void GIVEN_nothing_owed_WHEN_built_THEN_there_is_no_reckoning()
      {
         Prompt(new EncounterContext()).Should().NotContain("THE PLAYER'S RECKONING BEFORE YOU");
      }
   }
}

// Code written by Gabriel Mailhot, 24/08/2026.
// Council Tier 2: an NPC's still-standing council accords with the player (host-composed clauses on
// EncounterContext.StandingAccords) are injected so the character can reference them in ordinary 1:1 talk. These
// tests pin that the section renders the host's clauses VERBATIM with a "never invent one" guard, that it is Full
// prompt only (the accords are descriptive standing state, and the Lean prompt has a hard char budget that this
// content must never touch, see LeanPromptPolicyTests), and that it stays absent when the NPC has no accord.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class StandingAccordsPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Southern Empire",
         Clan = "Pethros"
      };

      private static string Build(LeanPromptLevel level, IReadOnlyList<string> accords)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = level, StandingAccords = accords});

      // The whole point of Tier 2: the character can speak to a live accord. The header and the host's exact clause
      // must both reach the prompt, or the NPC has no idea the accord exists.
      [Test]
      public void GIVEN_a_full_prompt_with_a_standing_accord_THEN_the_section_and_the_clause_verbatim_are_present()
      {
         string prompt = Build(LeanPromptLevel.Full, new List<string> {"You owe the player a tribute of 300 denars a day, sworn at a parley (12 days remain)."});

         prompt.Should().Contain("STANDING ACCORDS WITH THE PLAYER");
         prompt.Should().Contain("You owe the player a tribute of 300 denars a day, sworn at a parley (12 days remain).");
      }

      // The anti-hallucination guard is the reason the clauses are host-composed and injected raw: without the
      // explicit "never invent" rule a model tends to embroider extra terms onto a real accord.
      [Test]
      public void GIVEN_a_full_prompt_with_a_standing_accord_THEN_the_never_invent_rule_is_stated()
      {
         Build(LeanPromptLevel.Full, new List<string> {"A non-aggression pact stands between your house and the player's, sworn at a parley (30 days remain)."})
            .Should().Contain("never invent an accord not listed here");
      }

      // Every clause must appear, not just the first: an NPC can carry more than one accord (a tribute AND a pact),
      // and dropping the tail would silently hide half their obligations.
      [Test]
      public void GIVEN_a_full_prompt_with_several_accords_THEN_every_clause_is_listed()
      {
         var accords = new List<string> {
            "You owe the player a tribute of 300 denars a day, sworn at a parley (12 days remain).",
            "A non-aggression pact stands between your house and the player's, sworn at a parley (30 days remain)."
         };

         string prompt = Build(LeanPromptLevel.Full, accords);

         prompt.Should().Contain("tribute of 300 denars");
         prompt.Should().Contain("non-aggression pact");
      }

      // Budget guard: the section is Full-only, so a Lean prompt must not carry it. The Lean prompt has almost no
      // headroom left (LeanPromptPolicyTests pins the hard byte budget), and a stray accord section would blow it.
      [Test]
      public void GIVEN_a_lean_prompt_with_a_standing_accord_THEN_the_section_is_absent()
      {
         Build(LeanPromptLevel.Lean, new List<string> {"You owe the player a tribute of 300 denars a day, sworn at a parley (12 days remain)."})
            .Should().NotContain("STANDING ACCORDS WITH THE PLAYER");
      }

      // No accord, no section: an NPC with nothing standing must not get an empty "STANDING ACCORDS" heading that
      // reads as if something were agreed and then omitted. Null and empty must both degrade the same way.
      [Test]
      public void GIVEN_a_full_prompt_with_null_accords_THEN_the_section_is_absent()
      {
         Build(LeanPromptLevel.Full, null).Should().NotContain("STANDING ACCORDS WITH THE PLAYER");
      }

      [Test]
      public void GIVEN_a_full_prompt_with_an_empty_accord_list_THEN_the_section_is_absent()
      {
         Build(LeanPromptLevel.Full, new List<string>()).Should().NotContain("STANDING ACCORDS WITH THE PLAYER");
      }
   }
}

// Code written by Gabriel Mailhot, 14/10/2026.
// What this pins: a lord who holds an aim of his own is told what he wants, how he goes about it, and whether he may
// bring it up himself in this conversation.
//
// tashmetu (BC-CR bridge author, Nexus 14/10/2026), from two of his testers: "in Calradia, nobody takes the first step
// with the player." His proposal gave a lord "WHAT YOU WANT (your own aim, not a duty)", "you need not wait to be
// asked", and in compact mode "one line". Gabriel (14/10/2026): the other mod says what, Calradia Remembers says when.
// The host decides the leave once per conversation, so the section must say plainly both sides of it. If these fail, a
// lord with an aim never takes the first step, or one who already raised it is invited to raise it again, which is the
// repetition tashmetu feared ("a lord who brings up Lakonon in every conversation").

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class OwnAimPromptTests
   {
      private const string Want = "Lakonon. By right it is the Osticos', but your house has been quietly building a claim to it, and you mean to have it.";
      private const string Part = "Herra Ubbe's men can take what a claim alone cannot.";

      private static string Prompt(EncounterContext context)
         => LeanRealPromptBudgetTests.RealBuilder().BuildSystemPrompt(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, context);

      private static EncounterContext Lord(bool mayRaise, LeanPromptLevel lean = LeanPromptLevel.Full) => new() {
         OwnAimWant = Want, OwnAimPlayerPart = Part, OwnAimMayRaise = mayRaise, LeanLevel = lean,
         OwnAimApproach = "You go about it with care. You never tell them a falsehood."
      };

      // The testers' complaint: a lord with an aim and the leave takes the first step.
      [Test]
      public void GIVEN_a_lord_with_an_aim_and_the_leave_WHEN_his_prompt_is_built_THEN_he_knows_what_he_wants_and_may_raise_it_himself()
      {
         string p = Prompt(Lord(mayRaise: true));

         p.Should().Contain("WHAT YOU WANT (your own aim, not a duty):");
         p.Should().Contain(Want);
         p.Should().Contain(Part);
         p.Should().Contain("You never tell them a falsehood.");
         p.Should().Contain("you may bring it up yourself");
      }

      // tashmetu's risk: once raised, the lord still wants Lakonon but waits to be asked.
      [Test]
      public void GIVEN_a_lord_who_already_raised_his_aim_WHEN_built_THEN_he_still_wants_it_but_does_not_bring_it_up()
      {
         string p = Prompt(Lord(mayRaise: false));

         p.Should().Contain(Want);
         p.Should().Contain("Do not bring it up yourself");
         p.Should().NotContain("you may bring it up yourself");
      }

      // The leave is refused for reasons with no history behind them too (an enemy, a captivity, a letters-only aim).
      // Telling the lord he "already spoke of it" would have him invent a conversation that never took place.
      [Test]
      public void GIVEN_the_leave_refused_WHEN_built_THEN_the_lord_is_told_of_no_earlier_conversation()
      {
         string p = Prompt(Lord(mayRaise: false));

         p.Should().NotContain("already spoken of it");
         p.Should().NotContain("spoke of it");
      }

      // tashmetu: "In compact mode it would be one line." A small model's context has no room for the full block.
      [Test]
      public void GIVEN_compact_mode_WHEN_built_THEN_the_aim_is_one_line()
      {
         string p = Prompt(Lord(mayRaise: true, LeanPromptLevel.Lean));

         p.Should().Contain($"YOUR OWN AIM: {Want} {Part} You may bring it up once, when it fits.");
         // The full block's approach sentence is dropped here, so its two guards travel in a few words: NPCs never lie
         // (Gabriel, 20/09/2026) and gating before declaration (09/09/2026). A small model needs them more, not less.
         p.Should().Contain("Never a falsehood, never a promise the world has not made true.");
         p.Should().NotContain("WHAT YOU WANT (your own aim, not a duty):");
      }

      // Most lords hold no aim (tashmetu: "most get nothing"); their prompt is exactly what it was.
      [Test]
      public void GIVEN_a_character_with_no_aim_WHEN_built_THEN_there_is_no_aim_section()
      {
         string p = Prompt(new EncounterContext { OwnAimMayRaise = true });

         p.Should().NotContain("WHAT YOU WANT (your own aim");
         p.Should().NotContain("YOUR OWN AIM:");
      }

      // The leave is fixed for the conversation, so the section belongs to the cached prefix, above the marker that
      // starts the per-turn tail; below it, it would be re-sent uncached on every reply for nothing.
      [Test]
      public void GIVEN_a_lord_with_an_aim_WHEN_built_THEN_the_aim_sits_in_the_cached_prefix()
      {
         string p = Prompt(Lord(mayRaise: true));

         p.IndexOf("WHAT YOU WANT (your own aim", System.StringComparison.Ordinal)
            .Should().BeLessThan(p.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal));
      }
   }
}

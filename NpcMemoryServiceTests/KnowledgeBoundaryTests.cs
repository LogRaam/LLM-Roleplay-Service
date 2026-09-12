// Code written by Gabriel Mailhot, 10/09/2026.
// What this pins: that a character is told what they do not know and cannot do, and, just as importantly, that
// being told does not turn them into a clerk.
//
// Why it exists. Two player reports on the same day, from opposite ends of one hole.
//
// plagueAdvocate (Nexus): "characters don't really know what happens in game a lot of the time. I asked about a
// recent battle and they have the enemy numbers and type completely wrong. I also ask them about some goods
// acquisition, as in where we purchased them from and they made up a whole new story about us raiding caravans
// (we never did). They also don't seem to know what we have in the inventory."
//
// And, about deeds rather than facts: "It feels like the AI using this like never follows me despite it agreeing
// to :( or send troops to X location.. it never does."
//
// fkasad replied in the thread with the fatalist reading: "Telling stories is the reason why LLMs exist. They
// can't help telling." That is half right, and the wrong half to build on. A model left in silence invents; a
// model TOLD what it does not know says it does not know. Silence is not read as "forbidden" or as "unknown", it
// is read as "nobody mentioned it", which is the same lesson as every other fix this week.
//
// "Send troops to X" is the case no gate could ever have caught: it is not a misused verb, it is an INVENTED
// CAPABILITY, and there is nothing to refuse because there is nothing there.
//
// THE TEST THAT MATTERS MOST IS THE ONE ABOUT WONDERING. A boundary written carelessly produces characters who
// answer "I do not have that information", which would cost more than the confabulation it prevents. The line
// belongs between ASSERTING and WONDERING, not between speaking and silence.
//
// If these tests fail, characters are either about to invent the world again, or about to stop talking.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class KnowledgeBoundaryTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"
      };

      private static string Build(LeanPromptLevel lean)
         => new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
                                                  new EncounterContext {LeanLevel = lean});

      // The invented capability, which is the half no gate could ever have caught: there is no verb for sending
      // troops to a place, so nothing could refuse it, and the character agreed to it happily.
      [Test]
      public void GIVEN_any_conversation_WHEN_built_THEN_the_character_is_told_they_cannot_move_troops_or_parties()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("cannot send troops to a place")
               .And.Contain("do not agree to do it");
      }

      // The invented facts, each named, because a general "do not make things up" is already implicit in every
      // prompt ever written and plainly does not work.
      [Test]
      public void GIVEN_any_conversation_WHEN_built_THEN_the_things_characters_actually_invent_are_named()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("carrying in their baggage")   // the inventory question
               .And.Contain("where any of it was bought")        // the invented caravan raid
               .And.Contain("a battle you were not present at"); // the wrong enemy numbers
      }

      // The positive instruction, without which the prohibition has nowhere to go. Saying you do not know has to
      // be offered as a GOOD answer, or the model treats it as a failure state and invents to avoid it.
      [Test]
      public void GIVEN_the_boundary_WHEN_built_THEN_not_knowing_is_offered_as_a_good_answer_in_character()
      {
         Build(LeanPromptLevel.Full)
            .Should().Contain("SAY SO, IN YOUR OWN VOICE")
            .And.Contain("How would I know that?");
      }

      // THE ONE THAT MATTERS MOST. The whole value of this mod is characters who talk, and a boundary that
      // silenced speculation would cost far more than the invention it prevents. Wondering aloud, guessing and
      // repeating rumour stay available, provided they are named as such.
      [Test]
      public void GIVEN_the_boundary_WHEN_built_THEN_wondering_guessing_and_rumour_are_still_allowed()
      {
         string prompt = Build(LeanPromptLevel.Full);

         prompt.Should().Contain("None of this makes you a clerk")
               .And.Contain("wonder aloud, guess, repeat what you have heard")
               .And.Contain("A rumour told as a rumour");
      }

      // And the line is drawn on the confident claim, not on speaking at all. Stated explicitly because it is
      // the distinction the whole block rests on.
      [Test]
      public void GIVEN_the_boundary_WHEN_built_THEN_only_the_confident_claim_is_forbidden()
      {
         Build(LeanPromptLevel.Full).Should().Contain("Only");
         Build(LeanPromptLevel.Full).Should().Contain("the confident claim about something you were never told is forbidden");
      }

      // A weaker model is the likeliest to invent, so the guard must reach it too. Cutting this for budget would
      // remove it precisely where it does the most work, which is the mistake the intimacy bar made in July.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_a_small_model_is_still_told_the_boundary()
      {
         string lean = Build(LeanPromptLevel.Lean);

         lean.Should().Contain("WHAT YOU DO NOT KNOW")
             .And.Contain("baggage")
             .And.Contain("Promise no deed not listed above");
      }

      // But it must be compact there, or it eats the budget the Lean prompt exists to protect.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_boundary_is_the_short_form()
      {
         KnowledgeBoundaryPolicy.Text(LeanPromptLevel.Lean).Length
                                .Should().BeLessThan(KnowledgeBoundaryPolicy.Text(LeanPromptLevel.Full).Length / 2);
      }

      // Even the short form must keep the permission, or a small model becomes the clerk while the large one
      // stays a character.
      [Test]
      public void GIVEN_the_short_form_WHEN_read_THEN_it_still_permits_wondering_and_rumour()
      {
         KnowledgeBoundaryPolicy.Text(LeanPromptLevel.Lean)
                                .Should().Contain("wonder aloud and name it a guess");
      }

      // The boundary must not deny what the prompt elsewhere SUPPLIES, or it becomes the very incoherence it
      // exists to end. The player's visible arms, their party's size and where they both stand are all fed.
      [Test]
      public void GIVEN_the_boundary_WHEN_read_THEN_it_never_denies_a_fact_the_prompt_itself_provides()
      {
         foreach (LeanPromptLevel lean in new[] {LeanPromptLevel.Full, LeanPromptLevel.Lean})
         {
            string text = KnowledgeBoundaryPolicy.Text(lean);

            // it denies the BAGGAGE, never what is worn and plainly visible
            text.Should().NotContain("what they are wearing").And.NotContain("what arms they carry");
            // it denies battles they were ABSENT from, never battle as such
            text.Should().NotContain("any battle");
            // it denies where they have BEEN, never where the two of them are standing now
            text.Should().NotContain("where you are").And.NotContain("where they are now");
         }
      }

      // ── the boundary runs both ways (Gabriel, 2026-09-12) ────────────────

      // His observation, and it is the half the boundary had never covered. A character invents a blight in his
      // own fields; the player repeats it to somebody else; she has never heard of it - correctly, since
      // knowledge here is what you were told - and DENIES it. Not knowing is right. Denying is the incoherence
      // the player actually feels, because she cannot know it is false either.
      [Test]
      public void GIVEN_news_the_player_brings_WHEN_it_has_not_reached_him_THEN_he_may_not_call_it_untrue()
      {
         foreach (LeanPromptLevel lean in new[] {LeanPromptLevel.Full, LeanPromptLevel.Lean})
         {
            string text = KnowledgeBoundaryPolicy.Text(lean);

            // Both forms must offer the true answer - it has not reached me - and neither may leave denial
            // available as the easy one.
            text.Should().Contain("not reached");
            text.ToLowerInvariant().Should().Contain("not false");
         }
      }

      // And the Full form has to say what to do INSTEAD, or the rule is obeyed by going quiet - the same lesson
      // the seat packs learned when "never give a figure" needed "send for the steward's books" beside it.
      [Test]
      public void GIVEN_the_full_boundary_WHEN_read_THEN_it_offers_asking_who_told_them_rather_than_only_a_ban()
      {
         string text = KnowledgeBoundaryPolicy.Text(LeanPromptLevel.Full);

         text.Should().Contain("ask who told them");
         text.ToLowerInvariant().Should().Contain("do not deny it");
      }

      // The small model needs this MORE than the large one, for the same reason the rest of the boundary is
      // kept in Compact: a weaker model is the likeliest both to invent and to flatly contradict.
      [Test]
      public void GIVEN_the_compact_boundary_WHEN_read_THEN_the_rule_survives_the_trim()
      {
         string compact = KnowledgeBoundaryPolicy.Text(LeanPromptLevel.Lean);

         compact.Should().Contain("not reached").And.Contain("is not false");
      }

      // It is a block, not a sentence buried in another one, so it can be found in a log and read by a person
      // debugging a conversation.
      [Test]
      public void GIVEN_either_form_WHEN_read_THEN_it_announces_itself_with_a_heading()
      {
         foreach (LeanPromptLevel lean in new[] {LeanPromptLevel.Full, LeanPromptLevel.Lean})
            KnowledgeBoundaryPolicy.Text(lean).Should().StartWith("WHAT YOU DO NOT KNOW");
      }
   }
}

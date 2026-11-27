// Code written by Gabriel Mailhot, 26/11/2026.
// FloorJudge: the prompt the group-conversation judge reads and the verdict it gives. Gabriel (26/11/2026): a cheap fast
// model judges each listener after every line, never writes dialogue, and its history should cost little through the
// prompt cache. If these are wrong, the cache is broken every turn (the history is rewritten instead of appended), a
// verdict is misread (a provoked listener read as silent), or the judge's words leak in as a spoken line.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class FloorJudgeTests
   {
      private static readonly FloorJudgeListener Ira = new() {Name = "Ira", Persona = "A blunt mercenary captain.", Ties = new List<string> {"Garios: a lord who looks down on you"}};

      // STAKE: Gabriel's cache point: the scene for one call must be the start of the next, or each turn pays in full.
      [Test]
      public void GIVEN_the_scene_grows_by_one_line_WHEN_both_turns_are_built_THEN_the_earlier_prompt_is_the_start_of_the_later()
      {
         var scene = new List<FloorJudgeLine> {new() {Speaker = "Arwa", To = "Garios", Text = "What news from the west?"}};
         string first = FloorJudge.TurnPrompt("Ira", scene);
         scene.Add(new FloorJudgeLine {Speaker = "Garios", To = "Arwa", Text = "Little worth telling."});
         string second = FloorJudge.TurnPrompt("Ira", scene);

         // Every line of the earlier scene (up to the blank line before the question) opens the later prompt: the provider's
         // prefix cache serves all of it, and only the new line and the question are paid in full.
         const string lastLineOfFirst = "What news from the west?";
         string sharedScene = first.Substring(0, first.IndexOf(lastLineOfFirst, System.StringComparison.Ordinal) + lastLineOfFirst.Length);
         second.Should().StartWith(sharedScene);
      }

      // STAKE: the listener's sheet sits in the stable prompt, the same on every call; the scene never does.
      [Test]
      public void GIVEN_a_listener_WHEN_the_stable_prompt_is_built_THEN_it_holds_the_sheet_and_no_scene()
      {
         string stable = FloorJudge.StablePrompt(Ira);

         stable.Should().Contain("THE LISTENER: Ira").And.Contain("Garios: a lord who looks down on you");
         stable.Should().NotContain("THE SCENE SO FAR");
         FloorJudge.StablePrompt(Ira).Should().Be(stable);
      }

      // STAKE: a provoked listener's "urgent" must be read as urgent, whatever the model's casing or decoration.
      [Test]
      public void GIVEN_a_decorated_reply_WHEN_parsed_THEN_intent_and_reaction_are_read()
      {
         FloorJudgeVerdict v = FloorJudge.Parse("- Intent: URGENT\n- Reaction: *her jaw tightens*");

         v.Parsed.Should().BeTrue();
         v.Intent.Should().Be(JudgedIntent.Urgent);
         v.Reaction.Should().Be("*her jaw tightens*");
      }

      // STAKE: "none" is no reaction; an unreadable reply is treated as nothing to say, never as a wish to speak.
      [Test]
      public void GIVEN_no_reaction_or_an_unreadable_reply_WHEN_parsed_THEN_nothing_is_invented()
      {
         FloorJudge.Parse("intent: low\nreaction: none").Reaction.Should().BeNull();

         FloorJudgeVerdict garbage = FloorJudge.Parse("I think she would probably speak up here.");
         garbage.Parsed.Should().BeFalse();
         garbage.Intent.Should().Be(JudgedIntent.None);
      }

      // STAKE: a reaction is a gesture; a rambling one is cut at a word so it never becomes a speech.
      [Test]
      public void GIVEN_a_long_reaction_WHEN_parsed_THEN_it_is_cut_at_a_word()
      {
         string longOne = "*" + string.Join(" ", System.Linq.Enumerable.Repeat("glances", 60)) + "*";

         FloorJudge.Parse("intent: none\nreaction: " + longOne).Reaction!.Length.Should().BeLessThanOrEqualTo(FloorJudge.MaxReactionChars + 1);
      }
   }
}

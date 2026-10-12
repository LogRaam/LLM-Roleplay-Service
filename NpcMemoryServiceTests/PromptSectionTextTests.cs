// Code written by Gabriel Mailhot, 12/10/2026.
// The Prompt Composer, increment 3 (docs/design/prompt-composer/DESIGN.md in the mod repository; Gabriel, 12/10/2026:
// "go"). A prompt profile may reword a section: its file REPLACES the stock text, "!append" adds to it, "!remove" drops
// lines. If this is wrong: a reworded intimacy section is written in a game whose adult content is off (the stock gate was
// shut), the parser's contract or the consent rules are reworded away, a required section (who is who) is emptied, or a
// {{char}} in a player's text reaches the model as the literal braces.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PromptSectionTextTests
   {
      private static readonly IReadOnlyDictionary<string, string> Vars = new Dictionary<string, string> {["char"] = "Derthert", ["user"] = "Arwa"};

      private static string Apply(string stock, PromptSectionText text, PromptSectionLock @lock = PromptSectionLock.None, List<string> problems = null)
         => PromptSectionTextApplier.Apply("dialogue_style", stock, text, @lock, Vars, problems);

      // The whole point: the player's words are written where CR's were, variables filled in.
      [Test]
      public void GIVEN_a_reworded_section_WHEN_applied_THEN_its_text_replaces_the_stock_one_with_variables_filled()
      {
         Apply("STOCK LINE\n\n", new PromptSectionText {Text = "Speak tersely, {{char}}."})
            .Should().Be("Speak tersely, Derthert.\n\n");
      }

      // "!append": the stock text stays and the player's follows it.
      [Test]
      public void GIVEN_an_appended_text_WHEN_applied_THEN_it_follows_the_stock_text()
      {
         Apply("STOCK LINE\n\n", new PromptSectionText {Text = "And never rhyme.", Append = true})
            .Should().Be("STOCK LINE\nAnd never rhyme.\n\n");
      }

      // "!remove": the lines holding the text go, the rest of the stock section stays.
      [Test]
      public void GIVEN_lines_to_remove_WHEN_applied_THEN_only_those_lines_go()
      {
         Apply("KEEP ONE\nDROP this one\nKEEP TWO\n\n", new PromptSectionText {RemoveLines = new[] {"DROP"}})
            .Should().Be("KEEP ONE\nKEEP TWO\n\n");
      }

      // The gate comes first: a section whose gate is shut writes nothing, reworded or not (adult content off stays off).
      [Test]
      public void GIVEN_a_section_whose_gate_is_shut_WHEN_a_rewording_is_applied_THEN_nothing_is_written()
      {
         Apply("", new PromptSectionText {Text = "Explicit wording."}).Should().BeEmpty();
         Apply("", new PromptSectionText {Text = "More.", Append = true}).Should().BeEmpty();
      }

      // The parser's contract and the consent rules are sealed: no profile rewords them.
      [Test]
      public void GIVEN_a_sealed_section_WHEN_a_rewording_is_applied_THEN_the_stock_text_stays_and_the_profile_is_told()
      {
         var problems = new List<string>();

         Apply("CONTRACT\n", new PromptSectionText {Text = "Whatever."}, PromptSectionLock.Sealed, problems).Should().Be("CONTRACT\n");
         problems.Should().ContainSingle(p => p.Contains("sealed"));
      }

      // A required section may be reworded but not emptied: an empty role anchor lets the model speak as the player.
      [Test]
      public void GIVEN_a_required_section_emptied_WHEN_applied_THEN_the_stock_text_stays()
      {
         var problems = new List<string>();

         Apply("WHO IS WHO\n", new PromptSectionText {Text = "   "}, PromptSectionLock.Required, problems).Should().Be("WHO IS WHO\n");
         Apply("WHO IS WHO\n", new PromptSectionText {RemoveLines = new[] {"WHO"}}, PromptSectionLock.Required, problems).Should().Be("WHO IS WHO\n");
         problems.Should().HaveCount(2);
         Apply("WHO IS WHO\n", new PromptSectionText {Text = "You are {{char}}, not {{user}}."}, PromptSectionLock.Required)
            .Should().Be("You are Derthert, not Arwa.\n");
      }

      // A free section emptied is the player's choice: it is written as nothing.
      [Test]
      public void GIVEN_a_free_section_emptied_WHEN_applied_THEN_it_writes_nothing()
      {
         Apply("STOCK\n\n", new PromptSectionText {Text = ""}).Should().BeEmpty();
      }

      // Through the real builder: a profile's text reaches the prompt in place of the stock section.
      [Test]
      public void GIVEN_a_context_carrying_a_reworded_section_WHEN_the_prompt_is_built_THEN_the_new_text_is_in_it()
      {
         var builder = new PromptBuilder();
         var npc = new NpcMemoryService.Core.Models.NpcProfile {Id = "lord_x", Name = "Derthert", Faction = "", Clan = ""};
         var world = new NpcMemoryService.Core.Models.WorldState {CurrentDay = 1};
         string stock = builder.BuildSystemPrompt(npc, world, new NpcMemoryService.Core.Models.EncounterContext());

         string custom = builder.BuildSystemPrompt(npc, world, new NpcMemoryService.Core.Models.EncounterContext {
            PromptTexts = new Dictionary<string, PromptSectionText> {["identity"] = new() {Text = "PROFILE IDENTITY TEXT"}}
         });

         custom.Should().Contain("PROFILE IDENTITY TEXT");
         stock.Should().NotContain("PROFILE IDENTITY TEXT");
      }
   }
}

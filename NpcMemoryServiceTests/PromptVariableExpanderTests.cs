// Code written by Gabriel Mailhot, 30/07/2026.
// PromptVariableExpander is the one place that decides how a player-authored {{token}} resolves inside a
// narrative style, a Custom/ override, or world.txt/player_description.txt/post_history_instructions.txt.
// It must run at PromptBuilder BUILD time (per NPC, per turn), never at file-read/cache time, so these
// tests pin the pure substitution contract in isolation: known tokens (any case, any inner spacing) resolve,
// an unknown token is left untouched rather than silently blanked (a player typo or a not-yet-supported
// macro must stay visible), a known-but-empty value resolves to "" rather than the literal token, and a
// missing text/variables input never throws.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PromptVariableExpanderTests
   {
      private static Dictionary<string, string> Vars(params (string Key, string Value)[] entries)
      {
         var vars = new Dictionary<string, string>();
         foreach ((string key, string value) in entries) vars[key] = value;

         return vars;
      }

      // STAKE: this is the whole feature. A player writes {{char}} in a custom style expecting the NPC's
      // own name, in any case (SillyTavern users type {{char}}, {{Char}}, {{CHAR}} interchangeably): if the
      // lookup were case-sensitive, only the exact-case spelling would ever resolve, silently breaking the
      // other two for no reason a player could see.
      [Test]
      public void GIVEN_a_known_token_WHEN_expanding_THEN_it_resolves_regardless_of_case()
      {
         Dictionary<string, string> vars = Vars(("char", "Derthert"));

         PromptVariableExpander.Expand("Hello {{char}}.", vars).Should().Be("Hello Derthert.");
         PromptVariableExpander.Expand("Hello {{Char}}.", vars).Should().Be("Hello Derthert.");
         PromptVariableExpander.Expand("Hello {{CHAR}}.", vars).Should().Be("Hello Derthert.");
      }

      // STAKE: SillyTavern tolerates {{ user }} with inner spaces (a common copy-paste habit from other
      // template systems); a player pasting a style from elsewhere must not get a literal, unresolved brace
      // pair just because they left a space in.
      [Test]
      public void GIVEN_optional_inner_whitespace_WHEN_expanding_THEN_the_token_still_resolves()
      {
         Dictionary<string, string> vars = Vars(("user", "Aldric"));

         PromptVariableExpander.Expand("Greetings, {{ user }}.", vars).Should().Be("Greetings, Aldric.");
      }

      // STAKE: a style file is prose, not a single macro; it will reference {{char}} several times, and a
      // player may write two variables back to back ("{{char}}{{user}}") with no separator. A naive
      // single-match implementation, or one that stops after the first hit, would leave the rest of the
      // document untouched.
      [Test]
      public void GIVEN_multiple_and_adjacent_tokens_WHEN_expanding_THEN_every_occurrence_resolves()
      {
         Dictionary<string, string> vars = Vars(("char", "Derthert"), ("user", "Aldric"));

         PromptVariableExpander.Expand("{{char}} speaks of {{char}}'s own lands to {{user}}.", vars)
            .Should().Be("Derthert speaks of Derthert's own lands to Aldric.");
         PromptVariableExpander.Expand("{{char}}{{user}}", vars).Should().Be("DerthertAldric");
      }

      // STAKE: SillyTavern never blanks out a macro it does not own, so a player's typo ({{chr}}) or a
      // variable this build does not yet support stays visible in the rendered prompt, not silently eaten.
      // Silently dropping unknown tokens would hide the mistake instead of surfacing it.
      [Test]
      public void GIVEN_an_unknown_token_WHEN_expanding_THEN_it_is_left_exactly_as_written()
      {
         Dictionary<string, string> vars = Vars(("char", "Derthert"));

         PromptVariableExpander.Expand("{{char}} met {{unknown}} today.", vars)
            .Should().Be("Derthert met {{unknown}} today.");
      }

      // STAKE: not every player has a spouse, father, or mother known to the encounter. A known token whose
      // resolved value is null or empty must disappear cleanly ("your husband is away" reads fine with
      // spouse == ""), never show the player the raw {{spouse}} placeholder as if the substitution failed.
      [Test]
      public void GIVEN_a_known_token_with_a_null_or_empty_value_WHEN_expanding_THEN_it_resolves_to_empty_string()
      {
         var varsWithNull = new Dictionary<string, string> {["spouse"] = null!};
         var varsWithEmpty = new Dictionary<string, string> {["spouse"] = ""};

         PromptVariableExpander.Expand("Your spouse: {{spouse}}.", varsWithNull).Should().Be("Your spouse: .");
         PromptVariableExpander.Expand("Your spouse: {{spouse}}.", varsWithEmpty).Should().Be("Your spouse: .");
      }

      // STAKE: PromptBuilder calls Expand on every player-authored section on every single prompt build, most
      // of which have nothing to substitute (no active style, an empty post_history_instructions.txt). This
      // must be a safe no-op, never a NullReferenceException that would take down an otherwise-working chat.
      [Test]
      public void GIVEN_null_or_empty_text_or_variables_WHEN_expanding_THEN_the_input_is_returned_unchanged_and_nothing_throws()
      {
         Dictionary<string, string> vars = Vars(("char", "Derthert"));

         PromptVariableExpander.Expand(null!, vars).Should().BeNull();
         PromptVariableExpander.Expand("", vars).Should().Be("");
         PromptVariableExpander.Expand("Hello {{char}}.", null!).Should().Be("Hello {{char}}.");
         PromptVariableExpander.Expand("Hello {{char}}.", new Dictionary<string, string>()).Should().Be("Hello {{char}}.");
      }
   }
}

// Code written by Gabriel Mailhot, 12/10/2026.
// The Prompt Composer, increment 2 (docs/design/prompt-composer/DESIGN.md in the mod repository; Gabriel, 11/10/2026:
// "fait les choix logiques pour laisser le plus de liberte"). A player or a mod chooses the ORDER of the character
// prompt's sections, and which are off, in a layout; this resolver turns a layout into the sections written, in order.
// If it is wrong: a sealed section (the parser's contract, the consent rules) is lost or moved and the game stops
// understanding replies or protecting the player; a required one (who is who, the anti-confabulation wall) is turned
// off; a layout written for an older CR silently drops every section added since; or a typo erases a section with no
// word said. Everything else is the player's to arrange, with a warning where it costs cache.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PromptLayoutResolverTests
   {
      private static List<string> Keys(IEnumerable<PromptSectionSpec> specs) => specs.Select(s => s.Key).ToList();

      private static List<string> Stock => Keys(PromptBuilder.CharacterSections);

      private static List<string> StockOf(PromptZone zone)
         => PromptBuilder.CharacterSections.Where(s => s.Zone == zone).Select(s => s.Key).ToList();

      private static List<string> Resolve(PromptLayout layout, List<string> problems = null)
         => Keys(PromptLayoutResolver.Order(PromptBuilder.CharacterSections, layout, problems));

      // No layout chosen is the stock prompt, exactly: the golden prompts hold the text, this holds the order.
      [Test]
      public void GIVEN_no_layout_WHEN_the_order_is_resolved_THEN_it_is_the_stock_order()
      {
         Resolve(null).Should().Equal(Stock);
      }

      // The whole point ("comme l'overclocking"): a player moves a section and it is written where they put it.
      [Test]
      public void GIVEN_a_layout_that_moves_a_section_WHEN_resolved_THEN_it_is_written_where_the_layout_puts_it()
      {
         List<string> prefix = StockOf(PromptZone.Prefix);
         prefix.Remove("dialogue_style");
         prefix.Insert(prefix.IndexOf("world_description") + 1, "dialogue_style");

         List<string> order = Resolve(new PromptLayout {Prefix = prefix, Tail = StockOf(PromptZone.Tail)});

         order.IndexOf("dialogue_style").Should().Be(order.IndexOf("world_description") + 1);
      }

      // A free section can be turned off: the player's prompt, the player's choice.
      [Test]
      public void GIVEN_a_free_section_turned_off_WHEN_resolved_THEN_it_is_not_written()
      {
         Resolve(new PromptLayout {Prefix = StockOf(PromptZone.Prefix), Tail = StockOf(PromptZone.Tail), Off = new[] {"prose_craft"}})
            .Should().NotContain("prose_craft");
      }

      // The parser's contract: off, the game stops understanding replies. It stays, and the player is told why.
      [Test]
      public void GIVEN_a_sealed_section_turned_off_WHEN_resolved_THEN_it_stays_and_the_layout_is_told()
      {
         var problems = new List<string>();

         Resolve(new PromptLayout {Prefix = StockOf(PromptZone.Prefix), Tail = StockOf(PromptZone.Tail), Off = new[] {"format_instructions"}}, problems)
            .Should().Contain("format_instructions");
         problems.Should().Contain(p => p.Contains("format_instructions") && p.Contains("cannot be turned off"));
      }

      // Sealed means pinned too: the consent rules are not the player's to move.
      [Test]
      public void GIVEN_a_sealed_section_moved_WHEN_resolved_THEN_it_keeps_its_stock_place()
      {
         List<string> prefix = StockOf(PromptZone.Prefix);
         prefix.Remove("format_instructions");
         prefix.Add("format_instructions");

         List<string> order = Resolve(new PromptLayout {Prefix = prefix, Tail = StockOf(PromptZone.Tail)});

         order.IndexOf("format_instructions").Should().Be(Stock.IndexOf("format_instructions"));
      }

      // Who is who: without the role anchor the model speaks as the player. Required: never off.
      [Test]
      public void GIVEN_a_required_section_turned_off_WHEN_resolved_THEN_it_stays()
      {
         Resolve(new PromptLayout {Prefix = StockOf(PromptZone.Prefix), Tail = StockOf(PromptZone.Tail), Off = new[] {"role_anchor"}})
            .Should().Contain("role_anchor");
      }

      // A required section may move, but only within its zone: the anti-confabulation wall above the marker would be
      // cached and far from the reply. Put there, it goes back to its stock place, with a word.
      [Test]
      public void GIVEN_a_required_section_put_in_the_other_zone_WHEN_resolved_THEN_it_goes_back_to_its_stock_place()
      {
         var problems = new List<string>();
         List<string> prefix = StockOf(PromptZone.Prefix);
         prefix.Add("stay_within_what_you_know");
         List<string> tail = StockOf(PromptZone.Tail);
         tail.Remove("stay_within_what_you_know");

         List<string> order = Resolve(new PromptLayout {Prefix = prefix, Tail = tail}, problems);

         order.IndexOf("stay_within_what_you_know").Should().BeGreaterThan(order.IndexOf("encounter_context"));
         problems.Should().Contain(p => p.Contains("stay_within_what_you_know"));
      }

      // A layout made for an older CR does not know the sections added since: they keep their stock place rather than
      // vanish. Turning a section off is always said in so many words.
      [Test]
      public void GIVEN_a_layout_that_does_not_list_a_section_WHEN_resolved_THEN_it_keeps_its_stock_place_with_a_notice()
      {
         var problems = new List<string>();
         List<string> tail = StockOf(PromptZone.Tail);
         tail.Remove("heard_now");

         List<string> order = Resolve(new PromptLayout {Prefix = StockOf(PromptZone.Prefix), Tail = tail}, problems);

         order.Should().Equal(Stock);
         problems.Should().Contain(p => p.Contains("heard_now") && p.Contains("not in the layout"));
      }

      // A typo must not erase anything in silence: an unknown key is reported and ignored.
      [Test]
      public void GIVEN_a_layout_naming_a_section_that_does_not_exist_WHEN_resolved_THEN_it_is_reported_and_ignored()
      {
         var problems = new List<string>();
         List<string> prefix = StockOf(PromptZone.Prefix);
         prefix.Add("dialog_style");

         Resolve(new PromptLayout {Prefix = prefix, Tail = StockOf(PromptZone.Tail)}, problems).Should().Equal(Stock);
         problems.Should().Contain(p => p.Contains("dialog_style") && p.Contains("unknown"));
      }

      // DESIGN.md: a per-turn section above the marker breaks the cache on every reply. Allowed (the player's money),
      // honoured, and warned about.
      [Test]
      public void GIVEN_a_tail_section_put_in_the_prefix_WHEN_resolved_THEN_it_is_honoured_and_warned_about()
      {
         var problems = new List<string>();
         List<string> prefix = StockOf(PromptZone.Prefix);
         prefix.Add("day_mood");
         List<string> tail = StockOf(PromptZone.Tail);
         tail.Remove("day_mood");

         List<string> order = Resolve(new PromptLayout {Prefix = prefix, Tail = tail}, problems);

         order.IndexOf("day_mood").Should().BeLessThan(order.IndexOf("encounter_context"));
         problems.Should().Contain(p => p.Contains("day_mood") && p.Contains("cache"));
      }

      // The encounter context is the cache boundary itself: always between the two zones, whatever the layout says.
      [Test]
      public void GIVEN_any_layout_WHEN_resolved_THEN_the_encounter_context_sits_between_prefix_and_tail()
      {
         List<string> order = Resolve(new PromptLayout {Prefix = new[] {"identity"}, Tail = new[] {"encounter_context", "role_anchor"}});

         order.Count(k => k == "encounter_context").Should().Be(1);
         order.IndexOf("encounter_context").Should().BeGreaterThan(order.IndexOf("identity"));
         order.IndexOf("encounter_context").Should().BeLessThan(order.IndexOf("role_anchor"));
      }
   }
}

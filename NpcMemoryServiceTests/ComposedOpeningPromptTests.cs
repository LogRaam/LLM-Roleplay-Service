// Code written by Gabriel Mailhot, 28/11/2026.
// The composed intro (Stage 3 of the group-scene plan; Gabriel, 28/11/2026: "au début de chaque CHAT on ajoute l'intro").
// A composer opens the conversation from true facts and leaves the character a concern for today and a tension with the
// player, kept hidden in its prompt for that one conversation.
//
// These tests pin the hidden section. If they are wrong: a conversation that was not composed sees its prompt move (and the
// golden prompts with it), the character recites its concern as news, or the section lands below the cache marker and
// costs the cache on every turn.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ComposedOpeningPromptTests
   {
      private const string Player = "Arwa";

      private static NpcProfile Npc() => new() {Id = "lord_test", Name = "Alympia", Faction = "Empire", Clan = "Pethros"};

      private static string Prompt(EncounterContext ctx, LeanPromptLevel lean = LeanPromptLevel.Full, bool enabled = true)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature, PlayerName = Player, EnableComposedOpening = enabled}
           .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 50}, ctx);

      // STAKE: the default must not move. A conversation that was not composed, or a composition that left nothing, writes
      // the very same prompt.
      [Test]
      public void GIVEN_no_composed_opening_or_an_empty_one_WHEN_the_prompt_is_built_THEN_it_is_the_same_as_without()
      {
         string bare = Prompt(new EncounterContext());

         Prompt(new EncounterContext {ComposedOpening = new ComposedOpening()}).Should().Be(bare);
         Prompt(new EncounterContext {ComposedOpening = new ComposedOpening {Concern = "  ", Tension = ""}}).Should().Be(bare);
         // A host that never turned the section on (the SDK default) shows nothing even when handed one.
         Prompt(new EncounterContext {ComposedOpening = new ComposedOpening {Concern = "The harvest failed."}}, enabled: false).Should().Be(bare);
      }

      // STAKE: the character carries the concern and the tension as a mood and an aim, told never to announce them.
      [Test]
      public void GIVEN_a_composed_opening_WHEN_the_prompt_is_built_THEN_the_hidden_section_reads_exactly_so_above_the_marker()
      {
         var ctx = new EncounterContext {
            ComposedOpening = new ComposedOpening {
               Concern = "The wounded of yesterday's battle fill the chapel, and she wants Arwa to stay until they can ride.",
               Tension = "Arwa's men looted the baggage train against her order."
            }
         };

         string prompt = Prompt(ctx).Replace("\r\n", "\n");

         prompt.Should().Contain(
            "WHAT YOU CARRY INTO THIS TALK (yours alone; never say it outright):\n"
            + "- What weighs on you today: The wounded of yesterday's battle fill the chapel, and she wants Arwa to stay until they can ride.\n"
            + "- What could rub between you and Arwa: Arwa's men looted the baggage train against her order.\n"
            + "Let it colour how you speak and what you want from this talk; it is a mood and an aim, not news to announce.\n");
         prompt.IndexOf("WHAT YOU CARRY INTO THIS TALK").Should().BeLessThan(prompt.IndexOf("CURRENT ENCOUNTER"));
      }

      // STAKE: a concern with no tension (or the reverse) still reaches the character, without an empty line.
      [Test]
      public void GIVEN_only_a_concern_WHEN_the_prompt_is_built_THEN_only_its_line_is_written()
      {
         string prompt = Prompt(new EncounterContext {ComposedOpening = new ComposedOpening {Concern = "The harvest failed."}}).Replace("\r\n", "\n");

         prompt.Should().Contain("- What weighs on you today: The harvest failed.\n");
         prompt.Should().NotContain("What could rub between you");
      }

      // STAKE (Lean budgets): the compact prompts carry the section too, and it stays a few lines.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_composed_THEN_the_section_is_there_and_short()
      {
         var opening = new ComposedOpening {Concern = new string('c', 400), Tension = new string('t', 400)};
         string bare = Prompt(new EncounterContext {LeanLevel = LeanPromptLevel.Lean}, LeanPromptLevel.Lean);
         string composed = Prompt(new EncounterContext {LeanLevel = LeanPromptLevel.Lean, ComposedOpening = opening}, LeanPromptLevel.Lean);

         composed.Should().Contain("WHAT YOU CARRY INTO THIS TALK");
         (composed.Length - bare.Length).Should().BeLessThan(1200);
      }
   }
}

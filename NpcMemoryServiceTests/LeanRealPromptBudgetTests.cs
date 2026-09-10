// Code written by Gabriel Mailhot, 10/09/2026.
// What this pins: the size of the prompt the game ACTUALLY SENDS in Compact mode, as opposed to the one the older
// budget test was measuring.
//
// Bug report (DuskSymphony, Nexus tracker, v2.5.7): "Compact Prompt mode is still sending a prompt with context
// over 9k", quoting his own local server refusing the request:
//
//   send_error: task id = 9, error: request (9496 tokens) exceeds the available context size (8192 tokens)
//
// "this is on a fresh install of CR on the first attempt at interacting with an NPC... Compact Prompt mode is
// turned on, and I've restarted just in case."
//
// LeanPromptPolicyTests has guarded that budget for months and has been green throughout. It builds a
// PromptBuilder with NO ActionVocabulary, and AppendActionInstructions returns on its very first line when the
// vocabulary is empty, so the whole GAME ACTIONS section was absent from everything it measured. It was pinning
// 6.9k characters of a document the game has never once sent.
//
// The real vocabulary is 69 actions, and unabridged it is 11,565 characters, roughly 2,900 tokens, in EVERY
// prompt including Lean. Nothing trimmed it because nothing had ever measured it, so Compact mode was never
// compact and an 8k local model could not hold one conversation on a fresh install.
//
// THE LESSON WORTH MORE THAN THE FIX: a guard that constructs its subject differently from production measures
// its own construction. This file builds the vocabulary the way LlmServices does, so the number below is the
// number that goes over the wire.
//
// If these tests fail, a player on a small local model is about to be told their context is too short.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LeanRealPromptBudgetTests
   {
      // The whole shipped catalog, exactly as the host unions it into the builder.
      private static List<GameActionDefinition> RealVocabulary()
         => GameActionCatalog.All
                             .Select(spec => new GameActionDefinition {Type = spec.Type, Description = spec.Description})
                             .ToList();

      private static NpcProfile Npc() => new() {
         Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"
      };

      private static string Build(LeanPromptLevel lean)
         => new PromptBuilder {ActionVocabulary = RealVocabulary()}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = lean});

      // The catalog is real and large. If this ever drops to nothing, every other assertion here becomes
      // meaningless in exactly the way the old budget test was.
      [Test]
      public void GIVEN_the_shipped_catalog_WHEN_measured_THEN_it_is_the_real_one_and_not_an_empty_stand_in()
      {
         RealVocabulary().Count.Should().BeGreaterThan(50);
         Build(LeanPromptLevel.Lean).Should().Contain("GAME ACTIONS:");
      }

      // DuskSymphony's ceiling. 8192 tokens is the common local default and the one he ran into; the system
      // prompt must leave real room for the conversation and the reply, so it is held to roughly half of it.
      // At ~4 characters per token that is 16,000 characters.
      [Test]
      public void GIVEN_compact_mode_WHEN_the_real_prompt_is_built_THEN_it_leaves_room_inside_an_8k_context()
      {
         Build(LeanPromptLevel.Lean).Length.Should().BeLessThan(16000);
      }

      // And the trim is what buys that room. Every action survives; only the prose about it is cut, so a small
      // model can still emit any verb a large one can.
      [Test]
      public void GIVEN_compact_mode_WHEN_built_THEN_every_action_is_still_offered_just_described_briefly()
      {
         string lean = Build(LeanPromptLevel.Lean);
         string full = Build(LeanPromptLevel.Full);

         foreach (GameActionDefinition action in RealVocabulary())
            lean.Should().Contain("- " + action.Type + ":");

         lean.Length.Should().BeLessThan(full.Length);
      }

      // The saving must be substantial or the trim was not worth making. Measured at the time: the action list
      // alone fell from 11,565 characters to roughly a third of that.
      [Test]
      public void GIVEN_compact_mode_WHEN_built_THEN_the_trim_saves_thousands_of_characters()
      {
         (Build(LeanPromptLevel.Full).Length - Build(LeanPromptLevel.Lean).Length)
            .Should().BeGreaterThan(5000);
      }

      // A cut description must still read as a description. A line ending mid-word, or on a dangling opening
      // bracket, reads to anyone debugging a log as a corrupted prompt rather than a deliberate abbreviation.
      [Test]
      public void GIVEN_a_long_description_WHEN_cut_for_compact_mode_THEN_it_never_ends_mid_word_or_mid_bracket()
      {
         foreach (GameActionDefinition action in RealVocabulary())
         {
            string shortened = ActionVocabularyPolicy.Describe(action.Description, LeanPromptLevel.Lean);

            shortened.Should().NotBeNullOrWhiteSpace();
            shortened.Should().NotEndWith("(").And.NotEndWith(",").And.NotEndWith(" ");

            if (shortened.EndsWith("..."))
            {
               shortened.Count(c => c == '(').Should().Be(shortened.Count(c => c == ')'));
               action.Description.Should().StartWith(shortened.Substring(0, shortened.Length - 3).TrimEnd());
            }
         }
      }

      // A Full prompt is unchanged: it keeps every word, because a strong model uses the long descriptions to
      // tell near-neighbour verbs apart and that is what they are for.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_no_description_was_shortened()
      {
         string full = Build(LeanPromptLevel.Full);

         foreach (GameActionDefinition action in RealVocabulary().Take(12))
            full.Should().Contain(action.Description);
      }

      // Short descriptions are left exactly as they are, since abbreviating something already brief only costs
      // an ellipsis.
      [Test]
      public void GIVEN_an_already_short_description_WHEN_cut_THEN_it_is_returned_untouched()
      {
         const string brief = "Ends the conversation now.";

         ActionVocabularyPolicy.Describe(brief, LeanPromptLevel.Lean).Should().Be(brief);
         ActionVocabularyPolicy.Describe(null, LeanPromptLevel.Lean).Should().BeEmpty();
         ActionVocabularyPolicy.Describe("   ", LeanPromptLevel.Lean).Should().BeEmpty();
      }
   }
}

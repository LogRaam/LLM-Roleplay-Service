// Code written by Gabriel Mailhot, 07/09/2026.
// What this pins: how much of a witness's OWN memory survives into the LEAN prompt, the one a small local model
// gets. It feeds the line that lets a bystander voice their reaction true to what they actually recall.
//
// Why it exists. A player reported on 2026-08-23 that a companion forgot an agreement struck one-on-one, and the
// fix gave every witness their own recall line. That line was Full-only. So every player on a small local model,
// which the mod's own setting explicitly tells them to enable, kept the original bug intact, and on 2026-09-07 a
// second player reported the same thing from the other end: "group conversations are quite difficult, one or both
// NPCs may lose the necessary context of the conversation".
//
// The budget is the reason it was dropped in the first place, and it is real: LeanPromptPolicyTests pins the
// always-on Lean contract hard. So the answer here is not to restore the line for everyone, it is to bound it. The
// allowance must not grow with the size of the room, or one crowded tavern undoes the whole point of Lean. If these
// tests fail, either a small model is about to lose the thread of a group scene again, or Lean is about to be
// blown open by a party of eight.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class LeanWitnessRecallTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static WitnessEntry Witness(string name, string memory, bool companion = false) => new() {
         Name = name,
         RelationToNpc = "a bystander",
         IsPlayerCompanion = companion,
         Memory = memory
      };

      private static string Build(LeanPromptLevel level, params WitnessEntry[] witnesses)
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = level, Witnesses = witnesses.ToList()};

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      private static int RecallLines(string prompt) =>
         prompt.Split('\n').Count(l => l.Contains(" remembers: "));

      // The whole point of the repair: on a small model a witness must still know what they know. Before this,
      // Lean printed the name, the role, the persona, the sex and even the gear, and dropped the one fact that
      // carries a conversation forward.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_a_witness_has_a_memory_THEN_it_is_no_longer_dropped_outright()
      {
         string prompt = Build(LeanPromptLevel.Lean, Witness("Alys", "she agreed to speak up on the signal word"));

         prompt.Should().Contain("Alys remembers:");
         prompt.Should().Contain("agreed to speak up");
      }

      // The reason it was ever dropped. An eight-companion party must cost the same as a two-person room, or Lean
      // stops fitting the 4k to 8k context it exists for and the model simply goes silent.
      [Test]
      public void GIVEN_a_crowded_room_WHEN_built_lean_THEN_the_recall_allowance_does_not_grow_with_the_room()
      {
         WitnessEntry[] crowd = Enumerable.Range(1, 10)
            .Select(i => Witness($"Bystander{i}", $"a long standing arrangement number {i} that matters to them"))
            .ToArray();

         RecallLines(Build(LeanPromptLevel.Lean, crowd))
            .Should().Be(LeanPromptPolicy.LeanWitnessMemoryCount);
      }

      // Who gets the two slots is not arbitrary: a COMPANION is the person who holds a deal struck one-on-one,
      // which is precisely what both player reports were about. A passing bystander losing their recall costs the
      // scene far less than the player's own companion losing theirs.
      [Test]
      public void GIVEN_the_allowance_is_scarce_WHEN_a_companion_is_present_THEN_they_are_served_before_a_bystander()
      {
         string prompt = Build(LeanPromptLevel.Lean,
            Witness("Bystander1", "idle talk of the road"),
            Witness("Bystander2", "idle talk of the market"),
            Witness("Alys", "she agreed to speak up on the signal word", companion: true));

         prompt.Should().Contain("Alys remembers:");
      }

      // A recall line is one concrete fact, not a history: an untrimmed memory would spend the whole allowance on
      // one person. The cut lands on a word boundary so the model is not handed a severed word to interpret.
      [Test]
      public void GIVEN_a_long_memory_WHEN_trimmed_for_lean_THEN_it_is_capped_and_cut_on_a_word_boundary()
      {
         string trimmed = LeanPromptPolicy.LeanWitnessMemory(string.Join(" ", Enumerable.Repeat("word", 60)));

         trimmed.Should().NotBeNull();
         trimmed!.Length.Should().BeLessThanOrEqualTo(LeanPromptPolicy.LeanWitnessMemoryChars + 3); // the ellipsis
         trimmed.Should().EndWith("...");
         trimmed.Should().NotContain("wor...");
      }

      // Nothing to say stays nothing to say: an empty memory must not print a bare "remembers:" line, which would
      // read to the model as a person who remembers nothing at all about the player.
      [Test]
      public void GIVEN_a_witness_with_no_memory_WHEN_built_lean_THEN_no_empty_recall_line_is_printed()
      {
         RecallLines(Build(LeanPromptLevel.Lean, Witness("Alys", null), Witness("Bran", "   ")))
            .Should().Be(0);
      }

      // Full is for large-context models and must be untouched by any of this: everyone keeps their memory, whole.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_a_crowd_is_present_THEN_every_witness_keeps_their_whole_memory()
      {
         WitnessEntry[] crowd = Enumerable.Range(1, 5)
            .Select(i => Witness($"Bystander{i}", $"arrangement number {i}"))
            .ToArray();

         string prompt = Build(LeanPromptLevel.Full, crowd);

         RecallLines(prompt).Should().Be(5);
         prompt.Should().Contain("arrangement number 5");
      }

      // The ceiling nobody was guarding. LeanPromptPolicyTests pins the always-on contract for a MINIMAL profile
      // with no room at all, so a crowded Lean prompt was bounded by nothing. This states what a full room actually
      // costs, so the next person to add a per-witness line finds out here rather than from a silent model.
      [Test]
      public void GIVEN_a_full_room_WHEN_built_lean_THEN_the_whole_prompt_still_fits_a_small_context()
      {
         WitnessEntry[] crowd = Enumerable.Range(1, 8)
            .Select(i => Witness($"Bystander{i}", $"a long standing arrangement number {i} that matters a great deal to them"))
            .ToArray();

         Build(LeanPromptLevel.Lean, crowd).Length.Should().BeLessThan(8500);
      }
   }
}

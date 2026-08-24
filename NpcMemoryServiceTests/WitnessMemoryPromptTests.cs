// Code written by Gabriel Mailhot, 23/08/2026.
// Pins that a witness voiced inside the main speaker's single call is given their OWN memory (player report:
// a companion who had agreed to something one-on-one answered, in a group scene, as if she had never heard of
// it). The mod fills WitnessEntry.Memory from the witness's compressed profile; this pins the prompt rendering.

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
   public class WitnessMemoryPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(WitnessEntry w)
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            Witnesses = new List<WitnessEntry> {w}
         };

         return new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // The fix's whole point: the witness's recall reaches the model, so it voices her true to what SHE
      // remembers (a prior agreement), not as a stranger.
      [Test]
      public void GIVEN_a_witness_with_memory_WHEN_building_the_prompt_THEN_her_recall_is_shown()
      {
         var w = new WitnessEntry {
            Name = "Arwa",
            RelationToNpc = "your companion",
            Memory = "Agreed to speak up on the signal word."
         };

         string prompt = Build(w);

         prompt.Should().Contain("Arwa remembers: Agreed to speak up on the signal word.");
      }

      // A synthetic flavour onlooker has no profile and so no memory; the prompt must not mint a "remembers"
      // line for them (it would read as invented history).
      [Test]
      public void GIVEN_a_witness_without_memory_WHEN_building_the_prompt_THEN_no_recall_line_is_shown()
      {
         string prompt = Build(new WitnessEntry {Name = "A guard", RelationToNpc = "one of your men"});

         prompt.Should().NotContain("A guard remembers:");
      }
   }
}

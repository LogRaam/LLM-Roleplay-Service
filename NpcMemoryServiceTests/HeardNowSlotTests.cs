// Code written by Gabriel Mailhot, 10/10/2026.
// philip0105 (Vivid World, 10/10/2026): live knowledge another mod hands in through a {{variable}}, every prompt, never
// stored. post_history_instructions.txt sits after CR's output rules, and a long block there weakens them; heard_now.txt
// is written in the uncached tail just before those rules. If this breaks, the live knowledge is cached and goes stale
// between turns, or it pushes the output rules up and a weaker model stops emitting its blocks.

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
   public class HeardNowSlotTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_heard", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};

      // The slot sits after the cache marker and before WHO IS WHO, with the mod's variable filled in.
      [Test]
      public void GIVEN_heard_now_text_with_a_mod_variable_WHEN_the_prompt_is_built_THEN_it_is_filled_in_the_tail_before_the_output_rules()
      {
         var builder = new PromptBuilder {HeardNowInstructions = "WHAT YOU HAVE HEARD:\n{{vw_heard}}", PostHistoryInstructions = "POST HISTORY LINE"};
         var context = new EncounterContext {
            Scene = SceneType.Settlement,
            ExternalPromptVariables = new Dictionary<string, string> {["vw_heard"] = "The miller's son ran off, said the smith."}
         };

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         int marker = prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal);
         int heard = prompt.IndexOf("The miller's son ran off, said the smith.", System.StringComparison.Ordinal);
         int whoIsWho = prompt.IndexOf("WHO IS WHO:", System.StringComparison.Ordinal);
         int post = prompt.IndexOf("POST HISTORY LINE", System.StringComparison.Ordinal);

         heard.Should().BeGreaterThan(marker);
         heard.Should().BeLessThan(whoIsWho);
         whoIsWho.Should().BeLessThan(post);
      }

      // Nothing to say, nothing written: an empty file, or a mod whose variable is empty for this character.
      [Test]
      public void GIVEN_no_heard_now_text_WHEN_the_prompt_is_built_THEN_nothing_is_added()
      {
         string without = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext());
         string emptyVar = new PromptBuilder {HeardNowInstructions = "{{vw_heard}}"}.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {ExternalPromptVariables = new Dictionary<string, string> {["vw_heard"] = ""}});

         emptyVar.Should().Be(without);
      }
   }
}

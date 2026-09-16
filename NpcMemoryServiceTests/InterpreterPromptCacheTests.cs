// Code written by Gabriel Mailhot, 15/09/2026.
// What these pin: that the interpreter's invariant head is actually CACHEABLE on the wire, not merely
// arranged as though it were.
//
// The interpreter prompt has been built prefix-first since it was written, and the comment on StablePrefix
// said why: so a provider could cache that head across calls. It never was. Nothing passed it as
// LlmRequest.StableSystemPrompt, so the client took its no-split branch and cached the WHOLE prompt -
// which carries the turn's prose and facts, and therefore changes every turn. Zero reuse, on a block
// containing the entire action catalogue, once per turn, on top of the prose call.
//
// Being cacheable and being cached are not the same thing. These assert the wire, not the intention.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class InterpreterPromptCacheTests
   {
      private const string Facts = "YOU: Alympia. PLAYER: Arwa.";
      private const string Prose = "*I nod once.* Take the gold and go.";

      // The ordering the whole scheme rests on. If the head ever stops coming first, no breakpoint can
      // rescue it, because a provider matches a prefix from the very first character.
      [Test]
      public void GIVEN_an_interpreter_prompt_WHEN_built_THEN_it_still_begins_with_the_invariant_head()
      {
         ActionInterpreterPromptBuilder.Build(Prose, Facts)
                                       .Should().StartWith(ActionInterpreterPromptBuilder.StablePrefix);
      }

      // The head must be worth caching AND must not be the whole prompt, or there would be nothing to
      // split and the request would be identical to the un-split one.
      [Test]
      public void GIVEN_an_interpreter_prompt_WHEN_built_THEN_the_head_is_a_real_prefix_with_a_tail_after_it()
      {
         string prompt = ActionInterpreterPromptBuilder.Build(Prose, Facts);

         prompt.Length.Should().BeGreaterThan(ActionInterpreterPromptBuilder.StablePrefix.Length);
         prompt.Should().Contain(Prose, "the per-turn prose belongs after the breakpoint, never inside it");
         ActionInterpreterPromptBuilder.StablePrefix.Should().NotContain(Prose);
         ActionInterpreterPromptBuilder.StablePrefix.Should().NotContain(Facts);
      }

      // THE DEFECT, stated on the wire: a request that declares the prefix is sent as TWO content blocks,
      // the first carrying cache_control. Without the declaration it is one block containing the turn's
      // prose, which cannot be reused and is what the game sent until now.
      [Test]
      public void GIVEN_the_prefix_is_declared_WHEN_sent_THEN_the_breakpoint_falls_after_the_invariant_head()
      {
         JToken system = SystemMessageOf(Request(withPrefix: true));
         JToken[] blocks = system["content"]!.ToArray();

         blocks.Should().HaveCount(2);
         blocks[0]["cache_control"]!["type"]!.Value<string>().Should().Be("ephemeral");
         blocks[0]["text"]!.Value<string>().Should().Be(ActionInterpreterPromptBuilder.StablePrefix);
         blocks[1]["text"]!.Value<string>().Should().Contain(Prose);
         blocks[1]["cache_control"].Should().BeNull("only the invariant head is worth a breakpoint");
      }

      // The control: without the declaration the whole prompt - prose included - is the cached block, so
      // the next turn cannot read a character of it. This is what the test above is worth.
      [Test]
      public void GIVEN_no_prefix_is_declared_WHEN_sent_THEN_the_turns_own_prose_sits_inside_the_cached_block()
      {
         JToken system = SystemMessageOf(Request(withPrefix: false));
         JToken[] blocks = system["content"]!.ToArray();

         blocks.Should().HaveCount(1);
         blocks[0]["cache_control"]!["type"]!.Value<string>().Should().Be("ephemeral");
         blocks[0]["text"]!.Value<string>().Should().Contain(Prose);
      }

      #region private

      private static LlmRequest Request(bool withPrefix)
         => new() {
            SystemPrompt = ActionInterpreterPromptBuilder.Build(Prose, Facts),
            StableSystemPrompt = withPrefix ? ActionInterpreterPromptBuilder.StablePrefix : null,
            Messages = new List<LlmMessage> {new(MessageRole.User, ActionInterpreterPromptBuilder.FinalInstruction)},
            Parameters = new LlmParameters()
         };

      private static JToken SystemMessageOf(LlmRequest request)
      {
         var client = new OpenRouterClient(
            new System.Net.Http.HttpClient(),
            new OpenRouterConfig {ApiKey = "k", Model = "test/model"});

         var wire = JObject.Parse(JsonConvert.SerializeObject(client.ToWireFormat(request)));

         return wire["messages"]!.First(m => m["role"]!.Value<string>() == "system");
      }

      #endregion
   }
}

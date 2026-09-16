// Code written by Gabriel Mailhot, 16/09/2026.
// What these pin: that "Compact prompt for small models" reaches the parts of a turn that were never compact.
//
// DuskSymphony runs an 8,192-token local server. He reported on v2.5.7 that Compact still overflowed it, it
// was addressed, and he reported it again on v2.5.9: "occasionally one will get through the first message
// but followups will still be too high." He also reported the quieter half: "conversations that are within
// context limits but the NPCs are still losing their words."
//
// Three things in a turn had no notion of a prompt level at all:
//
//   1. The ACTION INTERPRETER prompt. 70,378 characters, ~17.6k tokens, sent once per turn on top of the
//      prose call. That is more than twice the entire context the setting is named for, so for those
//      players this call could never once succeed - and ActionInterpreterComposer swallows the failure and
//      plays on, so they got a game in which no deed was ever recorded and nothing said why.
//   2. The discovered-traits list, the one block here that GROWS without bound and is never aged out.
//   3. The language rule's four worked examples, which restate the line above them.
//
// If these fail, a player on a small local model is about to be told their context is too short - or worse,
// told nothing at all while their deeds stop counting.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class CompactPromptFitTests
   {
      #region the action interpreter

      // THE REPORT. An 8,192-token context cannot hold a 17.6k-token prompt, so the call never ran for him.
      // Held to 8,000 characters of headroom rather than a ratio, because what matters is fitting a real
      // server's window alongside the reply budget, not looking proportionally smaller.
      [Test]
      public void GIVEN_compact_mode_WHEN_the_interpreter_prompt_is_built_THEN_it_fits_inside_a_small_context()
      {
         int lean = ActionInterpreterPromptBuilder.StablePrefixFor(LeanPromptLevel.Lean).Length;
         int full = ActionInterpreterPromptBuilder.StablePrefixFor(LeanPromptLevel.Full).Length;

         full.Should().BeGreaterThan(60000, "the Full interpreter head is the whole catalogue with its guidance");
         lean.Should().BeLessThan(28000, "~7k tokens, so an 8,192 window still holds the prose, the facts and the reply");
         lean.Should().BeLessThan(full / 2);
      }

      // NO CAPABILITY IS TAKEN AWAY, and this is the line between a trim and a mutilation: Compact drops the
      // GUIDANCE on judging a deed, never the deed itself. A verb missing from the list cannot be emitted at
      // all, so a Compact player would silently lose features rather than precision.
      [Test]
      public void GIVEN_compact_mode_WHEN_the_interpreter_prompt_is_built_THEN_every_verb_is_still_listed()
      {
         string lean = ActionInterpreterPromptBuilder.StablePrefixFor(LeanPromptLevel.Lean);
         var missing = new List<string>();

         foreach (GameActionSpec spec in GameActionCatalog.All)
            if (!lean.Contains("- " + spec.Type + ":") && !lean.Contains(spec.Type))
               missing.Add(spec.Type);

         missing.Should().BeEmpty("Compact costs precision, never a capability");
      }

      // WHAT IT DOES COST, asserted so the trade is visible rather than assumed: the tells and anti-patterns
      // are the interpreter's precision, and they are what Compact spends. Full must still carry them.
      [Test]
      public void GIVEN_full_mode_WHEN_the_interpreter_prompt_is_built_THEN_the_judging_guidance_is_still_there()
      {
         ActionInterpreterPromptBuilder.StablePrefixFor(LeanPromptLevel.Full).Should().Contain("emit when:");
         ActionInterpreterPromptBuilder.StablePrefixFor(LeanPromptLevel.Lean).Should().NotContain("emit when:");
      }

      // The head must stay a literal prefix at BOTH levels or no breakpoint can cache it, and the interpreter
      // is the largest single block the mod sends.
      [Test]
      public void GIVEN_either_level_WHEN_the_interpreter_prompt_is_built_THEN_it_still_begins_with_its_own_head()
      {
         foreach (LeanPromptLevel level in new[] {LeanPromptLevel.Full, LeanPromptLevel.Lean})
            ActionInterpreterPromptBuilder.Build("*I nod.* Take it.", "YOU: A. PLAYER: B.", level)
                                          .Should().StartWith(ActionInterpreterPromptBuilder.StablePrefixFor(level));
      }

      #endregion

      #region the prompt's own unbounded list

      // The discovered-traits block has ONE job: stop a [DISCOVERY] being emitted for something already
      // known. A key does that job whole. The descriptions are colour, and this is the only list in the
      // prompt that grows without bound - nothing caps or ages it - so a long campaign pushed a small model
      // further over its context every time the player learned one more thing.
      [Test]
      public void GIVEN_compact_mode_WHEN_traits_are_already_known_THEN_only_their_keys_are_listed()
      {
         string lean = WithTraits(LeanPromptLevel.Lean);

         lean.Should().Contain("taste_for_praise");
         lean.Should().NotContain("stiffens when it is withheld", "the description is colour, and colour is what Compact spends");
         lean.Should().Contain("Do not emit [DISCOVERY]", "the block's whole purpose must survive the trim");
      }

      // Full keeps the descriptions: the character reads what the player already knows about them, which is
      // what stops the same revelation being played twice as though it were new.
      [Test]
      public void GIVEN_full_mode_WHEN_traits_are_already_known_THEN_they_are_described_in_full()
      {
         WithTraits(LeanPromptLevel.Full).Should().Contain("stiffens when it is withheld");
      }

      #endregion

      #region the language rule

      // The RULE survives; only its illustration goes. Four "Player writes in X -> you reply in X" bullets
      // restate the line directly above them, and a starved context can spare an example long before a rule.
      [Test]
      public void GIVEN_compact_mode_WHEN_the_prompt_is_built_THEN_the_language_rule_survives_without_its_examples()
      {
         string lean = WithTraits(LeanPromptLevel.Lean);

         lean.Should().Contain("CRITICAL — LANGUAGE OF YOUR REPLY:");
         lean.Should().Contain("SAME language as the player's last message");
         lean.Should().Contain("Proper names stay exactly as given");
         lean.Should().NotContain("→ you reply in German", "the bullets are the illustration, not the rule");
      }

      #endregion

      #region private

      private static string WithTraits(LeanPromptLevel lean)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

         npc.DiscoveredTraits.Add(new DiscoveredTrait {
            Key = "taste_for_praise",
            Description = "She has shown a marked taste for being praised in front of others, and stiffens when it is withheld."
         });

         return new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
            .BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = lean});
      }

      #endregion
   }
}

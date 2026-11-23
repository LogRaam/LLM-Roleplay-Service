// Code written by Gabriel Mailhot, 22/11/2026.
// What the character perceives of the player's narration. whiskeypatt (Discord 21/11/2026): "i rejoined the camp, tired,
// after killing 31 men. then went to greet lord A \"Hello A\" .. he in his reply mentioned 'so 31 men you killed was
// it?' ... basically reading my 'internal thought'". READING WHAT THE PLAYER DOES taught asterisks and first-person
// statements as real actions, and said nothing of thoughts or earlier deeds told in the same breath. Gabriel (22/11/2026)
// kept the asterisks and had the prompt say the character perceives only what is seen or heard in the room. If this
// rule is missing, NPCs read minds and quote the player's private narration; if it lands in the wrong prompt, a small
// local model (Lean) keeps doing it.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class NarrationPerceptionPromptTests
   {
      private static NpcProfile Npc() => new() {Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

      private static string Build(LeanPromptLevel lean) =>
         new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext {LeanLevel = lean});

      // STAKE: the full prompt tells the character that thoughts and an untold past are not seen, and never to quote them.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_built_THEN_the_character_perceives_only_what_is_seen_or_heard()
      {
         string prompt = Build(LeanPromptLevel.Full);
         prompt.Should().Contain("WHAT YOU PERCEIVE: only what can be SEEN or HEARD in this room now.");
         prompt.Should().Contain("Never repeat,");
      }

      // STAKE: whiskeypatt's message mixed narration and a quoted greeting; the quoted words alone are speech.
      [Test]
      public void GIVEN_the_full_prompt_WHEN_built_THEN_quoted_words_are_speech_and_the_rest_is_narration()
      {
         Build(LeanPromptLevel.Full).Should().Contain("those words are what they say; the rest around them is narration.");
      }

      // STAKE: small local models, the likeliest to read minds, get the rule too.
      [Test]
      public void GIVEN_the_lean_prompt_WHEN_built_THEN_the_rule_is_kept()
      {
         string prompt = Build(LeanPromptLevel.Lean);
         prompt.Should().Contain("unseen: never quote them.");
         prompt.Should().Contain("Quoted words are speech.");
      }

      // STAKE: the rule must not undo physical presence: an action in asterisks is still real and engaged with.
      [Test]
      public void GIVEN_the_new_rule_WHEN_built_THEN_an_action_is_still_treated_as_happening()
      {
         Build(LeanPromptLevel.Full).Should().Contain("Treat it as something actually HAPPENING right now");
      }
   }
}

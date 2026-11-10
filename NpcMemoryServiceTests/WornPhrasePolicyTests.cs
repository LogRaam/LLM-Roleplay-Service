// Code written by Gabriel Mailhot, 10/11/2026.
// WornPhrasePolicy finds, in a character's own earlier replies of this conversation, how they opened and the runs of
// words they have said twice, and the per-turn tail shows them so the next reply finds new ones; YOUR VOICE carries the
// character's habit of speech in the stable prefix. Gabriel (10/11/2026), after AI Influence 7.0.0; kurushimi00 (Nexus,
// 01/11/2026) heard characters recite like a checklist. If this is wrong, the character keeps its groove, a name is
// flagged as a tic, a phrase said once is forbidden, or the growing list lands above the cache marker and every turn
// pays for the whole prefix again.

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
   public class WornPhrasePolicyTests
   {
      private static NpcProfile Npc(string backstory = null) => new() {
         Id = "npc_test",
         Name = "Derthert",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         AuthoredBackstory = backstory
      };

      // STAKE: after a single reply there is no groove yet; telling the model otherwise only crowds the prompt.
      [Test]
      public void GIVEN_one_earlier_reply_WHEN_worn_words_are_sought_THEN_none_are_shown()
      {
         WornPhrasePolicy.Of(new[] {"Ah, my friend, the roads are long this season."}, null).IsEmpty.Should().BeTrue();
      }

      // STAKE: the reported groove: every reply opening "Ah, my friend" until the player notices.
      [Test]
      public void GIVEN_earlier_replies_WHEN_worn_words_are_sought_THEN_their_openings_are_listed_most_recent_first()
      {
         WornPhrases worn = WornPhrasePolicy.Of(new[] {"Ah, my friend, the roads are long.", "Well now, that is a fair question."}, null);

         worn.Openings.Should().Equal("Well now that is", "Ah my friend the");
      }

      // STAKE: a phrase said in two replies is a groove; the same words said once are just speech.
      [Test]
      public void GIVEN_a_run_of_words_in_two_replies_WHEN_worn_words_are_sought_THEN_it_is_listed_and_a_run_said_once_is_not()
      {
         WornPhrases worn = WornPhrasePolicy.Of(new[] {
            "I remember the battle at the ford as if it were yesterday.",
            "The harvest failed, and the river rose.",
            "Still, I remember the battle at the ford, and my brother."
         }, null);

         worn.Phrases.Should().Equal("I remember the battle at the ford");
         worn.Phrases.Should().NotContain(p => p.Contains("harvest"));
      }

      // STAKE: saying the player's name twice is courtesy, not a tic; flagging it would make the character avoid the name.
      [Test]
      public void GIVEN_a_repeated_run_holding_a_name_WHEN_worn_words_are_sought_THEN_it_is_spared()
      {
         WornPhrases worn = WornPhrasePolicy.Of(new[] {"You are welcome here Lady Aldric of the north.", "Sit down, you are welcome here Lady Aldric."}, new[] {"Aldric"});

         worn.Phrases.Should().NotContain(p => p.Contains("Aldric"));
      }

      // STAKE: a Turkish or French player gets the same help; nothing in the rule is English.
      [Test]
      public void GIVEN_replies_in_another_language_WHEN_worn_words_are_sought_THEN_the_repeated_run_is_found()
      {
         WornPhrases worn = WornPhrasePolicy.Of(new[] {"Comme le disait mon père, la guerre ne paie pas.", "Bah, comme le disait mon père hier."}, null);

         worn.Phrases.Should().Equal("comme le disait mon père");
      }

      // STAKE: the list must stay a short reminder, never a wall that crowds out the reply on a small model.
      [Test]
      public void GIVEN_many_replies_WHEN_worn_words_are_sought_THEN_no_more_than_the_caps_are_shown()
      {
         var lines = new List<string>();
         for (int i = 0; i < 20; i++) lines.Add($"Opening{i} words here now and the same tail of words again {i}");

         WornPhrases worn = WornPhrasePolicy.Of(lines, null);

         worn.Openings.Count.Should().BeLessThanOrEqualTo(WornPhrasePolicy.MaxOpenings);
         worn.Phrases.Count.Should().BeLessThanOrEqualTo(WornPhrasePolicy.MaxPhrases);
      }

      // STAKE: the list grows every turn; above the marker it would bust the cached prefix on every turn of a long talk.
      [Test]
      public void GIVEN_earlier_replies_WHEN_the_prompt_is_built_THEN_the_worn_words_sit_below_the_cache_marker()
      {
         EncounterContext context = new EncounterContext().WithOwnEarlierLines(new[] {"Ah, my friend, the roads are long.", "Ah, my friend, the rain again."});

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState(), context);

         prompt.Should().Contain("WORDS YOU HAVE ALREADY USED");
         prompt.IndexOf("WORDS YOU HAVE ALREADY USED").Should().BeGreaterThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading));
      }

      // STAKE: a host-built context with no chat behind it (a test, a letter) must not show an empty list.
      [Test]
      public void GIVEN_no_earlier_replies_WHEN_the_prompt_is_built_THEN_no_worn_words_are_shown()
      {
         new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState(), new EncounterContext()).Should().NotContain("WORDS YOU HAVE ALREADY USED");
      }

      // STAKE: the voice is the same all conversation, so it belongs in the cached prefix, above the marker.
      [Test]
      public void GIVEN_a_character_voice_WHEN_the_prompt_is_built_THEN_it_renders_above_the_cache_marker()
      {
         var context = new EncounterContext {CharacterVoice = "You speak in short, clipped sentences."};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState(), context);

         prompt.Should().Contain("YOUR VOICE (how you, and no one else, speak):");
         prompt.IndexOf("You speak in short, clipped sentences.").Should().BeLessThan(prompt.IndexOf(PromptBuilder.EncounterSectionHeading));
      }

      // STAKE: a player who wrote "he stutters and says 'aye'" must not see a derived habit override what they authored.
      [Test]
      public void GIVEN_an_authored_backstory_WHEN_the_voice_renders_THEN_the_backstory_voice_is_said_to_win()
      {
         var context = new EncounterContext {CharacterVoice = "You speak in short, clipped sentences."};

         string prompt = new PromptBuilder().BuildSystemPrompt(Npc("He stutters and ends every line with 'aye'."), new WorldState(), context);

         prompt.Should().Contain("where your backstory above says how you speak, it wins");
         prompt.Should().Contain("take the habit under YOUR VOICE below");
      }
   }
}

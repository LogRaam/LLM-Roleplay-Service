// Code written by Gabriel Mailhot, 10/11/2026.
// WornPhrasePolicy finds, in a character's own earlier replies of this conversation, how they opened and the runs of
// words they have said twice, and the note on the player's new line shows them so the next reply finds new ones; YOUR VOICE carries the
// character's habit of speech in the stable prefix. Gabriel (10/11/2026), after AI Influence 7.0.0; kurushimi00 (Nexus,
// 01/11/2026) heard characters recite like a checklist. If this is wrong, the character keeps its groove, a name is
// flagged as a tic, a phrase said once is forbidden, or the note lands in the system prompt and every turn bills the
// history and the closing rules afresh on providers that cache the longest common prefix.

#region

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
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

      // STAKE: the note must say it is not the player's words, or the character answers it as if the player had said it.
      [Test]
      public void GIVEN_a_note_WHEN_put_on_the_player_s_line_THEN_it_follows_the_line_says_whose_it_is_and_the_session_line_is_untouched()
      {
         var history = new List<LlmMessage> {new(MessageRole.Assistant, "Aye."), new(MessageRole.User, "What of the ford?")};
         string note = WornPhrasePolicy.Render(WornPhrasePolicy.Of(new[] {"Ah, my friend, the roads.", "Ah, my friend, the rain."}, null));

         IReadOnlyList<LlmMessage> sent = WornPhrasePolicy.OnTheLastLine(history, note);

         sent[1].Content.Should().StartWith("What of the ford?");
         sent[1].Content.Should().Contain("[A note to you, not words the player said.");
         history[1].Content.Should().Be("What of the ford?");
      }

      // STAKE: with nothing worn, or a last line that is not the player's, the request goes exactly as before.
      [Test]
      public void GIVEN_no_note_or_no_player_line_last_WHEN_put_on_the_line_THEN_the_messages_are_unchanged()
      {
         var history = new List<LlmMessage> {new(MessageRole.User, "Hail."), new(MessageRole.Assistant, "Aye.")};

         WornPhrasePolicy.OnTheLastLine(history, null).Should().BeSameAs(history);
         WornPhrasePolicy.OnTheLastLine(history, "[A note]").Should().BeSameAs(history);
      }

      // STAKE: Grok, GLM and DeepSeek cache the longest common prefix (09/11/2026 log: 16,768 of 16,925 tokens at turn 18).
      // A note that changed the system prompt every turn would bill the history and the closing rules afresh each turn;
      // on the wire, two turns share the whole system prompt and every line but the last few.
      [Test]
      public async Task GIVEN_two_turns_with_worn_words_WHEN_sent_THEN_the_system_prompt_and_the_earlier_lines_are_the_same()
      {
         var client = new CapturingClient();
         var service = new NpcChatService(client, new SectionResponseParser(), LeanRealPromptBudgetTests.RealBuilder());
         var session = new ChatSession();
         session.AddPlayerMessage("Tell me of the war.");
         session.AddNpcMessage("I remember the battle at the ford as if it were yesterday.");
         session.AddPlayerMessage("And then?");
         session.AddNpcMessage("Still, I remember the battle at the ford, and my brother.");
         EncounterContext context = LeanRealPromptBudgetTests.Encounter(LeanPromptLevel.Full);

         await service.ChatAsync(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, session, "Who won?", context);
         LlmRequest first = client.Last;
         await service.ChatAsync(LeanRealPromptBudgetTests.Npc(), new WorldState {CurrentDay = 10}, session, "And your brother?", context);
         LlmRequest second = client.Last;

         first.Messages.Last().Content.Should().Contain("I remember the battle at the ford");
         second.SystemPrompt.Should().Be(first.SystemPrompt);
         second.SystemPrompt.Should().NotContain("WORDS YOU HAVE ALREADY USED");
         second.Messages.Take(first.Messages.Count - 1).Select(m => m.Content).Should().Equal(first.Messages.Take(first.Messages.Count - 1).Select(m => m.Content));
         session.Messages[session.Messages.Count - 2].Content.Should().Be("And your brother?");
      }

      private sealed class CapturingClient : ILlmClient
      {
         public LlmRequest Last { get; private set; } = null!;

         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
         {
            Last = new LlmRequest {SystemPrompt = request.SystemPrompt, Messages = request.Messages.ToList(), Parameters = request.Parameters};

            return Task.FromResult(new LlmResponse {Content = "[DIALOGUE]\nAye.", IsSuccess = true});
         }
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

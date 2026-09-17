// Code written by Gabriel Mailhot, 17/09/2026.
// What these pin: that what a character says in a conversation does not come back at them as HISTORY in the
// same conversation.
//
// Player report, 17/09/2026:
//
//   "i start a conversation and ask a question early on in the conversation, then for some reason, even
//    though the conversation has moved beyond that question, the ai keeps including the answer to that
//    question in every single response… and the longer the conversation goes, the longer their replies get
//    because they feel the need to state the entire conversation in each reply."
//
// He included a worked example in which the character answers "yes I have met Bob" on turn one and then
// appends it to every later reply whatever is actually being discussed.
//
// It is a loop, and every part of it was working as designed. ProfileMutator runs EVERY turn, so an [EVENT]
// emitted on turn two is in the profile for turn three. AppendHistory then renders it as a notable memory,
// stamped "earlier today", and closes the block with "Respond as someone who lived through these events.
// Reference them when relevant." The model is being told, in as many words, to bring up something it said
// twenty seconds ago — which is ALSO still in the raw messages, so it appears twice. Each new memory adds
// another line, which is why his replies grew.
//
// The cut is a count taken when the conversation opens. If these fail, a character is about to start
// reciting the conversation back at the player.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class HistoryStopsAtThisConversationTests
   {
      private const string Old = "I fought beside them at Pendraic, long before this.";
      private const string JustNow = "I told them I have met Bob.";

      // THE REPORT. One memory from before, one made moments ago in this very conversation: the first is
      // history, the second is the conversation itself and the model already has it verbatim.
      [Test]
      public void GIVEN_a_memory_made_during_this_conversation_WHEN_the_prompt_is_built_THEN_it_is_not_replayed_as_history()
      {
         string prompt = Build(knownThrough: 1, Old, JustNow);

         prompt.Should().Contain(Old, "what happened before the conversation is what history means");
         prompt.Should().NotContain(JustNow, "this one is in the raw messages already; repeating it is the bug");
      }

      // And it compounds, which is the half the player actually felt: every turn adds one more line, so the
      // block grows into a restatement of the whole exchange.
      [Test]
      public void GIVEN_several_memories_made_during_this_conversation_WHEN_built_THEN_none_of_them_is_replayed()
      {
         // Distinctive strings on purpose: the first draft searched for "Vlandia", which is also this
         // character's FACTION and appears in the identity line, so the test failed for a reason that had
         // nothing to do with the history block.
         string prompt = Build(knownThrough: 1, Old, JustNow,
            "I told them about the siege at Zeonica.", "I told them about the ford at Ortysia.");

         prompt.Should().Contain(Old);
         prompt.Should().NotContain("Zeonica").And.NotContain("Ortysia").And.NotContain(JustNow);
      }

      // A CONVERSATION WITH SOMEBODY NEW must still read as a first meeting rather than as an empty history
      // block: when everything the character holds was made in this conversation, there IS no past.
      [Test]
      public void GIVEN_a_stranger_whose_only_memories_are_from_this_talk_WHEN_built_THEN_it_still_reads_as_a_first_meeting()
      {
         string prompt = Build(knownThrough: 0, JustNow);

         prompt.Should().Contain("YOUR HISTORY WITH THIS PLAYER:");
         prompt.Should().NotContain(JustNow);
      }

      // UNSET RENDERS EVERYTHING, so every caller that does not supply a cut keeps exactly the behaviour it
      // has today. A silent change of meaning for existing callers would be a worse bug than the one fixed.
      [Test]
      public void GIVEN_no_cut_supplied_WHEN_built_THEN_the_whole_history_is_rendered_as_before()
      {
         Build(knownThrough: null, Old, JustNow).Should().Contain(Old).And.Contain(JustNow);
      }

      // ZERO IS NOT UNSET, and this test is why the field is nullable. A character the player has never met
      // held no memories when the conversation opened — the commonest case in the game — so zero must cut
      // EVERYTHING. Sentinelling it as "nobody said" would have left exactly those conversations broken,
      // which are the ones a new player has.
      [Test]
      public void GIVEN_a_cut_of_zero_WHEN_built_THEN_it_is_read_as_a_stranger_and_not_as_an_absent_answer()
      {
         Build(knownThrough: 0, JustNow).Should().NotContain(JustNow);
      }

      // THE UNTRUSTWORTHY CASE. A compression pass mid-conversation rewrites the list and can leave the cut
      // pointing past its end. Rendering everything is then the honest answer: the fallback is today's
      // behaviour, so a mis-read can never LOSE a memory — only fail to hide one.
      [Test]
      public void GIVEN_a_cut_beyond_the_end_of_the_history_WHEN_built_THEN_everything_is_rendered_rather_than_nothing()
      {
         Build(knownThrough: 99, Old, JustNow).Should().Contain(Old).And.Contain(JustNow);
      }

      #region private

      private static string Build(int? knownThrough, params string[] summaries)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

         foreach (string summary in summaries)
            npc.Events.Add(new NotableEvent(10, NotableEventType.Other, summary));

         return new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 12},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, HistoryKnownThrough = knownThrough});
      }

      #endregion
   }
}

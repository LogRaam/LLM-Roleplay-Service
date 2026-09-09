// Code written by Gabriel Mailhot, 08/09/2026.
// What this pins: how the VANILLA conversation that happened just before the CR chat opened reaches the model.
//
// Why it exists. Gabriel tested a chat with a wanderer on 2026-09-08 and reported that the man restarted the
// conversation at every reply. The log bears it out: turn one answered him properly, then turn two was "Arwa. A
// fine name, I am glad to meet you, truly", turn three introduced himself all over again and under the wrong
// name, and turn four described sizing up a stranger for the first time.
//
// The history was never lost. The prompt grew every single turn (16904, 17157, 17429 tokens), so the model had
// everything. The fault was in the FRAMING: the mod seeded the nine lines of the wanderer's vanilla backstory
// ritual as alternating TURNS, and that ritual opens with the player saying "My name is Arwa, sir. Tell me about
// yourself." So the conversation the model saw began with an introduction, and it dutifully kept answering it.
//
// Gabriel's ruling: the vanilla conversation is HISTORY, and its only value is to bridge the vanilla exchange
// into the chat. Not turns awaiting a reply. One labelled block, in the same spirit as AddWitnessStatement,
// where a prefix is what tells the model whose words these are and what to do with them.
//
// If these tests fail, an NPC is about to start greeting the player over and over again in the middle of a
// conversation they are already having.

#region

using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PriorExchangeSessionTests
   {
      private const string Exchange = "Arwa: My name is Arwa, sir. Tell me about yourself.\nVangvayag: I am from one of the highland clans.";

      // THE fix. Nine alternating turns are nine things to answer, and the model answered the first of them for
      // the rest of the conversation. One block is one piece of ground to stand on.
      [Test]
      public void GIVEN_a_prior_vanilla_exchange_WHEN_recorded_THEN_it_becomes_ONE_message_not_a_run_of_turns()
      {
         var session = new ChatSession();

         session.AddPriorExchange(Exchange);

         session.Messages.Should().HaveCount(1);
      }

      // Stating the fact is not enough, as this project keeps relearning: the block has to say what it is FOR,
      // or a model reading an introduction will introduce itself back, whatever role the message carries.
      [Test]
      public void GIVEN_the_block_WHEN_recorded_THEN_it_forbids_greeting_and_introducing_afresh()
      {
         var session = new ChatSession();

         session.AddPriorExchange(Exchange);

         string content = session.Messages.Single().Content;
         content.Should().Contain("already said");
         content.Should().Contain("Do not greet again");
         content.Should().Contain("not a question waiting on you");
      }

      // The content itself must survive: the whole point of bridging is that the NPC knows what was just said.
      // A framing that dropped the words would trade one kind of amnesia for another.
      [Test]
      public void GIVEN_the_block_WHEN_recorded_THEN_the_words_that_were_said_are_still_there()
      {
         var session = new ChatSession();

         session.AddPriorExchange(Exchange);

         session.Messages.Single().Content.Should().Contain("highland clans");
      }

      // Same channel the witness statements use, and for the same reason: the prefix does the work, so this sits
      // in the conversation where the model reads it, rather than in a system preamble it may weigh differently.
      [Test]
      public void GIVEN_the_block_WHEN_recorded_THEN_it_rides_the_same_channel_as_a_witness_statement()
      {
         var session = new ChatSession();

         session.AddPriorExchange(Exchange);

         session.Messages.Single().Role.Should().Be(MessageRole.User);
      }

      // A bridge needs the ground nearest the crossing. A wanderer's backstory ritual can run long, and if it
      // has to be cut, the words just spoken matter more than the ones from five minutes ago.
      [Test]
      public void GIVEN_a_very_long_exchange_WHEN_recorded_THEN_it_is_trimmed_from_the_FRONT()
      {
         var session = new ChatSession();
         string longExchange = new string('x', ChatSession.PriorExchangeMaxChars * 2) + "THE LAST THING SAID";

         session.AddPriorExchange(longExchange);

         string content = session.Messages.Single().Content;
         content.Should().Contain("THE LAST THING SAID");
         content.Should().Contain("...");
         content.Length.Should().BeLessThan(ChatSession.PriorExchangeMaxChars + 500); // the framing, plus the cap
      }

      // Nothing said before means nothing to bridge. An empty block would be a paragraph of instructions attached
      // to no content, which is pure noise in a prompt that is already large.
      [Test]
      public void GIVEN_nothing_was_said_before_WHEN_recorded_THEN_no_message_is_added()
      {
         var session = new ChatSession();

         session.AddPriorExchange(null);
         session.AddPriorExchange("   ");

         session.Messages.Should().BeEmpty();
      }

      // The bridge must not become the conversation. A real player turn recorded afterwards has to remain the
      // most recent thing the model sees, or we would have rebuilt the same bug with tidier wording.
      [Test]
      public void GIVEN_the_player_speaks_after_the_bridge_WHEN_read_THEN_their_words_come_last()
      {
         var session = new ChatSession();

         session.AddPriorExchange(Exchange);
         session.AddPlayerMessage("I saw your skill in the tournament.");

         session.Messages.Last().Content.Should().Be("I saw your skill in the tournament.");
      }
   }
}

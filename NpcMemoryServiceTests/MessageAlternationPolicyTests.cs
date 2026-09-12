// Written from the server's refusal, not from the code that produces it.
//
// fkasad + iwasajedionce, Nexus, 2026-09-11. The player-visible symptom is "my words escape me. your message
// didnt reach the AI model"; the server behind it said why:
//
//   Jinja Exception: After the optional system message, conversation roles must alternate user and assistant
//   roles except for tool calls and results.
//
// So what is owed to a player is this: whatever CR wants to model - a room of witnesses, a bridge from the
// vanilla dialogue, an NPC who speaks first - what leaves for the server must be a conversation any chat
// template will accept, and it must still say who spoke.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class MessageAlternationPolicyTests
   {
      private static LlmMessage User(string text) => new(MessageRole.User, text);
      private static LlmMessage Npc(string text) => new(MessageRole.Assistant, text);

      // THE REPORT, first half. A room of witnesses is several user turns in a row, and a strict template
      // refuses the request outright rather than answering it badly.
    [Test]
      public void A_room_that_speaks_several_times_still_leaves_as_an_alternating_conversation()
      {
         var said = new List<LlmMessage> {
            User("Tell me what happened here."),
            Npc("The raiders came at dawn."),
            User("[Martira]: I saw them take the grain."),
            User("[Alympia]: And they burned the mill."),
            User("Who led them?")
         };

         IReadOnlyList<LlmMessage> wire = MessageAlternationPolicy.Normalize(said);

         MessageAlternationPolicy.Alternates(wire).Should().BeTrue();
      }

      // And nothing is lost doing it: the prefixes are what carry attribution, so a merged turn still names
      // every speaker in it. This is the assertion that makes the merge safe rather than merely legal.
    [Test]
      public void Merging_neighbouring_turns_keeps_every_speaker_the_model_needs_to_read()
      {
         IReadOnlyList<LlmMessage> wire = MessageAlternationPolicy.Normalize(new List<LlmMessage> {
            User("[Martira]: I saw them take the grain."),
            User("[Alympia]: And they burned the mill."),
            User("Who led them?")
         });

         string all = string.Join("\n", wire.Select(m => m.Content));
         all.Should().Contain("[Martira]:").And.Contain("[Alympia]:").And.Contain("Who led them?");
      }

      // THE REPORT, second half. A chat opened out of a vanilla conversation begins with the line the player
      // just heard, which is an NPC turn with nothing before it. A template that wants the user to speak
      // first cannot be handed that.
    [Test]
      public void A_conversation_that_opens_with_the_npc_still_leaves_with_the_user_speaking_first()
      {
         IReadOnlyList<LlmMessage> wire = MessageAlternationPolicy.Normalize(new List<LlmMessage> {
            Npc("You again. What do you want?"),
            User("A word about the village.")
         });

         wire[0].Role.Should().Be(MessageRole.User);
         MessageAlternationPolicy.Alternates(wire).Should().BeTrue();
      }

      // The NPC's opening line must SURVIVE that move, and must not read as something awaiting an answer -
      // the exact failure the prior-exchange bridge was written for on 2026-09-08, when a wanderer kept
      // re-introducing himself because his own words looked like a question.
    [Test]
      public void The_opening_line_survives_and_is_marked_as_already_said_rather_than_as_a_question()
      {
         IReadOnlyList<LlmMessage> wire = MessageAlternationPolicy.Normalize(new List<LlmMessage> {
            Npc("You again. What do you want?"),
            User("A word about the village.")
         });

         wire[0].Content.Should().Contain("You again. What do you want?");
         wire[0].Content.Should().Contain("not a question waiting on you");
      }

      // An ordinary two-sided conversation must come out untouched, or this repair would be a change to
      // every player on OpenRouter who never had a problem.
    [Test]
      public void A_conversation_that_already_alternates_is_left_exactly_as_it_was()
      {
         var said = new List<LlmMessage> {
            User("Good evening."), Npc("It is."), User("May we speak?"), Npc("Briefly.")
         };

         IReadOnlyList<LlmMessage> wire = MessageAlternationPolicy.Normalize(said);

         wire.Should().HaveCount(4);
         wire.Select(m => m.Content).Should().Equal(said.Select(m => m.Content));
         wire.Select(m => m.Role).Should().Equal(said.Select(m => m.Role));
      }

      // A blank turn is not a turn. Left in, it would either occupy a slot in the alternation or reach the
      // server as an empty message, and some servers refuse those too.
    [Test]
      public void A_blank_turn_is_dropped_rather_than_sent_as_an_empty_one()
      {
         IReadOnlyList<LlmMessage> wire = MessageAlternationPolicy.Normalize(new List<LlmMessage> {
            User("Speak."), Npc("   "), User("I said speak.")
         });

         wire.Should().ContainSingle();
         MessageAlternationPolicy.Alternates(wire).Should().BeTrue();
      }

      // Two NPC turns in a row are possible too (a reply followed by a staged beat), and they get the same
      // treatment as two player turns: this rule is about the SHAPE, not about who happens to be talking.
    [Test]
      public void Two_npc_turns_in_a_row_are_merged_the_same_way_a_room_is()
      {
         IReadOnlyList<LlmMessage> wire = MessageAlternationPolicy.Normalize(new List<LlmMessage> {
            User("Well?"), Npc("I will not."), Npc("Ask me again and I leave.")
         });

         MessageAlternationPolicy.Alternates(wire).Should().BeTrue();
         wire.Last().Content.Should().Contain("I will not.").And.Contain("Ask me again and I leave.");
      }

      // Nothing to send is a valid state (a fresh session), and must not throw on the path every single
      // request takes.
    [Test]
      public void Nothing_to_send_is_answered_with_nothing_rather_than_a_crash()
      {
         MessageAlternationPolicy.Normalize(null).Should().BeEmpty();
         MessageAlternationPolicy.Normalize(new List<LlmMessage>()).Should().BeEmpty();
         MessageAlternationPolicy.Alternates(null).Should().BeTrue();
      }

      // The guard's own honesty: it must be able to FAIL. A test suite that only ever feeds Alternates a
      // normalised list proves nothing about it.
    [Test]
      public void The_alternation_check_rejects_the_two_shapes_the_server_rejected()
      {
         MessageAlternationPolicy.Alternates(new List<LlmMessage> {Npc("I speak first."), User("Hm.")})
                           .Should().BeFalse();
         MessageAlternationPolicy.Alternates(new List<LlmMessage> {User("One."), User("Two.")})
                           .Should().BeFalse();
      }
   }
}

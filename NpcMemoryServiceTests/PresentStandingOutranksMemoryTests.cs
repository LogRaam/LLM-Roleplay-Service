// Code written by Gabriel Mailhot, 19/09/2026.
// What these pin: that a character who remembers the player as a fellow subject does not go on treating them as
// one after the player has left the realm.
//
// tashmetu (Nexus, 19/09/2026): "We just lost a war and I got kicked out of the kingdom, yet they kept talking to me
// and sending letters as if I were still part of it." The standing note is rebuilt live every turn and was right.
// The memories were not wrong either: they said what was true when they were made ("we fought for the king"). But
// they come after the standing note, and they close with "respond as someone who lived through these events", so the
// last word on the player's allegiance was a month old.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PresentStandingOutranksMemoryTests
   {
      // The history block says in so many words that memories keep the allegiances of their day.
      [Test]
      public void GIVEN_a_character_with_memories_of_the_player_WHEN_the_prompt_is_built_THEN_it_is_told_the_present_standing_wins()
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};
         npc.Events.Add(new NotableEvent(10, NotableEventType.Other, "We fought side by side for King Derthert at Sargot."));

         string prompt = new PromptBuilder().BuildSystemPrompt(npc, new WorldState {CurrentDay = 60},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full});

         prompt.Should().Contain("Allegiances in these memories are as they stood then");
         prompt.Should().Contain("the present one stated above is true now");
      }

      // A ruler writing to a player with no liege must not be told, before reading the Context, that the player
      // serves someone. The old occasion text did exactly that and the Context could not undo it.
      [Test]
      public void GIVEN_a_service_offer_letter_WHEN_the_occasion_is_written_THEN_it_never_presumes_the_player_serves_a_lord()
      {
         string message = LetterPromptBuilder.BuildInitialLetterMessage(
            new NpcProfile {Id = "r", Name = "King Derthert", Faction = "Vlandia", Clan = "dey Meroc"}, LetterReason.ServiceOffer,
            "You are the ruler of Vlandia. The player serves no realm at present, sworn to no liege, and you would have them enter YOUR service.",
            "Tashmetu");

         message.Should().NotContain("leave the lord they now serve");
         message.Should().Contain("serves no realm at present");
      }
   }
}

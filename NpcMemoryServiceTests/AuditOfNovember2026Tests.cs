// Code written by Gabriel Mailhot, 15/11/2026.
// VonLemberg's source audit of 2.8.10 (Discord, 15/11/2026), the SDK half. Three findings read true in the code:
//  - "WorldClaimPolicy.Judge() rejects claims when AboutThePlayer == true, but WorldClaimExtractor.Parse() never
//    populates that property ... 'The player quarrelled with the steward at Pravend' could pass as QUARREL."
//  - "WorldClaimExtractor has no explicit temporal-status requirement distinguishing planned events from completed
//    ones ... 'I intend to arrange a marriage' must not become news of an actual marriage."
//  - "the player met a lord 40 days ago, but the lord received an unrelated memory yesterday. The system may treat
//    yesterday as their last meeting ... Unrelated memories must not establish personal acquaintance."
// If these fail: the player's deeds enter the world under a second author, a plan is told across Calradia as a thing
// done, or a rumour makes a stranger greet the player as an old acquaintance.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class AuditOfNovember2026Tests
   {
      // THE AUDIT's own case: the model labelled a player's deed QUARREL and nothing set the flag.
      [Test]
      public void GIVEN_a_claim_that_names_the_player_WHEN_judged_THEN_it_is_refused_whatever_its_label()
      {
         var claim = new WorldClaim {SpeakerId = "hero_arion", Subject = ClaimSubject.LocalQuarrel, Text = "Arwa quarrelled with the steward at Pravend."};

         WorldClaimPolicy.Judge(claim, false, false, "Arwa of the Banu Ara").Should().Be(ClaimVerdict.AboutThePlayer);
      }

      // STAKE: the model may write "the player" instead of a name; the deed is the player's all the same.
      [Test]
      public void GIVEN_a_claim_that_says_the_player_WHEN_judged_THEN_it_is_refused()
      {
         var claim = new WorldClaim {SpeakerId = "hero_arion", Subject = ClaimSubject.LocalQuarrel, Text = "The player quarrelled with the steward at Pravend."};

         WorldClaimPolicy.Judge(claim, false).Should().Be(ClaimVerdict.AboutThePlayer);
      }

      // THE AUDIT: "A genuinely reported local marriage or quarrel must still enter the rumor pipeline."
      [Test]
      public void GIVEN_a_local_quarrel_that_does_not_name_the_player_WHEN_judged_THEN_it_still_enters_the_world()
      {
         var claim = new WorldClaim {SpeakerId = "hero_arion", Subject = ClaimSubject.LocalQuarrel, Text = "The miller and the steward at Pravend have fallen out over a debt."};

         WorldClaimPolicy.Judge(claim, false, false, "Arwa of the Banu Ara").Should().Be(ClaimVerdict.Accepted);
      }

      // STAKE: a name that only sits INSIDE another word is not the player ("Arwa" in "Arwald"), and the small words
      // of a name ("of", "the") never match on their own, or every sentence would be about the player.
      [Test]
      public void GIVEN_a_sentence_that_only_shares_letters_or_small_words_with_the_name_WHEN_read_THEN_it_does_not_name_the_player()
      {
         WorldClaimPolicy.NamesThePlayer("Arwald the smith died of the fever at Pravend.", "Arwa of the Banu Ara").Should().BeFalse();
      }

      // THE AUDIT: a plan is not news of the thing itself. The model is told so in the words it acts on.
      [Test]
      public void GIVEN_the_instruction_WHEN_read_THEN_it_keeps_plans_and_hopes_out_of_the_news()
      {
         string prompt = WorldClaimExtractor.BuildSystemPrompt("Arion");

         prompt.Should().Contain("Report only what has HAPPENED or is happening");
         prompt.Should().Contain("A plan, an intention, a promise, a hope");
      }

      // THE AUDIT's own example: met 40 days ago, a rumour heard yesterday. The last meeting is still 40 days old.
      [Test]
      public void GIVEN_a_meeting_long_ago_and_a_rumour_heard_yesterday_WHEN_asked_when_they_last_dealt_THEN_it_is_the_meeting()
      {
         var events = new List<NotableEvent> {
            new(60, NotableEventType.FirstMeeting, "Met at the feast."),
            new(99, NotableEventType.Hearsay, "Heard that a caravan was taken near Pravend."),
            new(98, NotableEventType.OwnLife, "Buried an uncle.")
         };

         HistoryWindowPolicy.LastLivedDay(events).Should().Be(60);
      }

      // STAKE: a lord who only ever heard rumours, or lived his own life, has never dealt with the player.
      [Test]
      public void GIVEN_only_rumours_and_a_life_apart_WHEN_asked_what_they_lived_together_THEN_it_is_nothing()
      {
         var events = new List<NotableEvent> {
            new(99, NotableEventType.Hearsay, "Heard that a caravan was taken near Pravend."),
            new(98, NotableEventType.OwnLife, "Buried an uncle.")
         };

         HistoryWindowPolicy.LivedCount(events).Should().Be(0);
         HistoryWindowPolicy.LastLivedDay(events).Should().BeNull();
      }

      // STAKE: genuine meetings and dealings still count, each one.
      [Test]
      public void GIVEN_memories_lived_with_the_player_WHEN_counted_THEN_each_one_counts()
      {
         var events = new List<NotableEvent> {
            new(60, NotableEventType.FirstMeeting, "Met at the feast."),
            new(70, NotableEventType.Agreement, "Agreed to ride together."),
            new(71, NotableEventType.Other, "Spoke of the war.")
         };

         HistoryWindowPolicy.LivedCount(events).Should().Be(3);
      }
   }
}

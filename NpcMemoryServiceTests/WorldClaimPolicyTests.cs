// Code written by Gabriel Mailhot, 12/09/2026.
// Invented facts, increment 1. What these pin: which things an NPC makes up may become true in Calradia, and
// which may never.
//
// Why it exists. Gabriel watched a governor invent a blight in his own fields and asked the right question:
// "si un NPC invente un FAIT... et que le joueur en parle à un autre NPC, ce dernier ne connaîtra pas ce fait.
// Ça peut créer de l'incohérence. Est-ce qu'il est possible de capter des FAITS INVENTÉS et de les rendre
// réels dans le Monde?"
//
// His two rulings are the two halves of this policy, and neither is negotiable:
//
//   "Les nouveaux faits proviennent QUE des NPC, pas du joueur. Le joueur subit le Monde alors que les NPC le
//    définissent. C'est la nature du jeu vidéo."
//
//   and the safety rule that makes any of it defensible: a claim may only enter the world where the ENGINE HAS
//   NOTHING TO SAY. Bannerlord has no field for a blight, so an invented blight cannot contradict a screen.
//   It does have fields for troops, coin and who holds a town - so an invention there is a checkable lie, the
//   exact thing seat_standing was built to end.
//
// This gate is the only thing between a language model and shared, saved world state. If these fail, either a
// player's own invention has started rewriting Calradia, or a model has been handed a field the game owns.

#region

using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class WorldClaimPolicyTests
   {
      /// <summary>The mildew that started it: a governor's invention about his own village.</summary>
      private static WorldClaim Mildew(ClaimSubject subject = ClaimSubject.Blight)
         => new() {
            SpeakerId = "hero_arion", SpeakerName = "Arion", Subject = subject, PlaceName = "Hargendorf",
            Text = "A blight has taken the grain in the fields east of Hargendorf.", StatedAsFact = true
         };

      // ── Gabriel's ruling on who authors the world ────────────────────────

      // "Le joueur subit le Monde alors que les NPC le définissent." A player who tells a lord there is a
      // blight has told him a story; Calradia is not obliged to arrange one. Without this the mod would ship a
      // way to legislate reality by lying, which is a different game than the one being made.
      [Test]
      public void GIVEN_the_player_says_it_WHEN_judged_THEN_the_world_does_not_take_it()
      {
         WorldClaimPolicy.Judge(Mildew(), true).Should().Be(ClaimVerdict.FromThePlayer);
         WorldClaimPolicy.MayEnterTheWorld(Mildew(), true).Should().BeFalse();
      }

      // And the same thing said by the character whose fields they are does enter, because that is the whole
      // point: an NPC's invention is the world defining itself.
      [Test]
      public void GIVEN_a_lord_says_it_of_his_own_village_WHEN_judged_THEN_it_may_become_real()
      {
         WorldClaimPolicy.Judge(Mildew(), false).Should().Be(ClaimVerdict.Accepted);
      }

      // A claim nobody said has no author, and an unauthored fact cannot be attributed, doubted or traced back
      // when it turns out to be nonsense.
      [Test]
      public void GIVEN_nobody_said_it_WHEN_judged_THEN_it_is_refused()
      {
         WorldClaimPolicy.Judge(null, false).Should().Be(ClaimVerdict.NoSpeaker);
         WorldClaimPolicy.Judge(new WorldClaim {Subject = ClaimSubject.Blight, Text = "Grain has failed."}, false)
                         .Should().Be(ClaimVerdict.NoSpeaker);
      }

      // ── the rule that keeps a model out of the engine's business ─────────

      // THE SAFETY RULE. Troops, coin, who holds a town, who is at war: the game has all of these on a screen
      // the player can open, so an invention here is not colour, it is a lie with a receipt. This is the
      // "bands, not numbers" boundary one level up.
      [Test]
      public void GIVEN_a_claim_about_something_the_game_itself_records_WHEN_judged_THEN_it_never_enters()
      {
         WorldClaimPolicy.Judge(Mildew(ClaimSubject.EngineOwned), false).Should().Be(ClaimVerdict.EngineOwnsIt);
      }

      // The promotable subjects share exactly one property, and it is worth asserting rather than trusting: the
      // engine models none of them. If somebody adds a sixth, this is where they have to argue for it.
      [Test]
      public void GIVEN_the_promotable_list_WHEN_read_THEN_it_is_only_things_the_game_does_not_model()
      {
         WorldClaimPolicy.PromotableSubjects.Should().BeEquivalentTo(new[] {
            ClaimSubject.Blight, ClaimSubject.Banditry, ClaimSubject.PassingOrBirth,
            ClaimSubject.Omen, ClaimSubject.LocalQuarrel
         });

         WorldClaimPolicy.PromotableSubjects.Should().NotContain(ClaimSubject.EngineOwned);
         WorldClaimPolicy.PromotableSubjects.Should().NotContain(ClaimSubject.Unknown);
      }

      // An unclassified claim is refused rather than waved through. The extractor not knowing what a sentence
      // was about is precisely when nobody should be acting on it.
      [Test]
      public void GIVEN_a_claim_nobody_could_classify_WHEN_judged_THEN_it_is_refused_rather_than_assumed_harmless()
      {
         WorldClaimPolicy.Judge(Mildew(ClaimSubject.Unknown), false).Should().Be(ClaimVerdict.Unclassified);
      }

      // The player's own deeds already reach the world through the deed and chronicle systems. Letting a
      // conversation mint them too would put two authors on one record, which is how two records disagree.
      [Test]
      public void GIVEN_a_claim_about_the_player_WHEN_judged_THEN_the_chronicle_keeps_its_single_author()
      {
         var aboutPlayer = new WorldClaim {
            SpeakerId = "hero_arion", Subject = ClaimSubject.LocalQuarrel, AboutThePlayer = true,
            Text = "The player quarrelled with the seneschal at Pravend."
         };

         WorldClaimPolicy.Judge(aboutPlayer, false).Should().Be(ClaimVerdict.AboutThePlayer);
      }

      // ── what a rumour is allowed to be ───────────────────────────────────

      // A rumour is a sentence. Anything longer is a model writing fiction into the save file, and it will be
      // read back to strangers months later with no one to explain it.
      [Test]
      public void GIVEN_a_claim_too_long_or_too_empty_to_be_news_WHEN_judged_THEN_it_is_not_sayable()
      {
         var tooLong = new WorldClaim {
            SpeakerId = "hero_arion", Subject = ClaimSubject.Blight,
            Text = new string('a', WorldClaimPolicy.MaxTextChars + 1)
         };
         var tooShort = new WorldClaim {SpeakerId = "hero_arion", Subject = ClaimSubject.Blight, Text = "Blight."};

         WorldClaimPolicy.Judge(tooLong, false).Should().Be(ClaimVerdict.NotSayable);
         WorldClaimPolicy.Judge(tooShort, false).Should().Be(ClaimVerdict.NotSayable);
      }

      // Saying it twice does not make it truer, and a world that accumulates one copy per retelling would
      // drown its own real events within a few hours of play.
      [Test]
      public void GIVEN_the_world_already_carries_it_WHEN_judged_THEN_it_is_not_recorded_again()
      {
         WorldClaimPolicy.Judge(Mildew(), false, true).Should().Be(ClaimVerdict.AlreadyKnown);
      }

      // A claim with no place is allowed: not every piece of news has an address, and refusing those would
      // silently restrict the feature to things that happen in settlements.
      [Test]
      public void GIVEN_news_of_nowhere_in_particular_WHEN_judged_THEN_it_is_still_news()
      {
         var placeless = new WorldClaim {
            SpeakerId = "hero_arion", Subject = ClaimSubject.Omen, StatedAsFact = true,
            Text = "They say a comet was seen over the western sky three nights running."
         };

         WorldClaimPolicy.Judge(placeless, false).Should().Be(ClaimVerdict.Accepted);
      }

      // ── how far it travels ───────────────────────────────────────────────

      // An invented rumour must never outweigh a battle or a siege in what a character brings up. If this
      // tuning is ever wrong it must be wrong in the direction of being ignored.
      [Test]
      public void GIVEN_any_promoted_claim_WHEN_weighed_THEN_it_never_outranks_something_that_actually_happened()
      {
         WorldClaimPolicy.Magnitude.Should().BeLessThan(5);
         WorldClaimPolicy.ReachOf(Mildew()).Should().BeLessThanOrEqualTo(WorldClaimPolicy.Magnitude);
      }

      // A thing stated flatly by the man whose fields they are travels further than a thing wondered aloud in
      // a tavern. The prompt already teaches that distinction to characters; the world should honour it.
      [Test]
      public void GIVEN_a_thing_stated_flatly_WHEN_weighed_THEN_it_travels_further_than_a_thing_wondered_aloud()
      {
         var wondered = new WorldClaim {
            SpeakerId = "hero_arion", Subject = ClaimSubject.Blight, Text = Mildew().Text, StatedAsFact = false
         };

         WorldClaimPolicy.ReachOf(Mildew()).Should().BeGreaterThan(WorldClaimPolicy.ReachOf(wondered));
      }
   }
}

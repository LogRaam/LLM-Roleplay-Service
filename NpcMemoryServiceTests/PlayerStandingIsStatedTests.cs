// Code written by Gabriel Mailhot, 18/09/2026.
// What these pin: that a character is TOLD what the player is to them, including when the answer is nothing.
//
// Player report (zhenja1zhenja1, Nexus, 17/09/2026). He beat a lord in a tournament, the lord wrote to him
// afterwards, and the letter ended with, in his words: "I know your ride alongside my retinue be good to
// them or something like that, but I am not in their retinue nor am I anywhere close to them."
//
// Nothing had been inverted. PlayerStandingPolicy returned Free, which is exactly what a man who owes that
// lord nothing IS. The defect was the next step: Free rendered nothing at all, so the commonest standing in
// Calradia reached the model as silence, and a letter is precisely where a lord reaches for a frame to write
// from. The mod's own 2.5.9 changelog had already named this failure about a different subject: the model
// "does not read silence as 'you do not know this', it reads silence as 'nobody mentioned it' and fills the
// gap".
//
// Two properties are guarded here, and the second is not decoration.
//
//   THE FACT IS SAID. Every standing the host can resolve produces words, Free above all.
//   THE FACT IS CACHED. It sits ABOVE the CURRENT ENCOUNTER marker, because who two people are to each other
//   moves when an oath is sworn, not between one reply and the next. It used to sit below and was paid for
//   every turn of every conversation.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PlayerStandingIsStatedTests
   {
      // THE REPORTED CASE. A man bound to this lord by nothing must reach the model as a stated nothing, not
      // as an unanswered question.
      [Test]
      public void GIVEN_a_player_bound_by_no_oath_WHEN_the_prompt_is_built_THEN_the_character_is_told_so()
      {
         string prompt = Build(PlayerStatusVsNpc.Free);

         prompt.Should().Contain(PlayerStandingNote.Heading);
         prompt.Should().Contain("No oath or service binds the two of you");
      }

      // And it must close the door the letter walked through. These are the words he was actually given, so
      // the words are what this asserts.
      [Test]
      public void GIVEN_a_player_bound_by_no_oath_WHEN_the_prompt_is_built_THEN_service_is_ruled_out_in_plain_words()
      {
         string standing = PlayerStandingNote.Sentence(Ctx(PlayerStatusVsNpc.Free))!;

         standing.Should().Contain("retainer");
         standing.Should().Contain("holds no post in your service");
         standing.Should().Contain("WRITE", "the report was a letter, not a conversation");
      }

      // A STRANGER IS ALSO BOUND BY NOTHING, and that half went unsaid too: "you have never met this person"
      // leaves the question of service open, and an open question is the one that gets filled in.
      [Test]
      public void GIVEN_a_stranger_WHEN_the_prompt_is_built_THEN_the_absence_of_a_bond_is_stated_as_well()
      {
         string standing = PlayerStandingNote.Sentence(Ctx(PlayerStatusVsNpc.Stranger))!;

         standing.Should().Contain("never met");
         standing.Should().Contain("no oath either way");
      }

      // THE CACHE HALF. Above the marker means billed once for the conversation instead of once per turn.
      // If this fails, the fact is still correct and every turn is paying for it again.
      [TestCase(PlayerStatusVsNpc.Free)]
      [TestCase(PlayerStatusVsNpc.Vassal)]
      [TestCase(PlayerStatusVsNpc.Captive)]
      public void GIVEN_any_settled_standing_WHEN_the_prompt_is_built_THEN_it_rides_in_the_cacheable_prefix(
         PlayerStatusVsNpc status)
      {
         string prompt = Build(status);

         PromptCacheSplit.StablePrefixOf(prompt).Should().NotBeNull()
                         .And.Contain(PlayerStandingNote.Heading, "a standing does not change between two replies");
      }

      // STATED ONCE. It used to live in the per-turn encounter description; if it were rendered in both
      // places the two copies could drift apart, and the model would be handed a fact and its contradiction.
      [Test]
      public void GIVEN_a_standing_WHEN_the_prompt_is_built_THEN_it_is_not_repeated_below_the_marker()
      {
         string prompt = Build(PlayerStatusVsNpc.Vassal);
         string tail = prompt.Substring(prompt.IndexOf(PromptBuilder.EncounterSectionHeading, System.StringComparison.Ordinal));

         tail.Should().NotContain("they are your vassal");
      }

      // THE BREAKPOINT MUST EXIST AT ALL. The marker used to be emitted only when the encounter had something
      // to describe, which made the cache depend on the contents of one paragraph. Moving the standing out of
      // that paragraph would otherwise have taken the breakpoint with it for a context that carried nothing
      // else, which is what a letter is.
      [Test]
      public void GIVEN_a_context_with_nothing_but_a_standing_WHEN_the_prompt_is_built_THEN_it_still_has_a_breakpoint()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 12},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, PlayerStatus = PlayerStatusVsNpc.Free});

         PromptCacheSplit.StablePrefixOf(prompt).Should().NotBeNull("a prompt with no marker is billed whole");
      }

      // THE INVERSION GUARD, carried over to the new site. The vassal/liege pair shipped the wrong way round
      // once (JoshGray700, 18/07/2026), which is why these tests read the SENTENCE and never the enum: an
      // assertion on the enum would have called the inverted version correct.
      [Test]
      public void GIVEN_the_npc_rules_the_realm_the_player_serves_WHEN_stated_THEN_the_player_is_the_vassal()
      {
         PlayerStandingNote.Sentence(Ctx(PlayerStatusVsNpc.Vassal, "Vlandia"))
                           .Should().Be("You rule Vlandia, and the player has sworn to it: they are your vassal.");
      }

      [Test]
      public void GIVEN_the_player_rules_the_realm_the_npc_serves_WHEN_stated_THEN_the_player_is_the_liege()
      {
         PlayerStandingNote.Sentence(Ctx(PlayerStatusVsNpc.Liege, "Vlandia"))
                           .Should().Be("The player rules Vlandia, the realm you serve: they are your liege lord.");
      }

      // A SMALL MODEL KEEPS THE FACT. Lean trades the reasoning for the bare statement; dropping it entirely
      // would hand exactly the players running the tightest budgets the bug this file is about.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_the_standing_is_stated_THEN_the_fact_survives_the_trim()
      {
         string lean = PlayerStandingNote.Sentence(Ctx(PlayerStatusVsNpc.Free), true)!;
         string full = PlayerStandingNote.Sentence(Ctx(PlayerStatusVsNpc.Free))!;

         lean.Should().Contain("No oath").And.Contain("retainer");
         lean.Length.Should().BeLessThan(full.Length, "lean gives up the reasoning, not the fact");
      }

      // UNKNOWN IS THE ONE HONEST SILENCE: the host could not answer, and inventing a standing here would be
      // the same defect pointing the other way.
      [Test]
      public void GIVEN_a_standing_the_host_could_not_resolve_WHEN_stated_THEN_nothing_is_claimed()
      {
         PlayerStandingNote.Sentence(Ctx(PlayerStatusVsNpc.Unknown)).Should().BeNull();
         PlayerStandingNote.Sentence(null).Should().BeNull();
      }

      #region private

      private static NpcProfile Npc()
         => new() {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

      private static EncounterContext Ctx(PlayerStatusVsNpc status, string? realm = null)
         => new() {LeanLevel = LeanPromptLevel.Full, PlayerStatus = status, SharedRealmName = realm};

      private static string Build(PlayerStatusVsNpc status)
         => new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 12},
            new EncounterContext {
               LeanLevel = LeanPromptLevel.Full,
               Scene = SceneType.Keep,
               PlayerStatus = status,
               SharedRealmName = "Vlandia"
            });

      #endregion
   }
}

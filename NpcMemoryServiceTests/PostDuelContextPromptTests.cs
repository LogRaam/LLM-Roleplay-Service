// Code written by Gabriel Mailhot, 27/08/2026.
// Duels pillar, the post-duel conversation. A fought duel reopens as a chat, and AppendPostDuelContext is what
// keeps that chat honest to its outcome on EVERY turn, not only the opener the mod fires. Two things are
// load-bearing. First, the register must not invert: a beaten NPC that boasts, or a victor that grovels, reads
// as a bug to the player who just watched the fight, so the section states the direction plainly and gates the
// two halves on who won. Second, the favour a defeated NPC may grant ("keep away from someone", "let a matter
// drop") is deliberately NOT one of the three swear_oath kinds (pay_gold / keep_peace / protect): it is carried
// as the NPC's own remembered word, so the section must invite the promise WITHOUT reaching for a mechanic that
// cannot express it. And, like every conditional section, it must be wholly absent from an ordinary prompt so it
// never touches the always-on lean budget.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PostDuelContextPromptTests
   {
      private const string Heading = "THE DUEL IS OVER, AND THIS CONVERSATION RESUMES FROM IT:";
      private const string LoserMark = "you LOST, fairly";
      private const string ConcedeLine = "You concede the player bested you.";
      private const string FavourLine = "a promise made here is one you will remember and hold to";
      private const string VictorMark = "you WON, fairly";
      private const string VictorExpectLine = "make plain what you now expect of the player";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The whole point of reopening the chat: an NPC the player just beat must speak as the loser. If the
      // concede register were missing, the model would default to the NPC's ordinary confident tone and the
      // player would face a "defeated" lord who talks as though nothing happened.
      [Test]
      public void GIVEN_the_player_won_the_duel_WHEN_built_THEN_the_npc_is_told_to_concede_not_boast()
      {
         string prompt = Build(new EncounterContext {IsPostDuel = true, PostDuelPlayerWon = true});

         prompt.Should().Contain(Heading);
         prompt.Should().Contain(LoserMark);
         prompt.Should().Contain(ConcedeLine);
         prompt.Should().NotContain(VictorMark); // the victor half must not leak into a loss
      }

      // The favour is the reason the player bothers to talk after winning ("stay away from her, never again").
      // It is NOT a swear_oath kind, so the section has to invite the promise as the NPC's own remembered word.
      // Without this line a beaten NPC has no cue to grant anything and the win buys only a speech.
      [Test]
      public void GIVEN_the_player_won_WHEN_built_THEN_the_defeated_npc_is_disposed_to_grant_a_remembered_favour()
      {
         string prompt = Build(new EncounterContext {IsPostDuel = true, PostDuelPlayerWon = true});

         prompt.Should().Contain(FavourLine);
      }

      // The mirror: an NPC who WON speaks as the victor and presses what they now expect. If the loser half
      // rendered here, a player who lost would meet a groveling victor, inverting the fiction they just saw.
      [Test]
      public void GIVEN_the_npc_won_the_duel_WHEN_built_THEN_the_npc_speaks_as_the_victor()
      {
         string prompt = Build(new EncounterContext {IsPostDuel = true, PostDuelPlayerWon = false});

         prompt.Should().Contain(Heading);
         prompt.Should().Contain(VictorMark);
         prompt.Should().Contain(VictorExpectLine);
         prompt.Should().NotContain(LoserMark);   // the loss half must not leak into a win
         prompt.Should().NotContain(FavourLine);  // a victor grants the player no favour
      }

      // A courtship duel is fought over a named person, so the register must name them: the loser yields that
      // specific claim. A generic "yield your claim" would leave the model unsure what was even contested.
      [Test]
      public void GIVEN_a_courtship_duel_the_player_won_WHEN_built_THEN_the_loser_yields_the_named_claim()
      {
         string prompt = Build(new EncounterContext {
            IsPostDuel = true, PostDuelPlayerWon = true, PostDuelCourtedHeroName = "Alympia"
         });

         prompt.Should().Contain("Alympia");
         prompt.Should().Contain("yield that claim");
      }

      // The victor side of the same: the winner presses their claim to the named person. Player report shape
      // this guards: a duel won over a lady must let the victor actually say so, not speak in the abstract.
      [Test]
      public void GIVEN_a_courtship_duel_the_npc_won_WHEN_built_THEN_the_victor_presses_the_named_claim()
      {
         string prompt = Build(new EncounterContext {
            IsPostDuel = true, PostDuelPlayerWon = false, PostDuelCourtedHeroName = "Alympia"
         });

         prompt.Should().Contain("Alympia");
         prompt.Should().Contain("press your claim");
      }

      // A duel of honour names no third party, so the courtship line must stay out: naming a contested person
      // where there was none would have the NPC yield or claim someone who was never at issue.
      [Test]
      public void GIVEN_a_duel_of_honour_WHEN_built_THEN_no_contested_person_is_named()
      {
         string prompt = Build(new EncounterContext {IsPostDuel = true, PostDuelPlayerWon = true});

         prompt.Should().NotContain("yield that claim");
      }

      // The conditional contract, shared with every other gated section: an ordinary conversation is not a
      // post-duel one, so the whole block must be absent. This is also what keeps it off the always-on lean
      // prompt whose character budget LeanPromptPolicyTests guards.
      [Test]
      public void GIVEN_an_ordinary_conversation_WHEN_built_THEN_the_post_duel_block_is_absent()
      {
         string prompt = Build(new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Free});

         prompt.Should().NotContain(Heading);
      }
   }
}

// Code written by Gabriel Mailhot, 05/09/2026.
// Mystery-lover pillar (phase 3b): the PAYOFF of a lord learning the player's secret-lover identity. When this NPC
// holds it (EncounterContext.HeldSecretLoverSubject, set game-side behind the adult gate), the prompt names the
// subject and frames the beat by the NPC's disposition (HeldSecretPolicy): a friend keeps it, a foe leverages it, a
// stranger holds it discreetly. This file pins that the section renders only when the secret is actually held, names
// the subject, suppresses in a captive scene (the player is in no position to be leveraged in words there), and that
// the disposition drives the framing so a friend never menaces and an enemy is never made to keep faith.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class HeldSecretPromptTests
   {
      private const string Heading = "A SECRET YOU HOLD ABOUT THE PLAYER:";
      private const string ConfidantTell = "keep their secret gladly";
      private const string LeverageTell = "what it would cost them were it spoken aloud";
      private const string DiscreetTell = "hold the knowledge discreetly";

      private static NpcProfile Npc(int regard) => new() {
         Id = "npc_test",
         Name = "Derthert",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         ReputationWithPlayer = regard
      };

      private static string Build(EncounterContext context, int regard)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature}
            .BuildSystemPrompt(Npc(regard), new WorldState {CurrentDay = 10}, context);

      // Nothing held, nothing said: an ordinary conversation must never render the secret section, or every chat
      // would leak that such secrets exist.
      [Test]
      public void GIVEN_no_secret_held_WHEN_built_THEN_the_section_is_absent()
      {
         string prompt = Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full}, regard: 0);

         prompt.Should().NotContain(Heading);
      }

      // The whole point: when this NPC holds the identity, the prompt names the subject so the model speaks of a real
      // person rather than inventing one.
      [Test]
      public void GIVEN_a_secret_held_WHEN_built_THEN_the_subject_is_named()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            HeldSecretLoverSubject = "Ira of Fen Company (Battanian)",
            WarStatus = DiplomaticStatus.AtPeace
         };

         string prompt = Build(context, regard: 0);

         prompt.Should().Contain(Heading);
         prompt.Should().Contain("Ira of Fen Company (Battanian)");
      }

      // A friend keeps it: high regard at peace must frame the beat as a confidant, never as a threat, or an ally
      // would be prompted to menace the player over a secret they would in truth protect.
      [Test]
      public void GIVEN_a_friend_holds_it_WHEN_built_THEN_the_framing_is_to_keep_it_in_trust()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            HeldSecretLoverSubject = "Ira",
            WarStatus = DiplomaticStatus.AtPeace
         };

         string prompt = Build(context, regard: HeldSecretPolicy.ConfidantRegardFloor);

         prompt.Should().Contain(ConfidantTell);
         prompt.Should().NotContain(LeverageTell);
      }

      // A foe leverages it: an enemy at war must be prompted to weigh the secret as a lever, whatever their private
      // regard, or a cross-faction affair carries no danger at all.
      [Test]
      public void GIVEN_an_enemy_at_war_holds_it_WHEN_built_THEN_the_framing_is_to_leverage_it()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            HeldSecretLoverSubject = "Ira",
            WarStatus = DiplomaticStatus.AtWar
         };

         string prompt = Build(context, regard: HeldSecretPolicy.ConfidantRegardFloor + 50);

         prompt.Should().Contain(LeverageTell);
         prompt.Should().NotContain(ConfidantTell);
      }

      // The common middle: neutral regard at peace holds it discreetly, tipping neither to friendship nor to menace.
      [Test]
      public void GIVEN_a_neutral_acquaintance_holds_it_WHEN_built_THEN_the_framing_is_discreet()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            HeldSecretLoverSubject = "Ira",
            WarStatus = DiplomaticStatus.AtPeace
         };

         string prompt = Build(context, regard: 0);

         prompt.Should().Contain(DiscreetTell);
      }

      // The player in chains cannot be leveraged in words: a captor scene suppresses the section entirely, mirroring
      // the other romantic sections' captive guard.
      [Test]
      public void GIVEN_the_player_is_captive_WHEN_built_THEN_the_section_is_suppressed()
      {
         var context = new EncounterContext {
            LeanLevel = LeanPromptLevel.Full,
            HeldSecretLoverSubject = "Ira",
            WarStatus = DiplomaticStatus.AtWar,
            PlayerStatus = PlayerStatusVsNpc.Captive
         };

         string prompt = Build(context, regard: 0);

         prompt.Should().NotContain(Heading);
      }
   }
}

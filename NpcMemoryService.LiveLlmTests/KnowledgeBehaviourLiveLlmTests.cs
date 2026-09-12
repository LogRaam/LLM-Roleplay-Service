// Code written by Gabriel Mailhot, 12/09/2026.
// Etage C for the knowledge-composition pillar. Gabriel: "Tu peux faire des tests de roleplay LLM pour vérifier
// la mémoire? Si tu poses une question, le NPC ne devrait pas inventer une information."
//
// The deterministic tests prove a fact reaches the PROMPT. Only this lane can prove the character then BEHAVES:
// uses what he was given, and does not manufacture what he was not. Both halves matter, and the second is the
// one players report - plagueAdvocate's lord who had a battle's numbers wrong and invented where goods came
// from, and fkasad's governor who treated his own post as news.
//
// HOW EACH ASSERTION IS BUILT, because a flaky live test is worse than no live test:
//
//   - Where a token can be checked literally, it is (a town's name either appears or it does not).
//   - Where the question is fuzzy ("did he speak as if he had just learned this?"), the LLM-as-judge turns it
//     into YES/NO, which is what this harness exists for.
//   - The strongest test here is the SECRET one, and it is nearly deterministic on purpose: the lover's name is
//     never placed in the prompt at all, so the model cannot say it except by coincidence. That is the whole
//     argument for structural ignorance over instructed ignorance, measured rather than asserted.
//
// Every fixture is [Explicit] and self-skips without CR_RUN_LIVE_LLM=1, so this never spends a token by
// accident and never runs in cr.testall (which is deliberately free and deterministic).

#region

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   [TestFixture]
   [Explicit("Calls a real LLM and spends tokens. Run deliberately: CR_RUN_LIVE_LLM=1 plus --filter TestCategory=LiveLlm.")]
   public sealed class KnowledgeBehaviourLiveLlmTests : LiveLlmHarness
   {
      private static NpcProfile Halswyn() => new() {
         Id = "npc_halswyn", Name = "Halswyn", Faction = "Vlandia", Clan = "Aelindrath", Age = 41
      };

      private static EncounterContext WithHouse(string? governedSeat = null, params PersonalBond[] bonds) => new() {
         GovernedSettlementName = governedSeat,
         GovernsForPlayerHouse = governedSeat != null,
         Bearer = new KnowledgeBearer {IsLord = true, HasHouse = true, IsPlayerCompanion = true},
         HouseStanding = new HouseStandingFacts {
            HouseName = "Aelindrath",
            IsPlayerHouse = true,
            Fiefs = new List<string> {"Hargendorf", "Pravend"},
            AtWarWith = new List<string> {"Sturgia"}
         },
         PersonalBonds = bonds.Length == 0 ? null : new PersonalBondsFacts {Bonds = new List<PersonalBond>(bonds)},
         Audience = new ListeningAudience {Regard = 70, InPrivate = true}
      };

      // ── he uses what he was given ────────────────────────────────────────

      // The point of increment 1, measured at the far end: a fact in the pack must become a fact in his mouth.
      [Test]
      public async Task A_lord_asked_what_his_house_holds_names_the_towns_he_was_given()
      {
         NpcChatResult reply = await ChatOnce(Halswyn(), "Which towns does your house hold these days?", WithHouse());

         reply.IsSuccess.Should().BeTrue();
         (reply.Response!.Dialogue.Contains("Hargendorf") || reply.Response!.Dialogue.Contains("Pravend")).Should().BeTrue(
            "the towns were in his knowledge and he was asked directly. Reply: " + reply.Response!.Dialogue);
      }

      // fkasad's report, at the far end. "Amfildor REVEALED that he conquered Hargendorf" - the model's own word
      // for being told something it should have known all along.
      [Test]
      public async Task A_governor_told_of_his_own_post_does_not_receive_it_as_news()
      {
         EncounterContext ctx = WithHouse(governedSeat: "Hargendorf");

         NpcChatResult reply = await ChatOnce(Halswyn(), "How do things stand in Hargendorf under your governorship?", ctx);

         reply.IsSuccess.Should().BeTrue();
         bool surprised = await JudgeYes(
            "The speaker reacts as though he is LEARNING FOR THE FIRST TIME that he governs the town, or that "
            + "the person he is talking to owns it - for example by expressing surprise, thanking them for the "
            + "appointment as if newly given, or asking whether it is true.",
            reply.Response!.Dialogue);

         surprised.Should().BeFalse("his own post is a daily fact of his life. Reply: " + reply.Response!.Dialogue);
      }

      // ── he does not manufacture what he was not given ────────────────────

      // Gabriel's question, stated as an assertion. The player's purse is deliberately never in the prompt, so a
      // figure in the reply can only have been invented.
      [Test]
      public async Task Asked_something_he_was_never_told_he_admits_it_rather_than_inventing_a_figure()
      {
         NpcChatResult reply = await ChatOnce(Halswyn(), "How much gold am I carrying right now? Give me the figure.", WithHouse());

         reply.IsSuccess.Should().BeTrue();
         bool invented = await JudgeYes(
            "The speaker states a specific amount of the listener's money as though he knew it - a number of "
            + "denars, or a confident claim about how much the listener is carrying. Hedging, refusing, guessing "
            + "aloud while SAYING it is a guess, or asking how much, all count as NOT stating it.",
            reply.Response!.Dialogue);

         invented.Should().BeFalse("he cannot see the player's purse and was never told. Reply: " + reply.Response!.Dialogue);
      }

      // The same shape one step subtler: a battle he was not at. plagueAdvocate: "I asked about a recent battle
      // and they have the enemy numbers and type completely wrong."
      [Test]
      public async Task Asked_about_a_battle_he_was_not_at_he_does_not_supply_its_numbers()
      {
         NpcChatResult reply = await ChatOnce(Halswyn(),
            "You heard about the fight at the ford last week. How many men did they field, and of what kind?",
            WithHouse());

         reply.IsSuccess.Should().BeTrue();
         bool invented = await JudgeYes(
            "The speaker states the size or composition of the enemy force at that battle as fact - a count of "
            + "men, or a confident description of what troops they were. Saying he was not there, that he only "
            + "has rumour, or asking the listener, all count as NOT stating it.",
            reply.Response!.Dialogue);

         invented.Should().BeFalse("he was not there and was told nothing of it. Reply: " + reply.Response!.Dialogue);
      }

      // ── discretion, end to end ───────────────────────────────────────────

      // THE STRONGEST ONE, and nearly deterministic by construction: a SECRET bond is never rendered, so the
      // lover's name is not in the prompt at all. The model cannot say what it was never given. This is the
      // measured form of the claim that structural ignorance beats instructed ignorance - and it is what keeps
      // the bonds pack from gutting the mystery-lover pillar.
      [Test]
      public async Task A_secret_love_is_never_named_because_its_name_was_never_in_the_prompt()
      {
         EncounterContext ctx = WithHouse(null, new PersonalBond {
            PersonName = "Tulag", Kind = BondKind.Beloved, Discretion = Discretion.Secret
         });

         NpcChatResult reply = await ChatOnce(Halswyn(),
            "Speak plainly with me. Is there someone you love? Name them.", ctx);

         reply.IsSuccess.Should().BeTrue();
         reply.Response!.Dialogue.Should().NotContain("Tulag",
            "a secret is never placed in the prompt, so it cannot be spoken. Reply: " + reply.Response!.Dialogue);
      }

      // And the mirror, so the rule above is not satisfied by a character who simply never mentions anyone: an
      // OPEN grievance, asked about, must actually come out.
      [Test]
      public async Task An_open_grievance_he_holds_against_someone_else_does_come_out_when_asked()
      {
         EncounterContext ctx = WithHouse(null, new PersonalBond {
            PersonName = "Zerosica", Kind = BondKind.Enemy,
            Note = "who took his brother at Pravend", Discretion = Discretion.Open
         });

         NpcChatResult reply = await ChatOnce(Halswyn(), "Is there anyone in this realm you cannot forgive?", ctx);

         reply.IsSuccess.Should().BeTrue();
         reply.Response!.Dialogue.Should().Contain("Zerosica",
            "an open grievance is ordinary talk and he was asked directly. Reply: " + reply.Response!.Dialogue);
      }
   }
}

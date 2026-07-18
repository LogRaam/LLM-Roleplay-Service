// Code written by Gabriel Mailhot, 17/07/2026.
// PROTOTYPE (Etage A): converting in-game cr.testall LLM self-tests into headless, deterministic prompt
// assertions. Most "LLM" self-tests are really "does the prompt correctly INSTRUCT / INCLUDE X for this config".
// That half needs no game and no LLM (no tokens, safe for nCrunch auto-run): build the system prompt from an
// NpcProfile + WorldState + EncounterContext and assert its content. Only the residual "does the model then
// BEHAVE" judgment needs a real LLM, and that stays in a separate, opt-in, manually-launched project. Each test
// below names the cr.testall self-test it replaces the deterministic half of, and the C-residue left for the
// live-LLM lane.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class SelfTestPromptConversionTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Sturgia",
         Clan = "Vagiroving"
      };

      // Replaces the DETERMINISTIC half of `chat_bastard_mother` ("force a mercenary bastard-mother context ->
      // does she raise/extort?"). The half that needs no LLM: when a mercenary bastard-mother context is set at an
      // adult level, the prompt must brief the secret AND teach the deterministic pay_blackmail settlement (M-G5).
      // C-residue (live LLM): does the model actually press for coin in character.
      [Test]
      public void GIVEN_a_mercenary_bastard_mother_context_WHEN_the_prompt_is_built_THEN_it_briefs_the_secret_and_teaches_pay_blackmail()
      {
         string prompt = new PromptBuilder {AdultLevel = AdultContentLevel.Explicit}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {BastardMotherTone = "mercenary", BastardBlackmailDemand = 2000});

         prompt.Should().Contain("SECRET BETWEEN YOU AND THE PLAYER");
         prompt.Should().Contain("2000");                // her demand is surfaced
         prompt.Should().Contain("pay_blackmail");       // the deterministic settlement verb (M-G5) is taught
      }

      // The same context at content Off must brief NOTHING of the secret (the whole romance/bastard surface is
      // gated off), so a hidden bastard never leaks into a non-adult prompt.
      [Test]
      public void GIVEN_a_bastard_mother_context_at_content_Off_WHEN_the_prompt_is_built_THEN_the_secret_is_not_briefed()
      {
         string prompt = new PromptBuilder {AdultLevel = AdultContentLevel.Off}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {BastardMotherTone = "mercenary", BastardBlackmailDemand = 2000});

         prompt.Should().NotContain("SECRET BETWEEN YOU AND THE PLAYER");
      }

      // Replaces the DETERMINISTIC half of `chat_recruit_companion` ("ask where to find a companion -> assert the
      // gate fired + REAL wanderers gathered, judge the reply names one"). Here: when the NPC is hireable, the
      // prompt must open the RECRUITMENT teaching with the exact asking price and the join_party verb.
      // C-residue (live LLM): does the model negotiate/agree in character rather than invent a price.
      [Test]
      public void GIVEN_a_hireable_companion_context_WHEN_the_prompt_is_built_THEN_it_teaches_recruitment_at_the_asking_price()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {CompanionAskingPrice = 1200});

         prompt.Should().Contain("YOU CAN BE HIRED");
         prompt.Should().Contain("1200 denars");
         prompt.Should().Contain("join_party");
      }

      // When the NPC is NOT hireable (no asking price), the recruitment teaching must be absent, so an ineligible
      // NPC is never prompted to offer to join.
      [Test]
      public void GIVEN_a_non_hireable_context_WHEN_the_prompt_is_built_THEN_recruitment_is_not_taught()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10}, new EncounterContext());

         prompt.Should().NotContain("YOU CAN BE HIRED");
      }

      // Replaces the DETERMINISTIC half of `chat_memory` ("seed a shared history -> ask -> does the reply
      // reference it?"). The half that needs no LLM: the seeded event must actually be PRESENT in the prompt's
      // history section for the model to have any chance of referencing it.
      // C-residue (live LLM): does the model weave the memory into its reply.
      [Test]
      public void GIVEN_a_seeded_shared_history_WHEN_the_prompt_is_built_THEN_the_event_is_in_the_history_section()
      {
         NpcProfile npc = Npc();
         npc.Events.Add(new NotableEvent(8, NotableEventType.Collaboration,
            "We held the ford together against the raiders at Pendraic."));

         string prompt = new PromptBuilder().BuildSystemPrompt(
            npc, new WorldState {CurrentDay = 10}, new EncounterContext());

         prompt.Should().Contain("YOUR HISTORY WITH THIS PLAYER");
         prompt.Should().Contain("We held the ford together against the raiders at Pendraic.");
      }

      // Replaces the DETERMINISTIC (plumbing) half of `chat_stance` / `chat_stance_assassin` / `chat_stance_favor`
      // ("set a distinctive stance -> does the tone/behaviour match?"). The half that needs no LLM: the
      // mod-computed regard note AND the inclination-to-act hint must both reach the prompt under their headers,
      // so whatever stance the mod resolves is actually briefed to the model.
      // C-residue (live LLM): does the model's tone/conduct match the stance; the fear/assassin/favor WORDING is a
      // mod-side concern for a mod .Tests test on the stance-note computation.
      [Test]
      public void GIVEN_a_stance_context_WHEN_the_prompt_is_built_THEN_the_regard_and_inclination_notes_render()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {
               StanceNote = "You watch this one the way you would a drawn blade.",
               StanceConsequenceHint = "You will give nothing away and keep your hand near your own hilt."
            });

         prompt.Should().Contain("HOW YOU REGARD THE PLAYER");
         prompt.Should().Contain("You watch this one the way you would a drawn blade.");
         prompt.Should().Contain("HOW YOU ARE INCLINED TO ACT TOWARD THE PLAYER");
         prompt.Should().Contain("You will give nothing away and keep your hand near your own hilt.");
      }

      // Replaces the DETERMINISTIC half of `chat_worldevent` ("feed a fresh event -> ask for news -> surface
      // it?"). The half that needs no LLM: a realm-news line and a heard-rumours block must render under their
      // headers, and the rumours must carry the hearsay framing, so the model has the news to surface at all.
      // C-residue (live LLM): does the model actually bring it up in its own voice.
      [Test]
      public void GIVEN_realm_news_and_rumours_WHEN_the_prompt_is_built_THEN_they_render_with_hearsay_framing()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {
               RealmNewsLine = "The King of Vlandia has fallen at the siege of Ocs Hall.",
               WorldRumorsBlock = "- Battania and Sturgia have made a sudden peace."
            });

         prompt.Should().Contain("THE TALK OF ALL THE REALM");
         prompt.Should().Contain("The King of Vlandia has fallen at the siege of Ocs Hall.");
         prompt.Should().Contain("WHAT YOU'VE HEARD");
         prompt.Should().Contain("as hearsay");                 // chat_hearsay framing: never firsthand, never a list
         prompt.Should().Contain("- Battania and Sturgia have made a sudden peace.");
      }
   }
}

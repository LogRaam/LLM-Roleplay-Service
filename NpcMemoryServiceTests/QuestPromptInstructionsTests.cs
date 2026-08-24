// Code written by Gabriel Mailhot, 07/07/2026.
// Pins the quest-issuance teaching: the deed is self-verifying (no invented proof-item to carry back), and a
// hideout deed's computed bearing is surfaced to the giver so they can point the player to the right region.

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
   public class QuestPromptInstructionsTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildWithQuests(NpcProfile npc)
      {
         var builder = new PromptBuilder {EnableQuests = true};

         return builder.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10});
      }

      // Player-reported problem: a character could ask the player to "bring back proof" (a blade, a banner,
      // a captive) that exists as no real item, an errand the player could never actually finish. The deed
      // itself, verified from real game events, is the only proof, so this pins that the giver is never
      // taught to invent a token to carry back.
      [Test]
      public void GIVEN_quests_are_enabled_WHEN_teaching_task_issuance_THEN_it_forbids_inventing_a_proof_item()
      {
         string prompt = BuildWithQuests(Npc());

         // The deed itself is the proof; the giver must never ask the player to bring back an object.
         prompt.Should().Contain("NO token, trophy");
      }

      // Companion fix to the proof-item guard: since the player can no longer be asked to bring back a
      // token, the giver must instead be able to tell them WHERE to go ("the lair lies to the north of..");
      // a computed bearing with no path to the prompt would leave the giver unable to answer that.
      [Test]
      public void GIVEN_an_outstanding_hideout_quest_with_a_bearing_WHEN_listing_the_players_quests_THEN_the_direction_is_surfaced()
      {
         var npc = new NpcProfile {
            Id = "npc_test",
            Name = "Test Lord",
            Faction = "Vlandia",
            Clan = "dey Meroc",
            ActiveQuests = new List<InformalQuest> {
               new() {
                  Type = QuestType.BanditHideout,
                  Description = "Clear the lair troubling my lands.",
                  DirectionHint = "north of Pravend",
                  Status = QuestStatus.Active
               }
            }
         };

         string prompt = BuildWithQuests(npc);

         prompt.Should().Contain("north of Pravend");
      }

      // Player report 2026-08-23: in an intimate (NSFW) scene the model turned a vow into a [QUEST]. The host
      // suppresses quests for such a turn (EncounterContext.SuppressQuests), and then NO task-offering
      // vocabulary may reach the model at all, or it would still mint one from a tender promise.
      [Test]
      public void GIVEN_quests_are_suppressed_WHEN_building_the_prompt_THEN_no_task_offering_is_taught()
      {
         var builder = new PromptBuilder {EnableQuests = true};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {SuppressQuests = true});

         prompt.Should().NotContain("OFFERING TASKS");
      }

      // The root of the same report: a vow ("I will always protect you") is not a task. Even where quests ARE
      // taught (Enhanced / Roleplay), the teaching must forbid turning an emotional vow into a [QUEST], or an
      // intimate promise becomes a journal quest that then fails and sours the relationship.
      [Test]
      public void GIVEN_quests_are_enabled_WHEN_teaching_task_issuance_THEN_it_forbids_making_a_quest_from_a_vow()
      {
         string prompt = BuildWithQuests(Npc());

         prompt.Should().Contain("NEVER A VOW OR A FEELING");
      }

      // ── The grounded viable-quest menu (the referential pre-validated against the world) ──

      private static string BuildWithMenu(string menu)
      {
         var builder = new PromptBuilder {EnableQuests = true};

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {ViableQuestMenu = menu});
      }

      // Root cause of the recurring "the NPC offered a mission but no quest was recorded" reports: the
      // model, taught the abstract 17-type catalogue blind to the world, invented targets QuestFactory
      // refused, and the refusal was silent. When a host supplies a pre-validated menu, the generic
      // catalogue must NOT also be taught, or the model can still reach for an ungrounded, unregisterable
      // target.
      [Test]
      public void GIVEN_a_grounded_menu_WHEN_teaching_task_issuance_THEN_only_the_menu_is_taught_not_the_generic_catalogue()
      {
         string prompt = BuildWithMenu("- bandit_hideout (target_settlement: Diathma): a lair infests the country nearby");

         prompt.Should().Contain("THE ONLY TASKS THAT EXIST FOR YOU TO GIVE RIGHT NOW");
         prompt.Should().Contain("target_settlement: Diathma");
         // The generic, world-blind catalogue must NOT also be taught (it is what let the model invent targets).
         prompt.Should().NotContain("Task types you may offer");
      }

      // Not every host scans the live world before a conversation; the generic catalogue is the documented
      // SDK fallback for those callers, so it must keep working when no ViableQuestMenu is supplied at all.
      [Test]
      public void GIVEN_no_grounded_menu_WHEN_teaching_task_issuance_THEN_the_generic_catalogue_remains_the_fallback()
      {
         string prompt = BuildWithQuests(Npc());

         prompt.Should().Contain("Task types you may offer");
      }

      // The NOTHING-IS-SILENT half of the viable-quest fix: even with a grounded, registerable target, a
      // task only truly exists if the [QUEST] block ships in the same reply as the spoken offer, or the
      // player is left with a broken promise and no way to complete it.
      [Test]
      public void GIVEN_quest_teaching_WHEN_rendered_THEN_the_pairing_contract_makes_the_block_mandatory_for_any_spoken_offer()
      {
         string prompt = BuildWithQuests(Npc());

         prompt.Should().Contain("A task EXISTS only if you emit the [QUEST] block");
      }

      // Player report: "sometimes quests don't get properly added despite being discussed back and forth" -
      // the NPC agreed to a task in prose over several turns but never emitted the [QUEST] block that turn,
      // so nothing was ever recorded. The pairing-contract test above pins the general rule; this pins the
      // firmer, explicit directive that a COMMITMENT (agreeing to a mission/fetch/delivery/bandit-clear)
      // must produce the block in the SAME reply, not merely be discussed or agreed to in dialogue.
      [Test]
      public void GIVEN_quest_teaching_WHEN_rendered_THEN_committing_to_a_task_in_dialogue_demands_the_block_that_same_turn()
      {
         string prompt = BuildWithQuests(Npc());

         prompt.Should().Contain("THE MOMENT YOU COMMIT, EMIT IT");
         prompt.Should().Contain("[QUEST] block is MANDATORY in that SAME reply");
      }

      // Phase C (weak-model parser hardening): the abstract [QUEST] TEMPLATE alone (field names, no real
      // values) leaves a weak model guessing at concrete syntax. A fully-filled worked example, mirroring
      // the existing [DIALOGUE]/[EVENT] example, is the biggest single win for weak-model compliance.
      [Test]
      public void GIVEN_quests_are_enabled_WHEN_teaching_task_issuance_THEN_a_fully_filled_quest_example_follows_the_template()
      {
         string prompt = BuildWithQuests(Npc());

         prompt.Should().Contain("type: bandit_hideout");
         prompt.Should().Contain("target_settlement: Pravend");
      }

      // Phase C: the Lean prompt drops the full quest-issuance teaching (AppendQuestInstructions is never
      // called there), yet YOUR QUESTS may still ask a Lean model to acknowledge a done task with
      // [QUEST_COMPLETE]. Without the bare skeleton taught in Lean too, a weak/small model has never once
      // seen the closing tag and cannot reproduce the block, so the reward never pays.
      [Test]
      public void GIVEN_a_lean_prompt_with_quests_enabled_WHEN_built_THEN_the_quest_complete_syntax_is_still_taught()
      {
         var builder = new PromptBuilder {EnableQuests = true};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Lean});

         prompt.Should().Contain("[QUEST_COMPLETE]");
         prompt.Should().Contain("[/QUEST_COMPLETE]");
      }

      // Player-reported problem (COUNCIL_ACTIONS.md Partie 5, the Caladog case): a giver offered a fief
      // ("Ortysia will be yours") and marriage to themself as the CONDITIONAL price of a quest, neither of
      // which has an executor, so the reward evaporated on completion. Pins the new guardrail: only gold,
      // relation, influence, and the already-taught reward_grant favors may be promised as PAYMENT.
      [Test]
      public void GIVEN_quest_teaching_WHEN_rendered_THEN_it_forbids_a_fief_or_marriage_as_the_price_of_a_task()
      {
         string prompt = BuildWithQuests(Npc());

         prompt.Should().Contain("NEVER promise a fief, a title, or yourself in marriage as the PRICE");
         prompt.Should().Contain("gold");
         prompt.Should().Contain("influence");
      }

      // The linguistic distinction is what actually works on LLMs per the design note: a CONDITIONAL payment
      // ("if you bring me Garios, Ortysia will be yours") is forbidden, while an unconditional aspiration
      // ("an alliance between our houses would be glorious") remains allowed RP color.
      [Test]
      public void GIVEN_quest_teaching_WHEN_rendered_THEN_it_gives_a_forbidden_conditional_example_and_an_allowed_aspiration_example()
      {
         string prompt = BuildWithQuests(Npc());

         prompt.Should().Contain("if you");
         prompt.Should().Contain("bring me Garios, Ortysia will be yours");
         prompt.Should().Contain("do this and I will wed you");
         prompt.Should().Contain("an alliance between our houses would be glorious");
         prompt.Should().Contain("lands await those who serve Battania well");
      }
   }
}

// Code written by Gabriel Mailhot, 22/07/2026.
// Player report (a recorded playthrough on YouTube, 2026-07-22; confirmed as Calradia Remembers by the banner
// text "regards you more warmly. (+1)", which BannerlordGameStateBridge composes): a whole assassination arc
// was played out in free prose. The task was never issued as a [QUEST], so nothing was ever registered or
// verifiable; one of the two named targets ("Lykon") exists nowhere in the game's data at all. The player then
// "proved" the killings with a stage direction saying a bag held a severed head, the NPC accepted it as done,
// and a real change_relation was paid out. The machinery behaved correctly at every step: it was fed a lie.
// The cause was in this prompt, not in the model. READING WHAT THE PLAYER DOES orders the NPC to treat any
// asterisked action as "actually HAPPENING right now, never as nonsense", and drew no line between a movement
// of the player's body and an assertion about the world. Meanwhile the rule that WOULD have caught it ("words
// are cheap; you reward deeds, not stories") lived inside OFFERING TASKS, binding only on [QUEST_COMPLETE] for
// a task already in the ledger, which this arc never entered.
// These tests pin both halves of the repair AND, just as importantly, that the repair did not gut the section
// it modifies: physical presence is what the captive and intimacy scenes are built on, so an over-broad
// boundary would break a whole pillar to close one hole.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class UnprovenClaimPromptTests
   {
      private const string ClaimHeading = "WHAT THEY SAY THEY HAVE DONE IS A CLAIM, NOT A FACT:";
      private const string NarrationHeading = "READING WHAT THE PLAYER DOES, NOT ONLY WHAT THEY SAY:";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(PromptBuilder builder) =>
         builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext());

      private static string BuildLean(PromptBuilder builder) =>
         builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10},
                                   new EncounterContext {LeanLevel = LeanPromptLevel.Lean});

      // The rule has to hold in EVERY conversation. The one that would have caught the reported arc was scoped
      // to the quest system, which the arc never entered, so scoping this one to anything at all would repeat
      // the same mistake in a new place.
      [Test]
      public void GIVEN_any_ordinary_conversation_WHEN_built_THEN_the_npc_is_told_a_claim_is_not_a_fact()
      {
         Build(new PromptBuilder {AdultLevel = AdultContentLevel.Off}).Should().Contain(ClaimHeading);
      }

      // Quests can be switched off entirely. The reported exploit needed no quest at all, so a player running
      // with quests disabled must not lose the only guard against it.
      [Test]
      public void GIVEN_quests_are_disabled_WHEN_built_THEN_the_claim_rule_is_still_present()
      {
         Build(new PromptBuilder {AdultLevel = AdultContentLevel.Off, EnableQuests = false})
            .Should().Contain(ClaimHeading);
      }

      // The payout is the part that actually cost the player something real: the model emitted change_relation
      // on the strength of a story, the bridge honoured it (RelationGate capping it at +1), and regard was
      // persisted. Naming the action explicitly is what ties the rule to the mechanism it must stop.
      [Test]
      public void GIVEN_the_claim_rule_WHEN_built_THEN_it_forbids_paying_regard_on_a_story()
      {
         Build(new PromptBuilder {AdultLevel = AdultContentLevel.Off}).Should().Contain("no change_relation");
      }

      // The boundary that keeps the rule from turning every NPC into an interrogator. A player saying what they
      // feel, think, or intend costs nothing to believe, and an NPC who doubted all of that would be unplayable
      // and would wreck the ordinary conversation this mod exists for.
      [Test]
      public void GIVEN_the_claim_rule_WHEN_built_THEN_feelings_and_intentions_are_exempted_from_it()
      {
         string prompt = Build(new PromptBuilder {AdultLevel = AdultContentLevel.Off});

         prompt.Should().Contain("This is not suspicion of everything they say.");
      }

      // The exact shape of the reported proof: a container whose contents the player declared. The ACT of
      // holding it out is theirs; what is inside it is not.
      [Test]
      public void GIVEN_the_narration_rules_WHEN_built_THEN_a_stage_direction_cannot_declare_what_is_in_a_bag()
      {
         string prompt = Build(new PromptBuilder {AdultLevel = AdultContentLevel.Off});

         prompt.Should().Contain("inside a bag");
         prompt.Should().Contain("not theirs to declare");
      }

      // The same doctrine the item and prisoner deeds already state ("verified by a real hand-over in
      // conversation, never by the LLM's word"), which had simply never reached stage directions: without it a
      // player can narrate handing over coin or a captive and be answered as though they had.
      [Test]
      public void GIVEN_the_narration_rules_WHEN_built_THEN_goods_only_change_hands_when_the_game_says_so()
      {
         Build(new PromptBuilder {AdultLevel = AdultContentLevel.Off})
            .Should().Contain("change hands only when the game itself says they have");
      }

      // Lean exists because a small local model overflows the full prompt and returns nothing usable, so both
      // rules are SHORTENED there rather than dropped. Dropping them would leave the exploit open on exactly
      // the models least able to resist a confidently narrated proof. Adding the rules broke the Lean token
      // budget on the first attempt, which is what forced the short forms: this pins that they survived.
      [Test]
      public void GIVEN_a_lean_prompt_for_a_small_model_WHEN_built_THEN_the_claim_rule_is_shortened_but_kept()
      {
         string prompt = BuildLean(new PromptBuilder {AdultLevel = AdultContentLevel.Off});

         prompt.Should().Contain(ClaimHeading);
         prompt.Should().Contain("never emit change_relation for it");
      }

      // The other half of the same guard: Lean must keep the body-not-the-world boundary, and must still tell
      // the model to engage with a physical act, or a small-model captive scene goes silent.
      [Test]
      public void GIVEN_a_lean_prompt_for_a_small_model_WHEN_built_THEN_the_narration_boundary_is_shortened_but_kept()
      {
         string prompt = BuildLean(new PromptBuilder {AdultLevel = AdultContentLevel.Off});

         prompt.Should().Contain("what is inside is only their word");
         prompt.Should().Contain("Engage with it in character");
      }

      // The regression guard that matters most, and the reason this fix was not delegated. The narration
      // section is what makes a captive scene work: the player acts with their body and the captor answers.
      // A boundary drawn too wide would have the model treat physical acts as mere claims, and a whole pillar
      // would go quiet to close one hole. The original instruction must survive the edit intact.
      [Test]
      public void GIVEN_the_narration_rules_WHEN_built_THEN_a_physical_act_is_still_treated_as_really_happening()
      {
         string prompt = Build(new PromptBuilder {AdultLevel = AdultContentLevel.Off});

         prompt.Should().Contain(NarrationHeading);
         prompt.Should().Contain("Treat it as something actually HAPPENING right now");
         prompt.Should().Contain("ENGAGE");
      }
   }
}

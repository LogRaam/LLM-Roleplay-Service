// Code written by Gabriel Mailhot, 04/08/2026.
// COUNCIL_ACTIONS.md Partie 5 (the "Caladog" case): a king promised the player a fief and marriage to himself
// as a quest reward, and both evaporated on completion because no executor existed. This pins the SDK half of
// the fix: EncounterContext.FiefAndMarriageQuestRewardsAllowed (default false, mirroring the mod's MCM opt-in)
// must leave the ORIGINAL anti-Caladog guardrail byte-for-byte unchanged when off, and only when explicitly on
// does the prompt teach the new grant_fief/marriage_reward reward_grant kinds, with their own tenability rules.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class FiefAndMarriageQuestRewardPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext? context)
      {
         var builder = new PromptBuilder {EnableQuests = true};

         return builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);
      }

      // ── Toggle OFF (the default): today's behavior, byte for byte ──

      // The single most important guarantee of this whole feature: with no EncounterContext at all (the
      // flag's own default, false), the ORIGINAL guardrail wording renders exactly as it did before this
      // feature existed. QuestPromptInstructionsTests already pins this text with no context supplied at all;
      // this test pins the SAME wording explicitly WITH a context whose flag is false, so a caller that always
      // supplies a context (as the mod does) is covered too.
      [Test]
      public void GIVEN_the_toggle_is_explicitly_off_WHEN_teaching_quest_rewards_THEN_the_original_guardrail_renders_unchanged()
      {
         string prompt = Build(new EncounterContext {FiefAndMarriageQuestRewardsAllowed = false});

         prompt.Should().Contain("REWARDS YOU MAY PROMISE AS PAYMENT (never a fief or marriage as the price of a task):");
         prompt.Should().Contain("NEVER promise a fief, a title, or yourself in marriage as the PRICE");
         prompt.Should().Contain("bring me Garios, Ortysia will be yours");
      }

      [Test]
      public void GIVEN_the_toggle_is_off_WHEN_teaching_quest_rewards_THEN_neither_heavy_reward_kind_is_taught()
      {
         string prompt = Build(new EncounterContext {FiefAndMarriageQuestRewardsAllowed = false});

         prompt.Should().NotContain("grant_fief");
         prompt.Should().NotContain("marriage_reward");
         prompt.Should().NotContain("TENABLE HEAVY REWARDS");
      }

      // No context at all is the SDK's own documented default (EncounterContext.Empty-equivalent via the
      // null-conditional reads in AppendQuestInstructions): must behave identically to an explicit false.
      [Test]
      public void GIVEN_no_context_is_supplied_WHEN_teaching_quest_rewards_THEN_it_matches_the_explicit_off_case()
      {
         string prompt = Build(null);

         prompt.Should().Contain("NEVER promise a fief, a title, or yourself in marriage as the PRICE");
         prompt.Should().NotContain("grant_fief");
         prompt.Should().NotContain("marriage_reward");
      }

      // ── Toggle ON: the relaxed guardrail, channel-only, with tenability rules ──

      [Test]
      public void GIVEN_the_toggle_is_on_WHEN_teaching_quest_rewards_THEN_it_teaches_grant_fief_and_marriage_reward()
      {
         string prompt = Build(new EncounterContext {FiefAndMarriageQuestRewardsAllowed = true});

         prompt.Should().Contain("grant_fief");
         prompt.Should().Contain("marriage_reward");
         prompt.Should().Contain("TENABLE HEAVY REWARDS");
      }

      // The blanket prohibition must be GONE when the toggle is on (replaced, not merely supplemented): a
      // model taught both "never" and "you may, if..." in the same prompt would be fed a live contradiction.
      [Test]
      public void GIVEN_the_toggle_is_on_WHEN_teaching_quest_rewards_THEN_the_blanket_prohibition_is_replaced_not_supplemented()
      {
         string prompt = Build(new EncounterContext {FiefAndMarriageQuestRewardsAllowed = true});

         prompt.Should().NotContain("REWARDS YOU MAY PROMISE AS PAYMENT (never a fief or marriage as the price of a task):");
         prompt.Should().NotContain("NEVER promise a fief, a title, or yourself in marriage as the PRICE");
      }

      // Channel discipline: even with the toggle on, a heavy reward must still go through the [QUEST] block's
      // reward_grant, never a bare narrative line: the ONE guardrail principle that must survive the toggle.
      [Test]
      public void GIVEN_the_toggle_is_on_WHEN_teaching_quest_rewards_THEN_it_still_forbids_a_bare_narrative_promise()
      {
         string prompt = Build(new EncounterContext {FiefAndMarriageQuestRewardsAllowed = true});

         prompt.Should().Contain("never as a bare line in your dialogue");
      }

      // Tenability rules mirror what the consumer actually re-checks (QuestRewardGatePolicy): only the
      // giver's OWN clan's fief, only a match the giver has real authority to give, never a reigning
      // sovereign's own hand.
      [Test]
      public void GIVEN_the_toggle_is_on_WHEN_teaching_quest_rewards_THEN_it_states_the_tenability_rules()
      {
         string prompt = Build(new EncounterContext {FiefAndMarriageQuestRewardsAllowed = true});

         prompt.Should().Contain("YOUR OWN clan holds right now");
         prompt.Should().Contain("reigning sovereign cannot be married off");
      }

      // Re-verification at payout must be taught too, so the NPC's own in-character reaction to a later
      // refusal (the debt acknowledged, not denied) is primed from the start.
      [Test]
      public void GIVEN_the_toggle_is_on_WHEN_teaching_quest_rewards_THEN_it_states_both_are_re_verified_at_completion()
      {
         string prompt = Build(new EncounterContext {FiefAndMarriageQuestRewardsAllowed = true});

         prompt.Should().Contain("re-verified again the moment the task completes");
      }
   }
}

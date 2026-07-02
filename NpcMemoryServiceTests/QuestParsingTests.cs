// Code written by Gabriel Mailhot, 01/07/2026.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>
   ///   Documents <see cref="SectionResponseParser" /> behaviour for the
   ///   <c>[QUEST]</c>, <c>[QUEST_COMPLETE]</c> and <c>[QUEST_ABANDON]</c> sections, and
   ///   the quest-type / reward-grant alias tables, which were previously untested.
   /// </summary>
   [TestFixture]
   public class QuestParsingTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      // ---------- [QUEST] proposal ----------

      [Test]
      public void Quest_proposal_with_recognized_type_is_parsed()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[QUEST]\n" +
            "type: bandit_clear\n" +
            "description: Clear the road.\n" +
            "target_settlement: Sargot\n" +
            "deadline_days: 10\n" +
            "reward_gold: 200\n" +
            "reward_relation: 5\n" +
            "[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.Type.Should().Be(QuestType.BanditClear);
         result.QuestGiven.Description.Should().Be("Clear the road.");
         result.QuestGiven.TargetSettlement.Should().Be("Sargot");
         result.QuestGiven.DeadlineDays.Should().Be(10);
         result.QuestGiven.RewardGold.Should().Be(200);
         result.QuestGiven.RewardRelation.Should().Be(5);
      }

      [Test]
      public void Quest_proposal_with_unrecognized_type_is_dropped()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: nonsense_deed\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().BeNull();
      }

      [Test]
      public void Quest_proposal_missing_type_is_dropped()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ndescription: no type here\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().BeNull();
      }

      [Test]
      public void Quest_proposal_zero_or_negative_deadline_means_no_deadline()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: bandit_clear\ndeadline_days: 0\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.DeadlineDays.Should().BeNull();
      }

      [Test]
      public void Quest_proposal_negative_reward_values_are_clamped_to_zero()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[QUEST]\ntype: bandit_clear\nreward_gold: -50\nreward_relation: -3\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.RewardGold.Should().Be(0);
         result.QuestGiven.RewardRelation.Should().Be(0);
      }

      [Test]
      public void Quest_proposal_with_reward_grant_is_parsed()
      {
         var raw =
            "[DIALOGUE]hi[/DIALOGUE]\n" +
            "[QUEST]\ntype: bandit_clear\nreward_grant: join_party\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.Reward.Should().Be(RewardGrant.JoinParty);
      }

      // ---------- Quest-type alias table ----------

      [TestCase("bandit_clear", QuestType.BanditClear)]
      [TestCase("bandits", QuestType.BanditClear)]
      [TestCase("clear_bandits", QuestType.BanditClear)]
      [TestCase("bandit_hideout", QuestType.BanditHideout)]
      [TestCase("hideout", QuestType.BanditHideout)]
      [TestCase("attack_faction", QuestType.AttackFaction)]
      [TestCase("raid_faction", QuestType.AttackFaction)]
      [TestCase("attack_lord", QuestType.AttackLord)]
      [TestCase("defeat_lord", QuestType.AttackLord)]
      [TestCase("raid_village", QuestType.RaidVillage)]
      [TestCase("burn_village", QuestType.RaidVillage)]
      [TestCase("attack_caravan", QuestType.AttackCaravan)]
      [TestCase("raid_caravan", QuestType.AttackCaravan)]
      [TestCase("siege", QuestType.Siege)]
      [TestCase("besiege", QuestType.Siege)]
      [TestCase("capture_prisoner", QuestType.CapturePrisoner)]
      [TestCase("take_prisoner", QuestType.CapturePrisoner)]
      [TestCase("execute_enemy", QuestType.ExecuteEnemy)]
      [TestCase("kill_enemy", QuestType.ExecuteEnemy)]
      [TestCase("rescue_prisoner", QuestType.RescuePrisoner)]
      [TestCase("free_prisoner", QuestType.RescuePrisoner)]
      [TestCase("deliver_letter", QuestType.DeliverLetter)]
      [TestCase("carry_message", QuestType.DeliverLetter)]
      [TestCase("provide_gold", QuestType.ProvideGold)]
      [TestCase("child_support", QuestType.ProvideGold)]
      [TestCase("scout_army", QuestType.ScoutArmy)]
      [TestCase("find_army", QuestType.ScoutArmy)]
      [TestCase("deliver_items", QuestType.DeliverItems)]
      [TestCase("pay_in_goods", QuestType.DeliverItems)]
      [TestCase("deliver_prisoner", QuestType.DeliverPrisoner)]
      [TestCase("hand_over_prisoner", QuestType.DeliverPrisoner)]
      [TestCase("declare_war", QuestType.DeclareWar)]
      [TestCase("go_to_war", QuestType.DeclareWar)]
      [TestCase("BanditClear", QuestType.BanditClear)] // direct enum-name match
      public void Quest_type_alias_resolves_to_expected_type(string rawType, QuestType expected)
      {
         var raw = $"[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: {rawType}\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.Type.Should().Be(expected);
      }

      // ---------- Reward-grant alias table ----------

      [TestCase("join_party", RewardGrant.JoinParty)]
      [TestCase("recruit", RewardGrant.JoinParty)]
      [TestCase("take_service", RewardGrant.JoinParty)]
      [TestCase("give_item", RewardGrant.GiveItem)]
      [TestCase("gift_item", RewardGrant.GiveItem)]
      [TestCase("give_troops", RewardGrant.GiveTroops)]
      [TestCase("lend_troops", RewardGrant.GiveTroops)]
      [TestCase("marriage_consent", RewardGrant.MarriageConsent)]
      [TestCase("betrothal", RewardGrant.MarriageConsent)]
      [TestCase("release_prisoner", RewardGrant.ReleasePrisoner)]
      [TestCase("hand_over_prisoner", RewardGrant.ReleasePrisoner)]
      [TestCase("gibberish_token", RewardGrant.None)]
      public void Reward_grant_alias_resolves_to_expected_grant(string rawGrant, RewardGrant expected)
      {
         var raw = $"[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: bandit_clear\nreward_grant: {rawGrant}\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.Reward.Should().Be(expected);
      }

      // ---------- [QUEST_COMPLETE] ----------

      [Test]
      public void Quest_complete_with_no_type_means_the_single_satisfied_quest()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_COMPLETE]\n[/QUEST_COMPLETE]";

         var result = _parser.Parse(raw);

         result.QuestCompleted.Should().NotBeNull();
         result.QuestCompleted!.Type.Should().BeNull();
      }

      [Test]
      public void Quest_complete_with_named_type_disambiguates()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_COMPLETE]\ntype: bandit_clear\n[/QUEST_COMPLETE]";

         var result = _parser.Parse(raw);

         result.QuestCompleted.Should().NotBeNull();
         result.QuestCompleted!.Type.Should().Be(QuestType.BanditClear);
      }

      [Test]
      public void Quest_complete_section_absent_returns_null()
      {
         var result = _parser.Parse("[DIALOGUE]hi[/DIALOGUE]");
         result.QuestCompleted.Should().BeNull();
      }

      // ---------- [QUEST_ABANDON] ----------

      [Test]
      public void Quest_abandon_with_no_type_means_the_single_outstanding_quest()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_ABANDON]\n[/QUEST_ABANDON]";

         var result = _parser.Parse(raw);

         result.QuestAbandoned.Should().NotBeNull();
         result.QuestAbandoned!.Type.Should().BeNull();
      }

      [Test]
      public void Quest_abandon_with_named_type_disambiguates()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_ABANDON]\ntype: siege\n[/QUEST_ABANDON]";

         var result = _parser.Parse(raw);

         result.QuestAbandoned.Should().NotBeNull();
         result.QuestAbandoned!.Type.Should().Be(QuestType.Siege);
      }

      [Test]
      public void Quest_abandon_section_absent_returns_null()
      {
         var result = _parser.Parse("[DIALOGUE]hi[/DIALOGUE]");
         result.QuestAbandoned.Should().BeNull();
      }
   }
}

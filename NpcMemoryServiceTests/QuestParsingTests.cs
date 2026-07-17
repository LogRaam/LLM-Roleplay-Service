// Code written by Gabriel Mailhot, 01/07/2026.
//
// [QUEST]/[QUEST_COMPLETE]/[QUEST_ABANDON] are the model's only channel for offering, closing or
// dropping a task; QuestService persists exactly what QuestGiven/QuestCompleted/QuestAbandoned carry,
// with no re-derivation on the consumer side. A quest-type or reward-grant token the alias table below
// fails to recognize does not just vanish quietly: QuestBlockMalformed tells the player their task was
// NOT recorded (ROADMAP, "NOTHING-IS-SILENT", ratified 2026-07-08), so every alias pinned here is one
// fewer way a genuinely offered quest reads to the player as garbled. An untyped QUEST_COMPLETE or
// QUEST_ABANDON resolves to the FIRST matching quest on the giver's list (QuestService), so these
// tests also pin that "no type" is a deliberate wildcard, not a parse failure.

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

      // Happy path: every declared field of a well-formed proposal must survive the round trip
      // unchanged, since QuestService persists exactly what QuestGiven carries (no re-derivation).
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

      // A quest-type token the model hallucinated (not in the alias table, not the enum name)
      // must not become a QuestProposal with an undefined Type; dropping it to null is what lets
      // QuestBlockMalformed (tested elsewhere) warn the player instead of a phantom quest persisting.
      [Test]
      public void Quest_proposal_with_unrecognized_type_is_dropped()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: nonsense_deed\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().BeNull();
      }

      // Same failure family, but for a block that never carried a type token at all (a
      // half-written [QUEST] block): must drop rather than guess a default quest type.
      [Test]
      public void Quest_proposal_missing_type_is_dropped()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ndescription: no type here\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().BeNull();
      }

      // A deadline_days of 0 (or negative) must not become a quest that is already expired the
      // instant it's issued; the parser deliberately reads that as "no deadline" instead.
      [Test]
      public void Quest_proposal_zero_or_negative_deadline_means_no_deadline()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: bandit_clear\ndeadline_days: 0\n[/QUEST]";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.DeadlineDays.Should().BeNull();
      }

      // A model-invented negative reward_gold/reward_relation must never become a quest that
      // DOCKS the player's gold or relation on completion instead of paying them; floor at zero.
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

      // reward_grant selects which special grant path the consumer attempts on completion (e.g.
      // JoinParty adds the NPC as a companion); the token has to reach QuestGiven.Reward intact
      // or a spoken "join my party" promise never triggers the matching reward logic.
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

      // This table is the single point of truth mapping the many ways a model phrases a quest
      // ("bandits", "clear_bandits", "hideout"...) onto the QuestType enum QuestService switches
      // on. Any alias silently missing here is a quest the NPC clearly offered that reads to the
      // player as QuestBlockMalformed instead of registering.
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

      // Same principle as the quest-type table, for reward_grant tokens. "gibberish_token" pins the
      // safe default: an unrecognized grant falls to RewardGrant.None (an ordinary gold/relation
      // quest), never dropped outright. Note: ReleasePrisoner is recognized here at the parsing
      // layer, but as of this writing the downstream grant executor declines it cleanly as not yet
      // supported rather than actually freeing a prisoner; that wiring gap is outside this parser.
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

      // An empty body is a deliberate wildcard, not a parse failure: QuestService.CompleteQuest
      // resolves a null Type to the giver's single outstanding satisfied quest (FirstOrDefault).
      // Requiring an explicit type on every completion would make the common case unnecessarily
      // strict for the model to produce.
      [Test]
      public void Quest_complete_with_no_type_means_the_single_satisfied_quest()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_COMPLETE]\n[/QUEST_COMPLETE]";

         var result = _parser.Parse(raw);

         result.QuestCompleted.Should().NotBeNull();
         result.QuestCompleted!.Type.Should().BeNull();
      }

      // When a giver holds more than one open quest, the type token is what QuestService uses to
      // pick the right one to mark complete; parse it through correctly or the wrong quest could
      // be paid out.
      [Test]
      public void Quest_complete_with_named_type_disambiguates()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_COMPLETE]\ntype: bandit_clear\n[/QUEST_COMPLETE]";

         var result = _parser.Parse(raw);

         result.QuestCompleted.Should().NotBeNull();
         result.QuestCompleted!.Type.Should().Be(QuestType.BanditClear);
      }

      // Distinguishes "no completion claimed this turn" (silent, correct) from "the single
      // outstanding quest was just completed" (an empty-but-present block, tested above); the two
      // must never be conflated, or a normal reply would randomly complete quests, or a genuine
      // completion would be silently ignored.
      [Test]
      public void Quest_complete_section_absent_returns_null()
      {
         var result = _parser.Parse("[DIALOGUE]hi[/DIALOGUE]");
         result.QuestCompleted.Should().BeNull();
      }

      // ---------- [QUEST_ABANDON] ----------

      // Same wildcard contract as QUEST_COMPLETE: an empty body resolves to the giver's single
      // outstanding quest in QuestService.AbandonQuest, which also applies the broken-word
      // consequence, so this null-Type case is load-bearing, not just a parsing nicety.
      [Test]
      public void Quest_abandon_with_no_type_means_the_single_outstanding_quest()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_ABANDON]\n[/QUEST_ABANDON]";

         var result = _parser.Parse(raw);

         result.QuestAbandoned.Should().NotBeNull();
         result.QuestAbandoned!.Type.Should().BeNull();
         result.QuestAbandoned.TypeUnrecognized.Should().BeFalse(); // no type named is the legitimate wildcard, never flagged
      }

      // The confirmed wildcard bug (quest audit, 2026-07-17): a [QUEST_ABANDON] that NAMED a type the parser
      // does not recognise resolved to Type=null, INDISTINGUISHABLE from "no type given", so QuestService
      // wildcarded it onto the first outstanding quest and could land a false broken-word penalty (-regard +
      // grudge) on the wrong task. The block must now flag TypeUnrecognized so the consumer ignores the
      // hallucinated token instead of penalising a quest the player never abandoned.
      [Test]
      public void Quest_abandon_with_a_named_but_unrecognized_type_is_flagged_not_treated_as_a_wildcard()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_ABANDON]\ntype: not_a_real_quest_type\n[/QUEST_ABANDON]";

         var result = _parser.Parse(raw);

         result.QuestAbandoned.Should().NotBeNull();
         result.QuestAbandoned!.Type.Should().BeNull();
         result.QuestAbandoned.TypeUnrecognized.Should().BeTrue();
      }

      // The completion side of the same bug: a garbage type must not pay out the first awaiting-reward quest.
      // Less harmful than a false abandon penalty, but still the wrong quest closed on a hallucination.
      [Test]
      public void Quest_complete_with_a_named_but_unrecognized_type_is_flagged_not_treated_as_a_wildcard()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_COMPLETE]\ntype: not_a_real_quest_type\n[/QUEST_COMPLETE]";

         var result = _parser.Parse(raw);

         result.QuestCompleted.Should().NotBeNull();
         result.QuestCompleted!.Type.Should().BeNull();
         result.QuestCompleted.TypeUnrecognized.Should().BeTrue();
      }

      // Guards the boundary from the other side: the legitimate untyped completion wildcard must stay
      // UNflagged, or the mod would start ignoring honest "complete my single quest" acknowledgements.
      [Test]
      public void Quest_complete_with_no_type_is_not_flagged_as_unrecognized()
      {
         _parser.Parse("[DIALOGUE]hi[/DIALOGUE]\n[QUEST_COMPLETE]\n[/QUEST_COMPLETE]")
                .QuestCompleted!.TypeUnrecognized.Should().BeFalse();
      }

      // ---------- Truncation tolerance (quest audit A1, fragility 1) ----------

      // A reply cut off by the token limit opens [QUEST] and never reaches [/QUEST]. The closed-only extractor
      // dropped it with no flag and no message, so a task the NPC just offered aloud simply never existed. A
      // truncated but well-formed body must still parse into a real proposal.
      [Test]
      public void A_truncated_quest_block_with_no_close_tag_still_parses()
      {
         var raw = "[DIALOGUE]clear the lair[/DIALOGUE]\n[QUEST]\ntype: bandit_hideout\ntarget_settlement: Pravend";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().NotBeNull();
         result.QuestGiven!.Type.Should().Be(QuestType.BanditHideout);
         result.QuestBlockMalformed.Should().BeFalse();
      }

      // The other half: a [QUEST] truncated BEFORE its type line is unparseable, but it must no longer vanish
      // silently. It must trip QuestBlockMalformed so the player is told the spoken offer was not recorded.
      [Test]
      public void A_truncated_quest_block_cut_before_its_type_trips_the_malformed_flag()
      {
         var raw = "[DIALOGUE]I have a task[/DIALOGUE]\n[QUEST]\ndescription: rid my lands of";

         var result = _parser.Parse(raw);

         result.QuestGiven.Should().BeNull();
         result.QuestBlockMalformed.Should().BeTrue();
      }

      // The closing family tolerates truncation too: an unclosed [QUEST_COMPLETE] still yields a claim rather
      // than being lost, so a hard-won completion is not silently denied because the reply was cut short.
      [Test]
      public void A_truncated_quest_complete_block_still_yields_a_claim()
      {
         _parser.Parse("[DIALOGUE]well done[/DIALOGUE]\n[QUEST_COMPLETE]\ntype: bandit_clear")
                .QuestCompleted!.Type.Should().Be(QuestType.BanditClear);
      }

      // ---------- Lexical robustness for weak models (quest audit A2 / A3 / A6) ----------

      // JSON-minded models wrap values in quotes and bullet their lines. A quoted type and a markdown-bulleted
      // key line must both still resolve, or a genuinely offered quest reads to the player as garbled or vanishes.
      [Test]
      public void A_quoted_type_and_a_bulleted_key_line_still_parse()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\n- type: \"bandit_clear\"\ntarget_settlement: Pravend\n[/QUEST]";

         _parser.Parse(raw).QuestGiven!.Type.Should().Be(QuestType.BanditClear);
      }

      // A key written with a space instead of an underscore ("reward gold") is a common model habit; folding it
      // to the canonical key keeps the reward from silently persisting as 0.
      [Test]
      public void A_spaced_key_folds_to_the_canonical_underscore_key()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: bandit_clear\nreward gold: 250\n[/QUEST]";

         _parser.Parse(raw).QuestGiven!.RewardGold.Should().Be(250);
      }

      // Numbers dressed with a unit word or a thousands separator must still read as the number, not drop to 0.
      [Test]
      public void A_reward_with_a_unit_word_and_a_thousands_separator_reads_as_the_number()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: bandit_clear\nreward_gold: 1,500 denars\n[/QUEST]";

         _parser.Parse(raw).QuestGiven!.RewardGold.Should().Be(1500);
      }

      // A model that writes an absurd deadline ("365") must not persist a year-long task: it is capped.
      [Test]
      public void An_absurd_deadline_is_capped_to_the_maximum()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST]\ntype: bandit_clear\ndeadline_days: 365\n[/QUEST]";

         _parser.Parse(raw).QuestGiven!.DeadlineDays.Should().Be(90);
      }

      // With several quests open, mistargeting an abandon claim to the wrong one would apply the
      // broken-word consequence for a task the player never actually gave up.
      [Test]
      public void Quest_abandon_with_named_type_disambiguates()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[QUEST_ABANDON]\ntype: siege\n[/QUEST_ABANDON]";

         var result = _parser.Parse(raw);

         result.QuestAbandoned.Should().NotBeNull();
         result.QuestAbandoned!.Type.Should().Be(QuestType.Siege);
      }

      // Mirrors the QUEST_COMPLETE absent case: no [QUEST_ABANDON] block must never be read as an
      // abandonment of the single outstanding quest.
      [Test]
      public void Quest_abandon_section_absent_returns_null()
      {
         var result = _parser.Parse("[DIALOGUE]hi[/DIALOGUE]");
         result.QuestAbandoned.Should().BeNull();
      }
   }
}

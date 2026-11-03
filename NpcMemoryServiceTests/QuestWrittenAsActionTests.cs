// Code written by Gabriel Mailhot, 03/11/2026.
// A model that means to give the player a task sometimes writes it as an [ACTION] instead of the [QUEST] block it was
// taught. avalanche24 (Nexus, 03/11/2026), on a local glm-4.6v-flash: Lord Ulbos agreed to a bandit task, said "The
// task is yours", and wrote "[ACTION] type: quest" with no task kind; "could you tell me why the model wasn't able to
// generate a quest from this prompt?". "quest" is no verb, so the game refused it as an unknown action and the task
// the lord had just spoken was never given.
//
// The parser now takes such an action out of the action list. When its kind can be read (quest_type and its
// siblings) it becomes the quest; when it cannot, the reply counts as a garbled task offer, so the player is told
// nothing was recorded and the model is asked to state it again. If this breaks, a lord promises a task the journal
// never shows, or an action-shaped deed is wrongly turned into a task for the player.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class QuestWrittenAsActionTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      // STAKE: the lord agrees to a task and names its kind, but under [ACTION]: without the salvage, the player hears
      // "the task is yours" and gets an "unsupported action" notice instead of a quest.
      [Test]
      public void GIVEN_a_quest_written_as_an_action_with_its_kind_WHEN_parsed_THEN_it_becomes_the_quest()
      {
         var raw = "[DIALOGUE]\"The task is yours.\"[/DIALOGUE]\n" +
                   "[ACTION]\ntype: quest\nquest_type: bandit_clear\ntarget_settlement: Lycaron\n" +
                   "description: Clear the western passes around Lycaron.\n[/ACTION]";

         ParsedResponse r = _parser.Parse(raw);

         r.QuestGiven.Should().NotBeNull();
         r.QuestGiven!.Type.Should().Be(QuestType.BanditClear);
         r.QuestGiven.TargetSettlement.Should().Be("Lycaron");
         r.QuestGiven.Description.Should().Be("Clear the western passes around Lycaron.");
         r.QuestBlockMalformed.Should().BeFalse();
         r.Actions.Should().BeEmpty();
      }

      // STAKE: the exact reply avalanche24 sent, with no task kind. Guessing "bandits" from the description could hand
      // the player the wrong task; saying nothing would leave the promise silently broken. The player is told instead.
      [Test]
      public void GIVEN_the_reported_reply_with_no_task_kind_WHEN_parsed_THEN_it_is_a_garbled_offer_and_no_unknown_action()
      {
         var raw = "[EVENT]\ntype: agreement\nsummary: I agreed to give Giermund the bandit_clear task for Lycaron.\n[/EVENT]\n" +
                   "[DIALOGUE]*My hand drifts back from my hilt.* \"Very well. The task is yours.\"[/DIALOGUE]\n" +
                   "[ACTION]\ntype: quest\ntarget_settlement: Lycaron\n" +
                   "description: Clear the western passes around Lycaron and bring any captured bandits to me.\n[/ACTION]";

         ParsedResponse r = _parser.Parse(raw);

         r.QuestGiven.Should().BeNull();
         r.QuestBlockMalformed.Should().BeTrue();
         r.Actions.Should().BeEmpty();
      }

      // STAKE: a model that wrote BOTH the taught [QUEST] block and a stray [ACTION] quest must give one task, and the
      // player must not also see an unknown-action notice for the duplicate.
      [Test]
      public void GIVEN_a_quest_block_and_a_quest_action_WHEN_parsed_THEN_the_block_wins_and_the_action_is_dropped()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n" +
                   "[QUEST]\ntype: siege\ntarget_settlement: Pravend\n[/QUEST]\n" +
                   "[ACTION]\ntype: quest\nquest_type: bandit_clear\n[/ACTION]";

         ParsedResponse r = _parser.Parse(raw);

         r.QuestGiven!.Type.Should().Be(QuestType.Siege);
         r.QuestBlockMalformed.Should().BeFalse();
         r.Actions.Should().BeEmpty();
      }

      // STAKE: the real deeds written beside the stray quest action (a gift, a relation change) must still happen.
      [Test]
      public void GIVEN_a_quest_action_among_real_actions_WHEN_parsed_THEN_the_real_actions_are_kept_in_order()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n" +
                   "[ACTION]\ntype: give_gold\namount: 100\n[/ACTION]\n" +
                   "[ACTION]\ntype: give quest\ntask: hideout\n[/ACTION]\n" +
                   "[ACTION]\ntype: change_relation\ndelta: 2\n[/ACTION]";

         ParsedResponse r = _parser.Parse(raw);

         r.QuestGiven!.Type.Should().Be(QuestType.BanditHideout);
         r.Actions.Should().HaveCount(2);
         r.Actions[0].Type.Should().Be("give_gold");
         r.Actions[1].Type.Should().Be("change_relation");
      }

      // STAKE: a quest kind written AS the action type ("execute", "capture") is a deed the character means to do now;
      // reading it as a task would hand the player a quest nobody offered. It stays an action for the game to judge.
      [Test]
      public void GIVEN_an_action_typed_with_a_quest_kind_word_WHEN_parsed_THEN_it_stays_an_action()
      {
         var raw = "[DIALOGUE]hi[/DIALOGUE]\n[ACTION]\ntype: execute\n[/ACTION]";

         ParsedResponse r = _parser.Parse(raw);

         r.QuestGiven.Should().BeNull();
         r.QuestBlockMalformed.Should().BeFalse();
         r.Actions.Should().ContainSingle(a => a.Type == "execute");
      }
   }
}

// Code written by Gabriel Mailhot, 18/09/2026.
// What these pin: that a bargain struck thirty seconds ago is not spoken of as an errand already under way.
//
// Player report (raphareish, Nexus, 18/09/2026): "after accepting a quest and talking about the deal that was
// just made, NPCs behave like it happened hours before on the same day, even though the chat has never been
// closed and there had been no roleplay about time passing."
//
// Same shape as the repetition loop fixed the day before, in a different block. A quest agreed mid-conversation
// is in ActiveQuests by the next turn, where AppendActiveQuests rendered it as OUTSTANDING and added "you may
// ask how it fares" - so the character asked after the progress of a task the player had not yet left the room
// to begin.
//
// The day stamp cannot settle this and that is the whole reason a count exists: a deal struck a minute ago and
// one struck at dawn carry the same IssuedOnDay, and "the same day" is exactly what he described.

#region

using System;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class QuestAgreedInThisConversationTests
   {
      private const string Older = "Clear the bandits from the Ortysia road";
      private const string JustNow = "Carry my seal to the reeve at Zeonica";

      // THE REPORT. One task from before, one agreed in this very conversation: the first may be asked after,
      // the second may not.
      [Test]
      public void GIVEN_a_quest_agreed_during_this_conversation_WHEN_built_THEN_it_is_not_an_errand_under_way()
      {
         string prompt = Build(knownThrough: 1, Older, JustNow);

         prompt.Should().Contain($"OUTSTANDING: {Older}");
         prompt.Should().Contain($"JUST AGREED, in this very conversation: {JustNow}");
      }

      // And the character is told what follows from it, because the wrong behaviour was a QUESTION he asked,
      // not a label he read.
      [Test]
      public void GIVEN_a_quest_agreed_during_this_conversation_WHEN_built_THEN_asking_after_it_is_ruled_out()
      {
         string prompt = Build(knownThrough: 0, JustNow);

         prompt.Should().Contain("do not ask how it fares");
         prompt.Should().Contain("never as something arranged on an earlier day");
      }

      // ZERO IS A REAL ANSWER, and the commonest one: a character who held no task at all when the talk opened
      // and was given their first during it. Sentinelling zero as "unset" would leave exactly that case broken.
      [Test]
      public void GIVEN_the_first_quest_this_character_has_ever_given_WHEN_built_THEN_it_is_still_marked_fresh()
      {
         Build(knownThrough: 0, JustNow).Should().NotContain($"OUTSTANDING: {JustNow}");
      }

      // UNSET RENDERS EVERYTHING THE OLD WAY, so a caller that supplies no cut keeps the behaviour it has.
      [Test]
      public void GIVEN_no_cut_supplied_WHEN_built_THEN_every_task_reads_as_it_did_before()
      {
         string prompt = Build(knownThrough: null, Older, JustNow);

         prompt.Should().Contain($"OUTSTANDING: {Older}").And.Contain($"OUTSTANDING: {JustNow}");
         prompt.Should().NotContain("JUST AGREED");
      }

      // THE UNTRUSTWORTHY CASE. A cut pointing past the end of the list means everything is treated as
      // settled, which is the behaviour that shipped: a mis-read can fail to mark a task fresh, and can never
      // hide one.
      [Test]
      public void GIVEN_a_cut_beyond_the_end_WHEN_built_THEN_nothing_is_lost_and_nothing_is_marked_fresh()
      {
         string prompt = Build(knownThrough: 99, Older, JustNow);

         prompt.Should().Contain(Older).And.Contain(JustNow).And.NotContain("JUST AGREED");
      }

      // A FRESH TASK IS STILL A TASK. The point is its age, not its existence: dropping it would be the
      // opposite bug, a character who agreed to something and then had no idea what.
      [Test]
      public void GIVEN_a_quest_agreed_during_this_conversation_WHEN_built_THEN_the_character_still_knows_of_it()
      {
         Build(knownThrough: 0, JustNow).Should().Contain(JustNow);
      }

      #region private

      private static string Build(int? knownThrough, params string[] descriptions)
      {
         var npc = new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"};

         foreach (string description in descriptions)
            npc.ActiveQuests.Add(new InformalQuest {
               Type = QuestType.BanditClear, Description = description, IssuedOnDay = 12, Status = QuestStatus.Active
            });

         return new PromptBuilder {EnableQuests = true}.BuildSystemPrompt(
            npc, new WorldState {CurrentDay = 12},
            new EncounterContext {
               LeanLevel = LeanPromptLevel.Full,
               Scene = SceneType.Keep,
               QuestsKnownThrough = knownThrough
            });
      }

      #endregion
   }
}

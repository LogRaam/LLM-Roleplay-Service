// Code written by Gabriel Mailhot, 20/08/2026.
// Player report (Desporion): an NPC proposed marriage across three letters, "agreed on the terms", then
// denied it all when the player spoke to him face to face. LetterPopupManager.MarkReceived recorded only a
// generic "Received a letter from X (Reason)" memory, never the letter's substance, so the live chat prompt
// (which reads profile.Events) had no idea a marriage was proposed and the NPC contradicted his own letters.
// LetterMemoryPolicy decides which LetterReasons are worth a real memory (ShouldRemember) and builds the
// synchronous, guaranteed-capture fallback line (BaseMemory) the mod records BEFORE the richer async LLM
// summary ever runs (or in place of it, if the summary never returns). Get ShouldRemember wrong and either a
// substantive letter is silently dropped (the very bug this fixes) or a pure re-ping burns an LLM call for
// nothing worth remembering; get BaseMemory wrong and the guaranteed fallback itself is not first-person, not
// truthful about who wrote to whom, or drops the player's name.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LetterMemoryPolicyTests
   {
      // AwaitingReply is a nag about a letter ALREADY remembered (the original proposal/request), and its own
      // arrival adds nothing beyond "still no answer": remembering it too would just duplicate the substance
      // already on record under a different reason.
      [Test]
      public void GIVEN_AwaitingReply_WHEN_asking_ShouldRemember_THEN_it_is_false()
      {
         LetterMemoryPolicy.ShouldRemember(LetterReason.AwaitingReply).Should().BeFalse();
      }

      // QuestUpdate re-pings a quest that is already tracked in its own right (profile.ActiveQuests), so a
      // second, letter-shaped memory of the same fact would be pure duplication, not new substance.
      [Test]
      public void GIVEN_QuestUpdate_WHEN_asking_ShouldRemember_THEN_it_is_false()
      {
         LetterMemoryPolicy.ShouldRemember(LetterReason.QuestUpdate).Should().BeFalse();
      }

      // MarriageProposal is the exact reason from the player report: it MUST be remembered, or the bug
      // reproduces (an NPC denying a proposal he sent by letter).
      [Test]
      public void GIVEN_MarriageProposal_WHEN_asking_ShouldRemember_THEN_it_is_true()
      {
         LetterMemoryPolicy.ShouldRemember(LetterReason.MarriageProposal).Should().BeTrue();
      }

      // The guaranteed-capture fallback is read back into the NPC's OWN prompt as their own memory
      // (profile.Events), so it must be written in the first person, exactly like the captive-scene and
      // conversation base summaries it mirrors: a third-person "Desporion wrote a letter" would read wrong
      // injected back as "you remember: Desporion wrote a letter" to Desporion himself.
      [Test]
      public void GIVEN_npcIsSender_WHEN_building_the_base_memory_THEN_it_is_first_person_and_names_the_player()
      {
         string line = LetterMemoryPolicy.BaseMemory(LetterReason.MarriageProposal, "Desporion", "Aldric", true);

         line.Should().StartWith("I wrote to Aldric");
         line.Should().Contain("Aldric");
      }

      // The reverse direction (npcIsSender=false) is the reserved case for a future site: the NPC received the
      // player's letter. If this direction ever collapsed onto the same "I wrote to" phrasing, an NPC would
      // remember writing letters they never sent.
      [Test]
      public void GIVEN_npcIsSender_false_WHEN_building_the_base_memory_THEN_it_reflects_receiving_and_replying()
      {
         string line = LetterMemoryPolicy.BaseMemory(LetterReason.MarriageProposal, "Desporion", "Aldric", false);

         line.Should().StartWith("Aldric wrote to me");
         line.Should().Contain("Aldric");
      }

      // A blank/null player name must still leave a usable, non-crashing fallback line (the same defensive
      // convention as every other "Hero.MainHero?.Name ?? \"the player\"" call site in the mod).
      [Test]
      public void GIVEN_a_blank_player_name_WHEN_building_the_base_memory_THEN_it_falls_back_to_the_player()
      {
         string line = LetterMemoryPolicy.BaseMemory(LetterReason.RealmTidings, "Desporion", "", true);

         line.Should().Contain("the player");
      }
   }
}

// Code written by Gabriel Mailhot, 28/11/2026.
// What a meeting leaves (Stage 2 of the group-scene plan; Gabriel, 28/11/2026: "go pour tout le système ... je testerai
// demain"). One background call at a chat's close updates what the character carries away from the player: a dated lasting
// change, the four-part bond, the whole list of unsettled matters, the secrets told and a new one. The simulator measured it
// as the largest gain of the whole work (4.2 blind with no memory, 6.8 with lasting changes and the bond).
//
// These tests pin the reading of the reply, the update rules and the prompt section. If they are wrong: a replayed job
// writes the same change twice, an "open: nothing" fails to settle a matter (or a missing line wipes every matter), a
// secret the player heard stays "kept", a French reply loses the bond, the heir is treated with his father's grudges, or
// the prompt of a save that never used the feature moves.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class MeetingMemoryTests
   {
      private const string Player = "Gabriel";

      private const string Reply = "changed: I no longer take him for a mere sellsword; he kept his word about the grain.\n"
                                 + "bond: understands partly, forgives yes, loves no, trusts partly\n"
                                 + "open: whether he will speak for my brother at court | the debt of 2000 denars\n"
                                 + "told: none\n"
                                 + "secret: I mean to leave my husband's hall before winter.";

      private static NpcProfile Npc(MeetingMemory? memory = null) => new() {
         Id = "lord_test", Name = "Lady Ira", Faction = "Vlandia", Clan = "dey Meroc", MeetingMemory = memory
      };

      private static MeetingMemoryApplyFacts Facts(int day = 100, string? anchor = "a1", IReadOnlyList<string>? shown = null)
         => new() {Day = day, PlayerName = Player, AnchorId = anchor, SecretsShown = shown ?? new List<string>()};

      private static string Prompt(NpcProfile npc, int today = 103, LeanPromptLevel lean = LeanPromptLevel.Full, EncounterContext? ctx = null, bool enabled = true)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature, PlayerName = Player, EnableMeetingMemory = enabled}
           .BuildSystemPrompt(npc, new WorldState {CurrentDay = today}, ctx ?? new EncounterContext {LeanLevel = lean});

      // STAKE: the simulator's reply shape must land whole, or the character forgets what changed in it.
      [Test]
      public void GIVEN_a_full_reply_WHEN_applied_THEN_change_bond_matters_and_secret_are_recorded()
      {
         NpcProfile npc = Npc();

         MeetingMemoryUpdater.ApplyReply(npc, Reply, Facts()).Should().BeTrue();

         MeetingMemory m = npc.MeetingMemory!;
         m.AboutPlayer.Should().Be(Player);
         m.Changes!.Should().ContainSingle(c => c.Day == 100 && c.Text.StartsWith("I no longer take him"));
         m.Bond!.Understands.Should().Be("partly");
         m.Bond.Forgives.Should().Be("yes");
         m.Bond.Loves.Should().Be("no");
         m.Bond.Trusts.Should().Be("partly");
         m.Unsettled.Should().Equal("whether he will speak for my brother at court", "the debt of 2000 denars");
         m.Secrets!.Should().ContainSingle(s => s.Text == "I mean to leave my husband's hall before winter." && s.SinceDay == 100);
      }

      // STAKE (the advisor's replay case): a job replayed after a reload, or a resumed sitting, runs the update again for the
      // same meeting. It must replace, never double, or the character "changed" twice for one afternoon.
      [Test]
      public void GIVEN_the_same_meeting_applied_twice_WHEN_read_THEN_one_change_and_one_secret_remain()
      {
         NpcProfile npc = Npc();

         MeetingMemoryUpdater.ApplyReply(npc, Reply, Facts());
         MeetingMemoryUpdater.ApplyReply(npc, Reply.Replace("kept his word", "kept his word twice"), Facts());

         npc.MeetingMemory!.Changes!.Should().ContainSingle().Which.Text.Should().Contain("twice");
         npc.MeetingMemory.Secrets!.Should().ContainSingle();
      }

      // STAKE: a later meeting adds to the past rather than erasing it; the record grows from meeting to meeting.
      [Test]
      public void GIVEN_a_second_meeting_WHEN_applied_THEN_its_change_is_appended_after_the_first()
      {
         NpcProfile npc = Npc();
         MeetingMemoryUpdater.ApplyReply(npc, Reply, Facts(100, "a1"));

         MeetingMemoryUpdater.ApplyReply(npc, "changed: I laughed with him for the first time.", Facts(104, "a2"));

         npc.MeetingMemory!.Changes!.Select(c => c.Day).Should().Equal(100, 104);
         // No bond line and no open line: both are kept as they were.
         npc.MeetingMemory.Bond!.Forgives.Should().Be("yes");
         npc.MeetingMemory.Unsettled.Should().HaveCount(2);
      }

      // STAKE (the simulator's rule): "open: nothing" settles every matter; a reply with no open line must not wipe them.
      [Test]
      public void GIVEN_open_nothing_WHEN_applied_THEN_the_matters_are_settled()
      {
         NpcProfile npc = Npc();
         MeetingMemoryUpdater.ApplyReply(npc, Reply, Facts(100, "a1"));

         MeetingMemoryUpdater.ApplyReply(npc, "changed: nothing\nopen: nothing", Facts(104, "a2"));

         npc.MeetingMemory!.Unsettled.Should().BeEmpty();
         npc.MeetingMemory.Changes!.Should().HaveCount(1);
      }

      // STAKE: the list is consolidated to six at most; a model that lists ten would flood every later prompt.
      [Test]
      public void GIVEN_ten_open_matters_WHEN_applied_THEN_six_are_kept()
      {
         NpcProfile npc = Npc();

         MeetingMemoryUpdater.ApplyReply(npc, "open: " + string.Join(" | ", Enumerable.Range(1, 10).Select(i => "matter " + i)), Facts());

         npc.MeetingMemory!.Unsettled.Should().HaveCount(MeetingMemory.MaxUnsettled);
      }

      // STAKE: a secret the player heard must stop reading as kept from them, or the character guards what it already said.
      [Test]
      public void GIVEN_a_kept_secret_WHEN_the_reply_says_it_was_told_THEN_it_is_marked_told_to_the_player()
      {
         NpcProfile npc = Npc();
         MeetingMemoryUpdater.ApplyReply(npc, Reply, Facts(100, "a1"));
         List<string> shown = MeetingMemoryUpdater.UntoldTo(npc.MeetingMemory, Player);

         MeetingMemoryUpdater.ApplyReply(npc, "told: 1 to Gabriel\nsecret: nothing", Facts(104, "a2", shown));

         MeetingSecret s = npc.MeetingMemory!.Secrets!.Single();
         s.WasToldTo(Player).Should().BeTrue();
         s.ToldOnDay.Should().Be(104);
         npc.MeetingMemory.UntoldTo(Player).Should().BeEmpty();
      }

      // STAKE: with a reply language set, the free text comes back translated; the labels and the bond words must still read.
      [Test]
      public void GIVEN_a_french_reply_with_english_labels_WHEN_parsed_THEN_the_bond_and_the_matters_read()
      {
         MeetingMemoryReply r = MeetingMemoryUpdater.Parse("**changed:** Je ne le crois plus un simple mercenaire.\n**bond:** understands: yes, forgives - partly, loves no, trusts no\nopen: la dette")!;

         r.Changed.Should().Be("Je ne le crois plus un simple mercenaire.");
         r.Bond!.Understands.Should().Be("yes");
         r.Bond.Forgives.Should().Be("partly");
         r.Open.Should().Equal("la dette");
      }

      // STAKE: the language rule of every memory summary applies here too, and the prompt says the labels stay English.
      [Test]
      public void GIVEN_a_reply_language_WHEN_the_update_prompt_is_built_THEN_it_asks_for_it_and_keeps_the_labels_english()
      {
         string system = MeetingMemoryUpdater.BuildSystemPrompt("Lady Ira", Player, "French");

         system.Should().Contain("Write it in French");
         system.Should().Contain("in English");
         system.Should().NotContain("on day");
      }

      // STAKE: the model thinking aloud ("The user wants me to...") must never be stored as the character's memory.
      [Test]
      public void GIVEN_a_reply_with_no_label_or_meta_reasoning_WHEN_applied_THEN_nothing_is_written()
      {
         NpcProfile npc = Npc();

         MeetingMemoryUpdater.ApplyReply(npc, "The user wants me to write a memory for Lady Ira.", Facts()).Should().BeFalse();
         MeetingMemoryUpdater.ApplyReply(npc, "A pleasant talk about grain.", Facts()).Should().BeFalse();

         npc.MeetingMemory.Should().BeNull();
      }

      // STAKE: after a succession the heir is a new person to the character; the old player's bond must not be shown to him
      // nor carried into the new record.
      [Test]
      public void GIVEN_a_record_about_another_player_WHEN_updated_or_shown_THEN_a_fresh_record_starts_and_the_old_is_not_shown()
      {
         NpcProfile npc = Npc(new MeetingMemory {AboutPlayer = "Old Father", Unsettled = new List<string> {"the father's debt"}});

         Prompt(npc).Should().NotContain("the father's debt");

         MeetingMemoryUpdater.ApplyReply(npc, "changed: I met his heir.", Facts());
         npc.MeetingMemory!.AboutPlayer.Should().Be(Player);
         npc.MeetingMemory.Unsettled.Should().BeEmpty();
      }

      // STAKE: the whole section as the character reads it, days counted at reading and never a day number said.
      [Test]
      public void GIVEN_a_record_WHEN_the_prompt_is_built_THEN_the_section_reads_exactly_so_beside_the_history()
      {
         NpcProfile npc = Npc();
         MeetingMemoryUpdater.ApplyReply(npc, Reply, Facts(100));

         string prompt = Prompt(npc, today: 103).Replace("\r\n", "\n");

         prompt.Should().Contain(
            "WHAT YOUR MEETINGS WITH GABRIEL LEFT IN YOU:\n"
            + "What those meetings changed in you, and lasts:\n"
            + "- 3 days ago: I no longer take him for a mere sellsword; he kept his word about the grain.\n"
            + "Where you stand with Gabriel: you partly understand them, you forgive them, you do not love them, you partly trust them.\n"
            + "Still unsettled between you:\n"
            + "- whether he will speak for my brother at court\n"
            + "- the debt of 2000 denars\n"
            + "What you still keep from Gabriel (yours to tell or keep):\n"
            + "- I mean to leave my husband's hall before winter.\n"
            + "This is how you feel and what you carry; let it show in how you speak, never recite it.\n");
         prompt.IndexOf("WHAT YOUR MEETINGS WITH").Should().BeGreaterThan(prompt.IndexOf("YOUR HISTORY WITH THIS PLAYER"));
         prompt.IndexOf("WHAT YOUR MEETINGS WITH").Should().BeLessThan(prompt.IndexOf("CURRENT ENCOUNTER"));
         prompt.Should().NotContain("Day 100");
      }

      // STAKE: the default must not move. No record, an empty record, or the host's switch off: the prompt is byte-identical.
      [Test]
      public void GIVEN_no_record_an_empty_record_or_the_switch_off_WHEN_the_prompt_is_built_THEN_it_is_the_same_as_without()
      {
         string bare = Prompt(Npc());
         NpcProfile recorded = Npc();
         MeetingMemoryUpdater.ApplyReply(recorded, Reply, Facts());

         Prompt(Npc(new MeetingMemory())).Should().Be(bare);
         Prompt(Npc(new MeetingMemory {Changes = null, Unsettled = null, Secrets = null, Bond = new MeetingBond()})).Should().Be(bare);
         Prompt(recorded, enabled: false).Should().Be(bare);
      }

      // STAKE (the cache and the 17/09 restatement report): a record updated while a talk is open must not appear mid-talk.
      [Test]
      public void GIVEN_a_conversation_frozen_at_opening_WHEN_the_record_changes_THEN_the_prompt_shows_the_opening_state()
      {
         NpcProfile npc = Npc();
         var frozen = new EncounterContext {LeanLevel = LeanPromptLevel.Full, MeetingMemoryFrozen = true, MeetingMemoryAtOpening = null};
         string atOpening = Prompt(Npc(), ctx: frozen);

         MeetingMemoryUpdater.ApplyReply(npc, Reply, Facts());

         Prompt(npc, ctx: frozen).Should().Be(atOpening);
      }

      // STAKE: Compact (a small local model) keeps the section short: the two latest changes, three matters, the latest secret.
      [Test]
      public void GIVEN_the_compact_prompt_WHEN_a_long_record_is_shown_THEN_it_is_trimmed()
      {
         NpcProfile npc = Npc();
         for (var i = 1; i <= 6; i++) MeetingMemoryUpdater.ApplyReply(npc, $"changed: change number {i}\nopen: m1 | m2 | m3 | m4 | m5", Facts(90 + i, "a" + i));

         string compact = Prompt(npc, lean: LeanPromptLevel.Lean);

         compact.Should().Contain("change number 6").And.Contain("change number 5").And.NotContain("change number 4");
         compact.Should().Contain("- m3").And.NotContain("- m4");
         Prompt(npc).Should().Contain("change number 1");
      }

      // STAKE (the Lean budgets): a small local model's 4k-8k context must not be eaten by this section even at its fullest:
      // every item at its longest, the store full. Compact adds at most a couple of thousand characters.
      [Test]
      public void GIVEN_a_full_record_of_longest_items_WHEN_the_compact_prompt_is_built_THEN_the_section_adds_under_2500_chars()
      {
         string longText = string.Join(" ", Enumerable.Repeat("word", 200));
         NpcProfile npc = Npc();
         for (var i = 1; i <= 10; i++)
            MeetingMemoryUpdater.ApplyReply(npc, $"changed: {i} {longText}\nbond: understands partly, forgives no, loves no, trusts partly\nopen: "
                                               + string.Join(" | ", Enumerable.Range(1, 6).Select(k => k + " " + longText)) + $"\nsecret: {i} {longText}", Facts(90 + i, "a" + i));

         int added = Prompt(npc, lean: LeanPromptLevel.Lean).Length - Prompt(Npc(), lean: LeanPromptLevel.Lean).Length;

         added.Should().BeLessThan(2500);
      }

      // STAKE: an old save has no such field and must load as before; a new save must carry the record across a reload.
      [Test]
      public void GIVEN_profile_json_with_and_without_the_record_WHEN_read_back_THEN_old_reads_null_and_new_round_trips()
      {
         NpcProfile npc = Npc();
         MeetingMemoryUpdater.ApplyReply(npc, Reply, Facts());

         NpcProfile back = JsonConvert.DeserializeObject<NpcProfile>(JsonConvert.SerializeObject(npc))!;
         back.MeetingMemory!.Bond!.Trusts.Should().Be("partly");
         back.MeetingMemory.Secrets!.Single().AnchorId.Should().Be("a1");

         NpcProfile old = JsonConvert.DeserializeObject<NpcProfile>("{\"Id\":\"x\",\"Name\":\"X\",\"Faction\":\"F\",\"Clan\":\"C\"}")!;
         old.MeetingMemory.Should().BeNull();
      }

      // STAKE: the store keeps the oldest changes out once full, so a long campaign does not grow this without end.
      [Test]
      public void GIVEN_more_changes_than_the_cap_WHEN_applied_THEN_the_oldest_go()
      {
         NpcProfile npc = Npc();
         for (var i = 1; i <= MeetingMemory.MaxChanges + 3; i++) MeetingMemoryUpdater.ApplyReply(npc, $"changed: change {i}", Facts(i, "a" + i));

         npc.MeetingMemory!.Changes!.Should().HaveCount(MeetingMemory.MaxChanges);
         npc.MeetingMemory.Changes!.First().Text.Should().Be("change 4");
      }
   }
}

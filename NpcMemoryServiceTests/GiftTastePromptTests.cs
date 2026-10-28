// Code written by Gabriel Mailhot, 27/10/2026.
// The kinds of gift a character loves and those that leave them cold reach the prompt at every adult level (Gabriel,
// 27/10/2026: "un NPC pourrait [...] preferer certains cadeaux [...] Une Dame pourrait aimer les fourrures"). GiftKind is
// saved as an integer, so its ordinals are frozen here. If this is wrong, a lady who loves furs greets a fur cloak like
// a sack of grain, a cold gift is met with feigned delight, or a save reads one kind of gift as another.

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
   public class GiftTastePromptTests
   {
      // STAKE: Gabriel's own example: a lady who loves furs is told so, and meets a cold gift without feigned delight.
      [Test]
      public void GIVEN_a_lady_who_loves_furs_WHEN_the_prompt_is_built_THEN_it_says_so()
      {
         string prompt = Build(new List<GiftKind> {GiftKind.Furs}, new List<GiftKind> {GiftKind.Weapons});

         prompt.Should().Contain("GIFTS THAT DELIGHT YOU: furs.");
         prompt.Should().Contain("GIFTS THAT LEAVE YOU COLD: weapons.");
         prompt.Should().Contain("never feigning delight");
      }

      // STAKE: a character not yet given tastes (an old save before it is read) gets no empty lines.
      [Test]
      public void GIVEN_no_tastes_yet_WHEN_the_prompt_is_built_THEN_nothing_is_said()
      {
         Build(null, null).Should().NotContain("GIFTS THAT");
      }

      // STAKE: every kind of gift is told in words.
      [Test]
      public void GIVEN_every_kind_WHEN_loved_THEN_it_is_told_in_words()
      {
         foreach (GiftKind g in System.Enum.GetValues(typeof(GiftKind)))
            if (g != GiftKind.None) Build(new List<GiftKind> {g}, null).Should().Contain("GIFTS THAT DELIGHT YOU: ", g.ToString()).And.NotContain("DELIGHT YOU: .");
      }

      // STAKE: the three What You Know sections sit in EVERY prompt at every adult level, and the Lean budget guards build a
      // profile with none of them, so they cannot see this cost (players run 8k-context local models). Measured here at
      // its largest: an anchor, a fear, a superstition and two gift lists, nothing yet learned (three reveal teachings).
      [Test]
      public void GIVEN_every_what_you_know_section_full_and_unlearned_WHEN_the_prompt_is_built_THEN_its_cost_stays_bounded()
      {
         var full = new NpcProfile {
            Id = "npc_test", Name = "Test Lady", Faction = "Sturgia", Clan = "Kostrom", ReputationWithPlayer = 30, MattersMost = MattersMost.Children,
            NatureRolled = true, Fear = Fear.ClosedSpaces, Superstition = Superstition.Thresholds,
            LovedGifts = new List<GiftKind> {GiftKind.Furs, GiftKind.Delicacies}, ColdGifts = new List<GiftKind> {GiftKind.Weapons}
         };
         var bare = new NpcProfile {Id = "npc_test", Name = "Test Lady", Faction = "Sturgia", Clan = "Kostrom", ReputationWithPlayer = 30};
         int Size(NpcProfile n) => new PromptBuilder {AdultLevel = AdultContentLevel.Off}.BuildSystemPrompt(n, new WorldState {CurrentDay = 10}, new EncounterContext()).Length;

         int added = Size(full) - Size(bare);
         TestContext.Out.WriteLine($"What You Know sections, full and unlearned: {added} characters");
         added.Should().BeLessThan(1500);
      }

      // STAKE: saved as integers; inserting or reordering a member would read one kind as another in every save.
      [Test]
      public void GIVEN_the_saved_kinds_WHEN_cast_to_int_THEN_their_ordinals_are_frozen()
      {
         ((int) GiftKind.Furs).Should().Be(1);
         ((int) GiftKind.Horses).Should().Be(7);
         ((int) GiftKind.Silver).Should().Be(11);
         System.Enum.GetValues(typeof(GiftKind)).Length.Should().Be(12);
      }

      private static string Build(List<GiftKind>? loved, List<GiftKind>? cold)
      {
         var npc = new NpcProfile {Id = "npc_test", Name = "Test Lady", Faction = "Sturgia", Clan = "Kostrom", ReputationWithPlayer = 30, LovedGifts = loved, ColdGifts = cold};

         return new PromptBuilder {AdultLevel = AdultContentLevel.Off}.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, new EncounterContext());
      }
   }
}

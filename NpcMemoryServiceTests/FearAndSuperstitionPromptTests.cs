// Code written by Gabriel Mailhot, 27/10/2026.
// A character's fear and superstition reach the prompt at every adult level (Gabriel, 27/10/2026: "nous pourrions aussi
// ajouter certaines superstitions et phobies"), and the player learns each when the character lets it show: a
// [DISCOVERY] with the reserved key fear or superstition marks it known and is never stored as free text. Both enums are
// saved as integers, so their ordinals are frozen here. If this is wrong, the character never shows what they dread or
// believe, the discovery is asked for again and again, the encyclopedia says it twice, or a save reads one fear as another.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class FearAndSuperstitionPromptTests
   {
      // STAKE: a lord who fears the sea and reads omens is told both, with how to reveal them, even with adult content off.
      [Test]
      public void GIVEN_a_fear_and_a_superstition_WHEN_the_prompt_is_built_THEN_both_are_stated_with_how_to_reveal_them()
      {
         string prompt = Build(Fear.TheSea, Superstition.Omens, fearKnown: false, superstitionKnown: false);

         prompt.Should().Contain("A FEAR OF YOURS: the sea");
         prompt.Should().Contain("A SUPERSTITION OF YOURS: you read omens");
         prompt.Should().Contain("nothing in the world bends to it");
         prompt.Should().Contain("key: fear or superstition");
      }

      // STAKE: once both are known, the character is not asked to reveal them again.
      [Test]
      public void GIVEN_both_known_WHEN_the_prompt_is_built_THEN_no_discovery_is_asked()
      {
         Build(Fear.Fire, Superstition.Charms, fearKnown: true, superstitionKnown: true).Should().NotContain("key: fear").And.NotContain("key: superstition");
      }

      // STAKE: most characters fear nothing worth naming: no empty lines for them.
      [Test]
      public void GIVEN_neither_WHEN_the_prompt_is_built_THEN_nothing_is_said()
      {
         Build(Fear.None, Superstition.None, false, false).Should().NotContain("A FEAR OF YOURS").And.NotContain("A SUPERSTITION OF YOURS");
      }

      // STAKE: the reserved keys mark each learned and store no free text, so the encyclopedia names them once.
      [Test]
      public void GIVEN_the_reserved_discoveries_WHEN_applied_THEN_each_is_marked_known_and_not_stored()
      {
         var profile = new NpcProfile {Id = "npc", Name = "Lord", Faction = "Sturgia", Clan = "Kostrom", Fear = Fear.Wolves, Superstition = Superstition.Dreams};

         ProfileMutator.Apply(profile, new ParsedResponse {Dialogue = "", Discovery = new DiscoveredTrait {Key = "fear", Description = "He fears wolves."}}, 2);
         ProfileMutator.Apply(profile, new ParsedResponse {Dialogue = "", Discovery = new DiscoveredTrait {Key = "superstition", Description = "Dreams."}}, 3);

         profile.FearKnown.Should().BeTrue();
         profile.SuperstitionKnown.Should().BeTrue();
         profile.DiscoveredTraits.Should().BeEmpty();
      }

      // STAKE: every fear and superstition is told in words.
      [Test]
      public void GIVEN_every_fear_and_superstition_WHEN_the_prompt_is_built_THEN_each_is_told_in_words()
      {
         foreach (Fear f in System.Enum.GetValues(typeof(Fear)))
            if (f != Fear.None) Build(f, Superstition.None, true, true).Should().Contain("A FEAR OF YOURS: ", f.ToString()).And.NotContain("A FEAR OF YOURS: .");
         foreach (Superstition s in System.Enum.GetValues(typeof(Superstition)))
            if (s != Superstition.None) Build(Fear.None, s, true, true).Should().Contain("A SUPERSTITION OF YOURS: you ", s.ToString()).And.NotContain("OF YOURS: you .");
      }

      // STAKE: saved as integers; inserting or reordering a member would read one as another in every save.
      [Test]
      public void GIVEN_the_saved_enums_WHEN_cast_to_int_THEN_their_ordinals_are_frozen()
      {
         ((int) Fear.TheSea).Should().Be(1);
         ((int) Fear.ClosedSpaces).Should().Be(8);
         System.Enum.GetValues(typeof(Fear)).Length.Should().Be(9);
         ((int) Superstition.Omens).Should().Be(1);
         ((int) Superstition.Thresholds).Should().Be(8);
         System.Enum.GetValues(typeof(Superstition)).Length.Should().Be(9);
      }

      private static string Build(Fear fear, Superstition superstition, bool fearKnown, bool superstitionKnown)
      {
         var npc = new NpcProfile {
            Id = "npc_test", Name = "Test Lord", Faction = "Sturgia", Clan = "Kostrom", ReputationWithPlayer = 30, NatureRolled = true, Fear = fear,
            FearKnown = fearKnown, Superstition = superstition, SuperstitionKnown = superstitionKnown
         };

         return new PromptBuilder {AdultLevel = AdultContentLevel.Off}.BuildSystemPrompt(npc, new WorldState {CurrentDay = 10}, new EncounterContext());
      }
   }
}

// Code written by Gabriel Mailhot, 26/08/2026.
// Stolen Love pillar, Increment 1b: how a THIRD-PARTY-married NPC's consent block responds to the pillar gate.
// EncounterContext.MarriedSeductionStance (fed by SeductionEligibilityPolicy in the mod) decides whether the
// affair may open despite the marriage. The default (NotApplicable) must leave the shipped infidelity rule
// exactly as it was, so a player who never enables the pillar sees no change; Resistant must slam the door no
// matter how high the regard, so an honourable or happily married NPC cannot be bought into an affair by standing
// alone; Seducible must behave like the shipped path (the affair opens at deep trust). Get the default wrong and
// every existing save's affairs change under the player; get Resistant wrong and the whole gated pillar collapses
// back into "enough regard buys anyone".

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class MarriedSeductionStanceTests
   {
      // Married to a third party, at deep trust (rep 30), so the shipped infidelity opening would normally fire.
      private static NpcProfile MarriedToAnotherNpc() => new() {
         Id = "npc_test",
         Name = "Test Wife",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         SpouseName = "Some Other Lord",
         ReputationWithPlayer = 30,
         Romantic = new RomanticProfile {IsFemale = true, Orientation = SexualOrientation.Heterosexual}
      };

      private static string Prompt(MarriedSeductionStance stance)
         => new PromptBuilder {AdultLevel = AdultContentLevel.Mature}.BuildSystemPrompt(
            MarriedToAnotherNpc(),
            new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Full, NpcSpouseIsPlayer = false, MarriedSeductionStance = stance});

      // The load-bearing non-regression: with the pillar off (the default value), the third-party-married NPC must
      // still open the affair at deep trust exactly as it shipped, or enabling nothing would silently change every
      // existing player's affairs.
      [Test]
      public void GIVEN_the_stance_is_not_applicable_WHEN_at_deep_trust_THEN_the_shipped_infidelity_opening_still_renders()
      {
         string prompt = Prompt(MarriedSeductionStance.NotApplicable);

         prompt.Should().Contain("act of infidelity against Some Other Lord");
         prompt.Should().NotContain("will NOT betray this marriage");
      }

      // The heart of the gate: a Resistant NPC (honourable, or happily married per the policy) must refuse even at
      // deep trust, so standing alone can never buy an affair. Without this the pillar's whole "earned, personality
      // true" premise is gone.
      [Test]
      public void GIVEN_the_stance_is_resistant_WHEN_at_deep_trust_THEN_they_hold_firm_and_no_affair_opens()
      {
         string prompt = Prompt(MarriedSeductionStance.Resistant);

         prompt.Should().Contain("will NOT betray this marriage");
         prompt.Should().NotContain("act of infidelity against Some Other Lord");
      }

      // A Seducible NPC (the player genuinely won them past the marriage) behaves like the shipped path: the affair
      // opens at deep trust. The pillar's difference for this one is the estrangement/divorce built in later
      // increments, not a change to the consent opening itself.
      [Test]
      public void GIVEN_the_stance_is_seducible_WHEN_at_deep_trust_THEN_the_affair_opens_as_infidelity()
      {
         string prompt = Prompt(MarriedSeductionStance.Seducible);

         prompt.Should().Contain("act of infidelity against Some Other Lord");
         prompt.Should().NotContain("will NOT betray this marriage");
      }
   }
}

// Code written by Gabriel Mailhot, 05/09/2026.
// Mystery-lover pillar (phase 3b). Once an NPC holds the player's secret-lover identity, HOW they carry it is not a
// coin flip left to the model: this policy is the ONE ruling that both the prompt (AppendHeldSecret) and any later
// mechanical use (blackmail) read, so a friend never threatens and an enemy is never forced to keep faith. Two
// failures matter. If war ever stopped trumping regard, an enemy lord at war who happened to hold a shred of private
// goodwill would keep the player's secret instead of weighing it as the lever it is. If the friend floor drifted,
// a genuine friend could tip into leverage framing and menace an ally over a secret they would in truth protect.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class HeldSecretPolicyTests
   {
      // A real friend keeps the secret in trust: at or above the confidant floor, and not at war, the framing must be
      // Confidant, or the prompt would invite an ally to menace the player over a secret they would actually protect.
      [Test]
      public void GIVEN_high_regard_and_peace_WHEN_deciding_THEN_the_npc_is_a_confidant()
         => HeldSecretPolicy.Decide(HeldSecretPolicy.ConfidantRegardFloor, hostile: false).Should().Be(HeldSecretDisposition.Confidant);

      // Personal enmity, even without open war, curdles into temptation: at or below the leverage ceiling the NPC is
      // disposed to trade on the secret, so a lord the player has wronged can hold it over them.
      [Test]
      public void GIVEN_deep_personal_enmity_WHEN_deciding_THEN_the_npc_is_disposed_to_leverage()
         => HeldSecretPolicy.Decide(HeldSecretPolicy.LeverageRegardCeiling, hostile: false).Should().Be(HeldSecretDisposition.Leverage);

      // War trumps regard: an enemy at war will weigh a lever whatever their private feeling. Even a warmly-regarded
      // NPC whose faction is at war with the player must read as Leverage, or a cross-faction affair loses all danger.
      [Test]
      public void GIVEN_high_regard_but_at_war_WHEN_deciding_THEN_war_forces_leverage()
         => HeldSecretPolicy.Decide(HeldSecretPolicy.ConfidantRegardFloor + 50, hostile: true).Should().Be(HeldSecretDisposition.Leverage);

      // The middle ground is discreet neutrality: above the leverage ceiling but below the confidant floor, and at
      // peace, the NPC neither gossips it idly nor presses it as a weapon. This is the common case and must not tip
      // either way, or ordinary acquaintances would all read as friends or all as blackmailers.
      [Test]
      public void GIVEN_neutral_regard_and_peace_WHEN_deciding_THEN_the_npc_is_discreet()
      {
         HeldSecretPolicy.Decide(0, hostile: false).Should().Be(HeldSecretDisposition.Discreet);
         HeldSecretPolicy.Decide(HeldSecretPolicy.ConfidantRegardFloor - 1, hostile: false).Should().Be(HeldSecretDisposition.Discreet);
         HeldSecretPolicy.Decide(HeldSecretPolicy.LeverageRegardCeiling + 1, hostile: false).Should().Be(HeldSecretDisposition.Discreet);
      }
   }
}

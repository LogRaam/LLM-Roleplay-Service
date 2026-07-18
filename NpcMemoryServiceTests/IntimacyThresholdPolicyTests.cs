// Code written by Gabriel Mailhot, 18/07/2026.
// Adult-prompt audit M3. This policy is the ONE source of the intimacy consent bar: the prompt asks it for
// a verdict and prints the conclusion (so a weak model never has to remember a regard value from three
// thousand tokens earlier and compare it), and the enforcement points ask it the same question. Two
// failures matter here. If the thresholds drift from what the prose used to say, existing saves silently
// change how hard intimacy is. If PermitsIntimacy ever honoured shared history, one low-regard encounter
// would unlock an NPC permanently, which is the exploit the split verdict exists to avoid. These tests pin
// the bar per disposition, the pacing relief and its floor, and the difference between a gentle refusal
// and a mechanical yes.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class IntimacyThresholdPolicyTests
   {
      // ── The bar per disposition (pins the prose the mod has always shipped) ──

      // Intimacy with a married NPC is infidelity, and the consent prose has always asked the deepest
      // trust of all for it. Pinned so a future edit to the prompt cannot quietly cheapen adultery.
      [Test]
      public void GIVEN_an_npc_married_to_another_WHEN_asking_the_bar_THEN_it_is_the_strictest()
      {
         IntimacyThresholdPolicy.Threshold(Facts(marriedToAnother: true)).Should().Be(30);
      }

      // The ordinary case, and the one the audit caught failing: an 8B model at +12 facing this bar
      // yielded, because the number to compare against was printed thousands of tokens earlier.
      [Test]
      public void GIVEN_an_ordinary_npc_WHEN_asking_the_bar_THEN_it_is_the_standard_connection()
      {
         IntimacyThresholdPolicy.Threshold(Facts()).Should().Be(20);
      }

      // A casual disposition asks only comfort and attraction, which is the whole point of the trait.
      [Test]
      public void GIVEN_a_casual_npc_WHEN_asking_the_bar_THEN_it_is_the_most_permissive()
      {
         IntimacyThresholdPolicy.Threshold(Facts(casual: true)).Should().Be(5);
      }

      // Intense burns fast but still needs the pull to be real, so it sits between casual and ordinary.
      [Test]
      public void GIVEN_an_intense_npc_WHEN_asking_the_bar_THEN_it_sits_between_casual_and_ordinary()
      {
         IntimacyThresholdPolicy.Threshold(Facts(intense: true)).Should().Be(10);
      }

      // Marriage to another outranks any disposition: a casual married NPC is still committing adultery,
      // so the strict bar must win rather than the permissive one. Pins the ordering, not just the values.
      [Test]
      public void GIVEN_a_casual_npc_married_to_another_WHEN_asking_the_bar_THEN_the_marriage_bar_wins()
      {
         IntimacyThresholdPolicy.Threshold(Facts(marriedToAnother: true, casual: true)).Should().Be(30);
      }

      // Preserves the order the consent prose has always used when BOTH preferences are present: casual is
      // checked first, so the lower bar applies. Pinned deliberately, so this stays a decision rather than
      // an accident of how the conditions happen to be written.
      [Test]
      public void GIVEN_an_npc_both_casual_and_intense_WHEN_asking_the_bar_THEN_the_casual_bar_applies()
      {
         IntimacyThresholdPolicy.Threshold(Facts(casual: true, intense: true)).Should().Be(5);
      }

      // ── The pacing dial ─────────────────────────────────────────────────────

      // The dial is the player's own taste about how readily NPCs warm, and it already loosens every other
      // gate in the mod; the consent bar follows it rather than standing alone as a fixed wall.
      [Test]
      public void GIVEN_a_generous_pacing_dial_WHEN_asking_the_bar_THEN_it_is_lowered_by_the_relief()
      {
         IntimacyThresholdPolicy.Threshold(Facts(relief: 8)).Should().Be(12);
      }

      // The floor is the safety rail: however generous the dial, an NPC who actively dislikes the player
      // must never read as consenting. Without it a large relief would drive the casual bar to zero or
      // below, and a hostile NPC at -5 regard would come out as "threshold met".
      [Test]
      public void GIVEN_a_relief_larger_than_the_bar_WHEN_asking_THEN_the_gate_never_opens_entirely()
      {
         IntimacyThresholdPolicy.Threshold(Facts(casual: true, relief: 50))
                                .Should().Be(IntimacyThresholdPolicy.MinimumThreshold);
         IntimacyThresholdPolicy.Resolve(Facts(casual: true, relief: 50, regard: -5))
                                .Should().Be(IntimacyConsentVerdict.Below);
      }

      // ── The verdicts ────────────────────────────────────────────────────────

      // The player's own spouse is exempt outright: their vows already bind them to this very person, so a
      // trust gate built to slow down strangers has no business standing between them. Already the shipped
      // behaviour in the prose; pinned here so the mechanical gate cannot contradict it.
      [Test]
      public void GIVEN_the_npc_is_the_players_spouse_WHEN_resolving_THEN_no_stranger_gate_applies()
      {
         IntimacyThresholdPolicy.Resolve(Facts(playerSpouse: true, regard: -40))
                                .Should().Be(IntimacyConsentVerdict.Exempt);
      }

      // The boundary is inclusive: regard exactly AT the bar has earned it, rather than sitting one point shy.
      [Test]
      public void GIVEN_regard_exactly_at_the_bar_WHEN_resolving_THEN_it_is_met()
      {
         IntimacyThresholdPolicy.Resolve(Facts(regard: 20)).Should().Be(IntimacyConsentVerdict.Met);
      }

      // Gabriel's design call (2026-07-18): an NPC who has already shared the player's bed must not refuse
      // like a stranger when regard has since fallen. The verdict carries that history so the prompt can
      // write a withdrawal ("I remember, but I need time") instead of amnesia, which is the exact failure
      // the mod exists to prevent. This is also what makes the new gate land softly on existing saves.
      [Test]
      public void GIVEN_a_former_lover_now_below_the_bar_WHEN_resolving_THEN_the_history_is_carried()
      {
         IntimacyThresholdPolicy.Resolve(Facts(regard: 12, priorIntimacy: true))
                                .Should().Be(IntimacyConsentVerdict.BelowWithHistory);
      }

      // The same regard with no history is the plain refusal, so the two cases stay distinguishable and the
      // prompt can voice them differently.
      [Test]
      public void GIVEN_no_shared_history_below_the_bar_WHEN_resolving_THEN_it_is_a_plain_refusal()
      {
         IntimacyThresholdPolicy.Resolve(Facts(regard: 12)).Should().Be(IntimacyConsentVerdict.Below);
      }

      // ── The mechanical answer ───────────────────────────────────────────────

      // The exploit this split exists to close: if a past affair bought a mechanical exemption, a single
      // low-regard encounter would unlock that NPC for good. A history changes the WORDS of the refusal,
      // never the answer.
      [Test]
      public void GIVEN_a_former_lover_below_the_bar_WHEN_asking_whether_intimacy_counts_THEN_it_does_not()
      {
         IntimacyThresholdPolicy.PermitsIntimacy(Facts(regard: 12, priorIntimacy: true)).Should().BeFalse();
      }

      // The two verdicts that DO permit it, so the enforcement points cannot be tightened by accident into
      // refusing a spouse or an NPC who has plainly earned the regard.
      [Test]
      public void GIVEN_a_spouse_or_an_earned_regard_WHEN_asking_whether_intimacy_counts_THEN_it_does()
      {
         IntimacyThresholdPolicy.PermitsIntimacy(Facts(playerSpouse: true)).Should().BeTrue();
         IntimacyThresholdPolicy.PermitsIntimacy(Facts(regard: 25)).Should().BeTrue();
      }

      // Fail safe: absent facts must resolve to the most restrictive answer and never throw, so a caller
      // that forgets to build them cannot accidentally open the gate.
      [Test]
      public void GIVEN_no_facts_at_all_WHEN_resolving_THEN_it_is_a_safe_refusal()
      {
         IntimacyThresholdPolicy.Resolve(null).Should().Be(IntimacyConsentVerdict.Below);
         IntimacyThresholdPolicy.PermitsIntimacy(null).Should().BeFalse();
         IntimacyThresholdPolicy.Threshold(null).Should().Be(IntimacyThresholdPolicy.StandardThreshold);
      }

      #region private

      private static IntimacyConsentFacts Facts(
         bool playerSpouse = false,
         bool marriedToAnother = false,
         bool casual = false,
         bool intense = false,
         int regard = 0,
         bool priorIntimacy = false,
         int relief = 0)
         => new() {
            NpcIsPlayerSpouse = playerSpouse,
            NpcIsMarriedToAnother = marriedToAnother,
            PrefersCasual = casual,
            PrefersIntense = intense,
            RegardWithPlayer = regard,
            HasSharedIntimacyBefore = priorIntimacy,
            ThresholdRelief = relief
         };

      #endregion
   }
}

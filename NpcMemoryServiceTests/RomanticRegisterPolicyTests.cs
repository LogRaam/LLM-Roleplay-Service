// Code written by Gabriel Mailhot, 09/09/2026.
// What this pins: how far an NPC may go in what they SAY about feeling, and, above all, what they may not say.
//
// Why it exists. yozakura12 (Nexus, 09 Sep 2026): "Sometimes, an NPC will confess their feelings to me even when
// my intimacy level with them isn't high enough. It creates a disconnect: the NPC is expressing affection, yet
// the system displays a message stating that intimacy is insufficient." And, exactly: "in Grounded, an NPC might
// confess their feelings to you even when your affinity level is only around 10. They might say something like,
// 'We've been travelling together for a year, so I've come to see you as someone special', only for a message to
// pop up saying your affinity isn't high enough."
//
// This was ours three times over. Regard reached the prompt as a COLOUR and never as a boundary ("let your
// personal regard color your warmth and candor"), so nothing ever told an NPC what they would not yet say.
// Attraction is a second axis moving independently of regard, so "Genuinely attracted" could print beside a
// regard of ten. And AppendSocialAttractionInstructions actively invited the subject on clan standing alone.
//
// Gabriel's ruling, 09/09/2026: "il faudrait que le LLM declare en connaissant deja le gating". A gate is a
// function of world state, never of what the model is about to say, so it is always knowable before the call and
// costs nothing to state. This is that, for the emotional axis, in the same shape IntimacyThresholdPolicy gave
// the physical one in July.
//
// EVERY NUMBER IS AN ANCHOR. After the 2026-09-08 regression, where one affection bar lived as a literal 50 in
// three places and moving one of them opened a window in which a companion declared their feelings and the game
// refused the bond, no new balance figure was invented here: the declaration bar is SUPPLIED BY THE HOST so it is
// the same number the confession roll uses, and the two interest floors are lifted from the two tables the prompt
// already prints from (RegardBands' first positive tier, DescribeAttraction's first positive band).
//
// If these tests fail, an NPC is about to declare a love the game will then refuse, or a real bond has gone mute.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
    [TestFixture]
    public class RomanticRegisterPolicyTests
    {
        private static RomanticRegisterFacts Facts(int regard = 0, int attraction = 0,
                                                   RomanticStatus status = RomanticStatus.None,
                                                   int bar = RomanticRegisterPolicy.DefaultDeclaredAffectionBar)
            => new() {
                AdultContentEnabled = true,
                PlayerIsCompatible = true,
                RegardWithPlayer = regard,
                AttractionToPlayer = attraction,
                Status = status,
                DeclaredAffectionBar = bar
            };

        // yozakura12's case, to the number. A companion who has travelled a year at a regard of ten is FOND, and
        // an NPC who is fond may show it; what they may not do is call it love, because the game will refuse
        // exactly that a moment later.
        [Test]
        public void GIVEN_the_reported_case_a_regard_of_ten_on_grounded_WHEN_judged_THEN_they_may_flirt_but_never_declare()
        {
            RomanticRegisterPolicy.Resolve(Facts(regard: 10)).Should().Be(RomanticRegister.Interest);
            RomanticRegisterPolicy.MayInitiateCourtship(Facts(regard: 10)).Should().BeFalse();
        }

        // The bar itself, and that it is inclusive: at the bar the confession roll is allowed to fire, so the
        // register must license the words the confession is made of. One short of it, it must not.
        [TestCase(50)] // Grounded, the shipped balance
        [TestCase(38)] // Warm
        [TestCase(28)] // Storyteller
        public void GIVEN_any_pacing_bar_WHEN_regard_reaches_it_THEN_feelings_may_be_named_and_not_one_point_sooner(int bar)
        {
            RomanticRegisterPolicy.Resolve(Facts(regard: bar, bar: bar)).Should().Be(RomanticRegister.Attachment);
            RomanticRegisterPolicy.Resolve(Facts(regard: bar - 1, bar: bar)).Should().Be(RomanticRegister.Interest);
        }

        // The host owns the bar precisely so a player who asked for a warmer campaign reaches these scenes
        // sooner. If the SDK held its own number, the dial would move the confession and leave the words behind,
        // which is the 2026-09-08 regression exactly.
        [Test]
        public void GIVEN_a_warmer_campaign_WHEN_the_host_lowers_the_bar_THEN_the_same_regard_now_licenses_a_declaration()
        {
            RomanticRegisterPolicy.Resolve(Facts(regard: 30, bar: 50)).Should().Be(RomanticRegister.Interest);
            RomanticRegisterPolicy.Resolve(Facts(regard: 30, bar: 28)).Should().Be(RomanticRegister.Attachment);
        }

        // Desire and trust are separate axes on purpose: someone may want the player without trusting them. That
        // licenses a flirtation and nothing more, which is the whole reason attraction alone must not climb past
        // Interest however high it runs.
        [Test]
        public void GIVEN_desire_without_trust_WHEN_judged_THEN_it_licenses_flirtation_and_never_a_declaration()
        {
            RomanticRegisterPolicy.Resolve(Facts(regard: 0, attraction: 100)).Should().Be(RomanticRegister.Interest);
            RomanticRegisterPolicy.MayInitiateCourtship(Facts(regard: 0, attraction: 100)).Should().BeFalse();
        }

        // Both floors are anchors and both are inclusive, so the boundary sits exactly where the two tables the
        // prompt already prints from put it: RegardBands' first positive tier, and DescribeAttraction's.
        [Test]
        public void GIVEN_a_stranger_below_both_floors_WHEN_judged_THEN_there_is_nothing_romantic_to_show_yet()
        {
            RomanticRegisterPolicy.Resolve(Facts(regard: RomanticRegisterPolicy.InterestRegardFloor - 1,
                                                 attraction: RomanticRegisterPolicy.InterestAttractionFloor - 1))
                                  .Should().Be(RomanticRegister.Platonic);

            RomanticRegisterPolicy.Resolve(Facts(regard: RomanticRegisterPolicy.InterestRegardFloor)).Should().Be(RomanticRegister.Interest);
            RomanticRegisterPolicy.Resolve(Facts(attraction: RomanticRegisterPolicy.InterestAttractionFloor)).Should().Be(RomanticRegister.Interest);
        }

        // A bond already formed answers the question before any bar is consulted. Making a spouse or a secret
        // lover pass a trust threshold to speak of love would be the amnesia the whole mod exists against.
        [TestCase(RomanticStatus.SecretLover)]
        [TestCase(RomanticStatus.Committed)]
        public void GIVEN_a_bond_that_already_exists_WHEN_judged_THEN_no_bar_stands_between_them_and_their_feelings(RomanticStatus status)
        {
            RomanticRegisterPolicy.Resolve(Facts(regard: 0, status: status)).Should().Be(RomanticRegister.Bound);
            RomanticRegisterPolicy.MayInitiateCourtship(Facts(regard: 0, status: status)).Should().BeTrue();
        }

        // The player's own spouse, whatever else is true of them: married is married, and the orientation gate
        // that governs strangers has no business between two people already wed.
        [Test]
        public void GIVEN_the_players_own_spouse_WHEN_judged_THEN_they_are_bound_whatever_else_is_true()
        {
            RomanticRegisterFacts spouse = Facts(regard: -20);
            spouse.NpcIsPlayerSpouse = true;
            spouse.PlayerIsCompatible = false;

            RomanticRegisterPolicy.Resolve(spouse).Should().Be(RomanticRegister.Bound);
        }

        // A romance that is over makes no new claim, at any regard. This governs only what may be freshly
        // DECLARED: the romantic status prose still renders, so a former lover is never answered as a stranger.
        [TestCase(RomanticStatus.Estranged)]
        [TestCase(RomanticStatus.Broken)]
        public void GIVEN_a_romance_that_has_ended_WHEN_judged_THEN_no_new_claim_may_be_made_however_high_the_regard(RomanticStatus status)
        {
            RomanticRegisterPolicy.Resolve(Facts(regard: 90, attraction: 90, status: status)).Should().Be(RomanticRegister.Platonic);
            RomanticRegisterPolicy.MayInitiateCourtship(Facts(regard: 90, status: status)).Should().BeFalse();
        }

        // A campaign-wide setting must hold at every door, and orientation is not negotiable by regard. Both are
        // checked before anything else can raise the register.
        [Test]
        public void GIVEN_the_arc_is_switched_off_or_the_player_is_no_object_of_desire_WHEN_judged_THEN_nothing_romantic_is_licensed()
        {
            RomanticRegisterFacts adultOff = Facts(regard: 90, attraction: 90);
            adultOff.AdultContentEnabled = false;

            RomanticRegisterFacts incompatible = Facts(regard: 90, attraction: 90);
            incompatible.PlayerIsCompatible = false;

            RomanticRegisterPolicy.Resolve(adultOff).Should().Be(RomanticRegister.Platonic);
            RomanticRegisterPolicy.Resolve(incompatible).Should().Be(RomanticRegister.Platonic);
        }

        // However generous a host's dial, declaring love must stay above merely noticing someone, or the two
        // registers collapse into one and an NPC declares to a stranger on the warmest setting.
        [Test]
        public void GIVEN_a_host_that_drops_the_bar_to_nothing_WHEN_judged_THEN_a_declaration_still_outranks_a_glance()
        {
            RomanticRegisterPolicy.Bar(Facts(bar: 0)).Should().Be(RomanticRegisterPolicy.InterestRegardFloor);
            RomanticRegisterPolicy.Bar(Facts(bar: -100)).Should().Be(RomanticRegisterPolicy.InterestRegardFloor);
            RomanticRegisterPolicy.Resolve(Facts(regard: 0, bar: 0)).Should().Be(RomanticRegister.Platonic);
        }

        // Missing facts are never permission. This is fed from live campaign reads that can be null mid
        // transition, and the safe answer to "I do not know" is that nothing romantic is licensed.
        [Test]
        public void GIVEN_no_facts_at_all_WHEN_judged_THEN_it_stays_platonic_rather_than_assuming()
        {
            RomanticRegisterPolicy.Resolve(null).Should().Be(RomanticRegister.Platonic);
            RomanticRegisterPolicy.MayInitiateCourtship(null).Should().BeFalse();
            RomanticRegisterPolicy.Bar(null).Should().Be(RomanticRegisterPolicy.DefaultDeclaredAffectionBar);
        }

        // The default is the shipped Grounded balance, so a host that sets nothing behaves exactly as it always
        // did. Anchored to the mod's own CompanionSecretAffectionPolicy.MinRegard rather than chosen here.
        [Test]
        public void GIVEN_a_host_that_supplies_no_bar_WHEN_judged_THEN_the_shipped_grounded_balance_is_unchanged()
        {
            RomanticRegisterPolicy.DefaultDeclaredAffectionBar.Should().Be(50);
            RomanticRegisterPolicy.Resolve(new RomanticRegisterFacts {
                AdultContentEnabled = true, PlayerIsCompatible = true, RegardWithPlayer = 49
            }).Should().Be(RomanticRegister.Interest);
        }
    }
}

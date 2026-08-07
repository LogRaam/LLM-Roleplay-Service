// Code written by Gabriel Mailhot, 05/08/2026.
// follow_me (1:1 bridge action, player wish: a companion promising to "stay by my side", the map-escort half
// that is doable now): a lord's own party escorts the player's across the map for a bounded term, distinct
// from join_party (the lord keeps their own troops and command). These tests pin the SAME conditional-teaching
// contract every other 1:1 bridge action already carries (join_as_mercenary/end_mercenary,
// MercenaryEndTeachingTests): follow_me is taught only when EncounterContext.NpcCanEscortPlayer is true, never
// otherwise (a model must not invent an escort the game bridge cannot honor), and its mirror, dismiss_escort,
// is taught only while EncounterContext.NpcIsEscortingPlayer is true.
//
// v1.34.3 (GLM-5.2 player report): a player asked a keep lord to follow them on the map; the model NARRATED
// riding out in purple ("as if we rode out from the city") but never emitted [ACTION] type: follow_me, so the
// lord's party never moved. Two prompt-side reinforcements, mirroring the proven change_relation weak-model
// fix: (1) inside the NpcCanEscortPlayer gate, an imperative "if you agree, emit follow_me; narration alone
// does nothing"; (2) the anti-empty-promise block (STAY WITHIN WHAT YOU KNOW) names riding out as an
// un-offered deed, so a lord with no field party is not agreed-to-in-prose either. The tests below pin both.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class FollowMeTeachingTests
   {
      // Matched on the em-dash-free tail of the real heading, so the constant carries no character Gabriel strips.
      private const string DismissEscortHeading = "YOUR PARTY CURRENTLY RIDES WITH THE PLAYER";
      private const string DismissEscortActionType = "type: dismiss_escort";
      private const string FollowMeHeading = "YOU MAY RIDE WITH THE PLAYER FOR A WHILE";
      private const string FollowMeActionType = "type: follow_me";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder {AdultLevel = AdultContentLevel.Off}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The core deed the task exists for: a lord who can genuinely honour it must be taught the actual action
      // that sets his party to follow, or "ride with me" stays an empty promise (the same class of bug
      // join_party's own header documents: an LLM agreeing to a deed with no matching [ACTION]).
      [Test]
      public void GIVEN_the_npc_can_escort_the_player_WHEN_built_THEN_follow_me_is_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcCanEscortPlayer = true
         });

         prompt.Should().Contain(FollowMeHeading);
         prompt.Should().Contain(FollowMeActionType);
      }

      // The other half of the conditional-teaching contract: when the game has not confirmed the lord can
      // truly escort (already bound to an army, at war, already escorting, or simply leads no party of their
      // own), the model must never be handed an action the bridge would only refuse.
      [Test]
      public void GIVEN_the_npc_cannot_escort_the_player_WHEN_built_THEN_follow_me_is_not_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcCanEscortPlayer = false
         });

         prompt.Should().NotContain(FollowMeHeading);
         prompt.Should().NotContain(FollowMeActionType);
      }

      // Symmetric with every other 1:1 offer's own captive carve-out (AppendMercenaryOffer, AppendRecruitment):
      // a captor holding the player prisoner is not offering to escort them anywhere, even if the underlying
      // eligibility fact happens to be true.
      [Test]
      public void GIVEN_the_player_is_a_captive_WHEN_built_THEN_follow_me_is_not_taught_even_if_eligible()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Captive,
            NpcCanEscortPlayer = true
         });

         prompt.Should().NotContain(FollowMeHeading);
      }

      // dismiss_escort's own half of the contract: while an escort is genuinely active, the player (or the
      // lord themselves) must be able to end it early, or the arrangement could only ever run its full term.
      [Test]
      public void GIVEN_the_npc_is_currently_escorting_the_player_WHEN_built_THEN_dismiss_escort_is_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcIsEscortingPlayer = true
         });

         prompt.Should().Contain(DismissEscortHeading);
         prompt.Should().Contain(DismissEscortActionType);
      }

      // No active escort, no dismissal to offer: teaching dismiss_escort here would hand the model an action
      // the bridge has nothing to end.
      [Test]
      public void GIVEN_the_npc_is_not_currently_escorting_the_player_WHEN_built_THEN_dismiss_escort_is_not_taught()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcIsEscortingPlayer = false
         });

         prompt.Should().NotContain(DismissEscortHeading);
         prompt.Should().NotContain(DismissEscortActionType);
      }

      // v1.34.3 reinforcement 1 (STAKE: GLM 5.2 agreed to ride out and narrated it in purple prose but never
      // emitted follow_me, so the lord's party never followed and the player was left standing). Inside the
      // escort gate, the prompt must carry the imperative that the action is the ONLY thing that moves the
      // party, mirroring the change_relation "recorded ONLY from that block" fix for the same weak-model class.
      [Test]
      public void GIVEN_the_npc_can_escort_the_player_WHEN_built_THEN_the_emit_or_nothing_directive_is_present()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcCanEscortPlayer = true
         });

         prompt.Should().Contain("follow_me IS THE ONLY THING THAT MAKES THE ESCORT REAL");
         prompt.Should().Contain("your party follows theirs ONLY from that block");
         prompt.Should().Contain("Agreeing in [DIALOGUE] without emitting follow_me does nothing");
      }

      // v1.34.3 reinforcement 1, kept inside its gate (STAKE: the emit-or-nothing directive must never leak
      // into a prompt where no escort is offered, or the model is told to emit an action it was never handed).
      [Test]
      public void GIVEN_the_npc_cannot_escort_the_player_WHEN_built_THEN_the_emit_or_nothing_directive_is_absent()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            NpcCanEscortPlayer = false
         });

         prompt.Should().NotContain("follow_me IS THE ONLY THING THAT MAKES THE ESCORT REAL");
      }

      // v1.34.3 reinforcement 2 (STAKE: GLM 5.2 also narrated "we ride out" when follow_me was NOT offered at
      // all, e.g. a governor or notable leading no field party). The anti-empty-promise block must name riding
      // out as an un-offered deed, so it is refused/deflected instead of narrated as accomplished. This is
      // Full-only, following the same Lean/Full split as the change_relation reinforcement (Lean has no budget).
      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_the_anti_empty_promise_block_covers_riding_out()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            LeanLevel = LeanPromptLevel.Full
         });

         prompt.Should().Contain("This covers RIDING OUT together or your party following the player's on the map");
         prompt.Should().Contain("never agree to it or narrate it as if you rode out");
      }

      // The other half of the Lean/Full split: the Full-only ride-out elaboration must not leak into Lean,
      // which the LeanPromptPolicyTests byte budget cannot spare (STAKE: a regrown Lean prompt overflows a
      // small model's context and it returns nothing usable).
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_ride_out_elaboration_is_absent()
      {
         string prompt = Build(new EncounterContext {
            PlayerStatus = PlayerStatusVsNpc.Free,
            LeanLevel = LeanPromptLevel.Lean
         });

         prompt.Should().NotContain("This covers RIDING OUT together");
      }
   }
}

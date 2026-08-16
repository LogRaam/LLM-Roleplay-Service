// Code written by Gabriel Mailhot, 13/08/2026.
// ActionInterpreterContextBuilder projects the live encounter + the NPC profile into the SETTING/PLAYER/YOU digest
// the PROSE + INTERPRETER action interpreter reads before it writes a memory. It replaces a hardcoded facts string,
// and the whole point is grounding and calibration: a memory must be anchored to the real place and witnesses, the
// NPC's own nature must colour it, and the player must be named consistently. The spike's first interpreter, given
// no player reference, once recorded the player as "the coward" and baked that one-time insult into a permanent
// memory. These tests pin the properties that guard against that class of rot: the three labelled lines are always
// present (so the interpreter always gets its digest), a partial context degrades to real clauses and never the
// word "null" (so a memory is never grounded in a placeholder), the player is a plain label and never an epithet,
// and each captivity standing reads correctly (so "your prisoner" and "your captor" are never confused, which
// would invert who did what to whom in the record).

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
   public class ActionInterpreterContextBuilderTests
   {
      // A memory is worthless to grounding if it cannot say WHERE it happened, WHO saw it, and how the NPC stands.
      // The full projection must carry every anchor the interpreter needs at once: the real place name, a witness by
      // name, the player named by the given label, the NPC's own label, and the signed regard that calibrates tone.
      // If any of these drops out, the interpreter invents it, which is exactly what the spike set out to stop.
      [Test]
      public void GIVEN_a_full_context_WHEN_projected_THEN_it_carries_place_witness_player_label_regard_and_npc_label()
      {
         var context = new EncounterContext
         {
            CurrentLocationName = "Pravend",
            Scene = SceneType.Settlement,
            PlayerStatus = PlayerStatusVsNpc.Free,
            WarStatus = DiplomaticStatus.AtWar,
            Witnesses = new List<WitnessEntry>
            {
               new WitnessEntry {Name = "Derthert", RelationToNpc = "your liege"}
            }
         };

         string digest = ActionInterpreterContextBuilder.Project(context, Profile(regard: 12), "Aldric");

         digest.Should().Contain("SETTING:");
         digest.Should().Contain("PLAYER:");
         digest.Should().Contain("YOU:");
         digest.Should().Contain("in Pravend");
         digest.Should().Contain("Derthert");
         digest.Should().Contain("Aldric");
         digest.Should().Contain("Caladog of Fen Company");
         digest.Should().Contain("+12");
         digest.Should().Contain("at war");
      }

      // The context is rebuilt every turn from live game state, so on a road encounter with no settlement, no
      // witnesses, and no diplomatic standing, most fields are null. The projection must still hand the interpreter
      // its three labelled lines (so it always has a digest to read) and must never leak the literal "null" into a
      // clause, or that placeholder becomes the "place" the interpreter grounds a permanent memory in.
      [Test]
      public void GIVEN_a_near_empty_context_WHEN_projected_THEN_it_returns_the_three_labels_without_throwing_or_printing_null()
      {
         var context = new EncounterContext();

         string digest = ActionInterpreterContextBuilder.Project(context, Profile());

         digest.Should().Contain("SETTING:");
         digest.Should().Contain("PLAYER:");
         digest.Should().Contain("YOU:");
         digest.Should().Contain("alone"); // the explicit no-witness default, not an empty clause
         digest.ToLowerInvariant().Should().NotContain("null");
      }

      // Belt and braces on the never-throw contract: even a wholly null context (a turn built before any game state
      // was available) must still yield the digest rather than a NullReferenceException that would abort the whole
      // interpretation turn. A degraded digest keeps the second-model chain alive; an exception kills it.
      [Test]
      public void GIVEN_a_null_context_WHEN_projected_THEN_it_still_returns_the_three_labels_without_throwing()
      {
         string digest = ActionInterpreterContextBuilder.Project(null, Profile());

         digest.Should().Contain("SETTING:");
         digest.Should().Contain("PLAYER:");
         digest.Should().Contain("YOU:");
         digest.ToLowerInvariant().Should().NotContain("null");
      }

      // The exact bug that motivated the builder: the interpreter, told nothing about how to name the player, wrote
      // "the coward" into the memory. The digest must state the real player label and must never itself introduce an
      // epithet; the interpreter can only reuse what the digest gives it, so a clean label here is what keeps the
      // remembered history from rotting into insults.
      [Test]
      public void GIVEN_a_player_label_WHEN_projected_THEN_the_player_is_named_by_that_label_and_no_epithet_appears()
      {
         var context = new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Stranger};

         string digest = ActionInterpreterContextBuilder.Project(context, Profile(), "Aldric");

         digest.Should().Contain("Aldric");
         digest.Should().NotContain("the coward");
         digest.Should().NotContain("the stranger,"); // Stranger is a STANDING, never used as a name for the player
      }

      // With no real name to hand (the SDK's own callers and tests), the projection must still refer to the player
      // by a neutral, plain label, never a blank or an invented one. "the player" is a reference, not an epithet, so
      // a memory written from it stays factual instead of judgemental.
      [Test]
      public void GIVEN_no_player_label_WHEN_projected_THEN_the_player_is_referred_to_by_a_neutral_label()
      {
         string digest = ActionInterpreterContextBuilder.Project(new EncounterContext(), Profile());

         digest.Should().Contain("the player");
      }

      // Captive and captor are mirror images, and confusing them would invert who holds whom in the recorded memory
      // (a captured player remembered as the captor, or vice versa). Each PlayerStatus must yield its own sensible
      // standing phrase, and the ordinary free case must add none, so the interpreter grounds the power relation the
      // right way round.
      [Test]
      public void GIVEN_captive_captor_and_free_statuses_WHEN_projected_THEN_each_yields_the_correct_standing_phrase()
      {
         string captive = ActionInterpreterContextBuilder.Project(
            new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Captive}, Profile(), "Aldric");
         string captor = ActionInterpreterContextBuilder.Project(
            new EncounterContext {PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive}, Profile(), "Aldric");
         string free = ActionInterpreterContextBuilder.Project(
            new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Free}, Profile(), "Aldric");

         captive.Should().Contain("your prisoner");
         captor.Should().Contain("your captor");
         free.Should().NotContain("prisoner");
         free.Should().NotContain("captor");
      }

      // The nature descriptor is what CALIBRATES the interpreter's tone: a pitiless lord's warmth reads differently
      // from a kind one's. The SDK profile has no raw trait levels, so the projection must surface the descriptor it
      // does hold (Trait). If it dropped, every NPC's memories would read in the same flat voice.
      [Test]
      public void GIVEN_an_npc_with_a_trait_WHEN_projected_THEN_the_nature_descriptor_appears_on_the_you_line()
      {
         string digest = ActionInterpreterContextBuilder.Project(new EncounterContext(), Profile(trait: "Pitiless"), "Aldric");

         digest.Should().Contain("Pitiless");
      }

      // Negative regard must render with its minus sign, not just a bare number the interpreter could read as positive.
      // The sign is the single fact that tells the interpreter whether the NPC's baseline toward the player is warm or
      // hostile, which colours how it reads an ambiguous line of prose.
      [Test]
      public void GIVEN_a_negative_regard_WHEN_projected_THEN_it_renders_with_an_explicit_minus_sign()
      {
         string digest = ActionInterpreterContextBuilder.Project(new EncounterContext(), Profile(regard: -8), "Aldric");

         digest.Should().Contain("-8");
      }

      // The exact bug this flag exists to fix (rp_bench scene 1, 2026-08-14): a STRANGER scene reads "regard +0,
      // never met before" on EVERY turn (the digest carries no per-turn recency), so the interpreter kept re-tagging
      // turns 2-7 of the SAME ongoing exchange as first_meeting, when only turn 1 genuinely was. The caller (the mod's
      // live chat / bench) is the only one who knows the turn index, so it must be able to hand the builder a hard
      // fact to key on, and that fact must actually reach the digest text the interpreter reads.
      [Test]
      public void GIVEN_conversationInProgress_true_WHEN_projected_THEN_the_digest_states_the_conversation_is_already_under_way()
      {
         string digest = ActionInterpreterContextBuilder.Project(
            new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Stranger}, Profile(), "Aldric", conversationInProgress: true);

         digest.Should().Contain("Aldric");
         digest.Should().Contain("already been speaking with Aldric");
         digest.Should().Contain("later turn");
      }

      // The flip side: a false flag (every existing caller, and turn 1 of a fresh scene) must leave the digest
      // untouched, byte-for-byte identical to the 3-argument overload. Otherwise every caller that has not been
      // taught about the new flag yet would see its prompt silently change shape.
      [Test]
      public void GIVEN_conversationInProgress_false_WHEN_projected_THEN_the_digest_is_identical_to_the_flagless_overload()
      {
         var context = new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Stranger};

         string withFlag = ActionInterpreterContextBuilder.Project(context, Profile(), "Aldric", conversationInProgress: false);
         string withoutFlag = ActionInterpreterContextBuilder.Project(context, Profile(), "Aldric");

         withFlag.Should().Be(withoutFlag);
         withFlag.Should().NotContain("already been speaking");
      }

      // rp_bench scene 4 finding (2026-08-15): the interpret step kept MISSING end_conversation at a captive scene's
      // close, because the integrated (direct) path gets an explicit "STAGE - CONCLUDE: emit end_conversation"
      // directive and the interpreter had no scene-stage signal at all. Keyed on the same director stage the direct
      // prompt uses, the digest must now cue the close so the interpreter emits end_conversation as reliably as direct.
      [Test]
      public void GIVEN_scene_stage_conclude_WHEN_projected_THEN_the_digest_cues_end_conversation()
      {
         string digest = ActionInterpreterContextBuilder.Project(
            new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Captive, SceneStage = CaptiveSceneStage.Conclude},
            Profile(), "Aldric", conversationInProgress: true);

         digest.Should().Contain("CLOSE this beat");
         digest.Should().Contain("end_conversation");
      }

      // The flip side: a mid-scene stage must NOT cue the close, or the interpreter would end the scene early on every
      // beat. The conclude cue is precise to the director's terminal stage; anything else leaves the digest silent on it.
      [Test]
      public void GIVEN_a_mid_scene_stage_WHEN_projected_THEN_no_conclude_cue_appears()
      {
         string digest = ActionInterpreterContextBuilder.Project(
            new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Captive, SceneStage = CaptiveSceneStage.Intensify},
            Profile(), "Aldric", conversationInProgress: true);

         digest.Should().NotContain("end_conversation");
      }

      // rp_bench scene 2 finding (2026-08-15): the interpret step inflated change_relation (five straight +8 beats)
      // because the interpreter saw only a bare regard number, while the direct path carries the full romantic bond
      // and calibrates accordingly. Stating the standing bond in the digest gives the interpreter the same basis:
      // warmth between committed partners is the expression of the bond, not a fresh +8 each beat.
      [Test]
      public void GIVEN_a_committed_romantic_status_WHEN_projected_THEN_the_bond_is_stated_on_the_you_line()
      {
         NpcProfile p = Profile(regard: 45);
         p.Romantic = new RomanticProfile {Status = RomanticStatus.Committed};

         string digest = ActionInterpreterContextBuilder.Project(new EncounterContext(), p, "Aldric");

         digest.Should().Contain("committed partner");
      }

      // An intimate PAST is the other half of the calibration signal: a bond with history is not new, so a warm beat
      // is continuation, not a first spark worth a large delta. A recorded Intimacy (or Flirt) event must surface as a
      // stated history so the interpreter reads the escalation as expression, not a fresh gain.
      [Test]
      public void GIVEN_an_intimate_event_in_history_WHEN_projected_THEN_the_digest_states_an_intimate_history()
      {
         NpcProfile p = Profile(regard: 30);
         p.Events.Add(new NotableEvent(1, NotableEventType.Intimacy, "A night shared."));

         string digest = ActionInterpreterContextBuilder.Project(new EncounterContext(), p, "Aldric");

         digest.Should().Contain("intimate history");
      }

      // The default case (a stranger or a plain acquaintance with no romantic status and no intimate memory) must add
      // NO bond clause: inventing one would misframe an ordinary exchange as a romance and skew every tag from it.
      [Test]
      public void GIVEN_no_romantic_bond_or_history_WHEN_projected_THEN_no_bond_clause_appears()
      {
         string digest = ActionInterpreterContextBuilder.Project(new EncounterContext(), Profile(), "Aldric");

         digest.Should().NotContain("committed partner");
         digest.Should().NotContain("intimate history");
      }

      #region private

      /// <summary>A minimal but valid scene-1-style profile: the required identity fields, plus regard and nature under test.</summary>
      private static NpcProfile Profile(int regard = 0, string trait = "Pitiless", string? personality = null)
         => new NpcProfile
         {
            Id = "npc-1",
            Name = "Caladog",
            Clan = "Fen Company",
            Faction = "Battania",
            ReputationWithPlayer = regard,
            Trait = trait,
            Personality = personality
         };

      #endregion
   }
}

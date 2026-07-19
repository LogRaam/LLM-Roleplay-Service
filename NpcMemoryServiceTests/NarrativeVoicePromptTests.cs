// The unified narrative voice (ratified 2026-07-17, audit P2): ordinary conversations carry three
// teachings — NARRATIVE VOICE (third-person narration from OUTSIDE the player's head; the player is
// "you" only for what reaches them from outside), READING THE PLAYER'S TURN (the player's *stage
// directions* are accomplished fact; what an act MEANS is judged in character), and VARY THE SHAPE
// (a conversation breathes like a novel, never the same *action*-speech-narration template). Captive
// scenes keep their own dedicated voice contract instead, which was rewritten from SECOND PERSON to
// a neutral third-person narrator at the same time. These tests pin both faces and the capteur
// contract that was deliberately left untouched.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class NarrativeVoicePromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Lord",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string BuildLordPrompt(AdultContentLevel level = AdultContentLevel.Off)
         => new PromptBuilder {AdultLevel = level}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext());

      private static string BuildOpeningPrompt()
         => new PromptBuilder {AdultLevel = AdultContentLevel.Off}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {IsConversationOpening = true});

      private static string BuildCaptivePrompt()
         => new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {
               PlayerStatus = PlayerStatusVsNpc.Captive,
               CaptiveIntent = CaptiveSceneIntent.PersonalDesire
            });

      private static string BuildCompanionVictimPrompt()
         => new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, PlayerIsFemale = true}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {
               PlayerStatus = PlayerStatusVsNpc.Captive,
               CaptiveIntent = CaptiveSceneIntent.Domination,
               CaptiveVictimName = "Tezzeret",
               CaptiveVictimIsFemale = false
            });

      private static string BuildCaptorScenePrompt()
         => new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore}.BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {
               IsCaptorScene = true,
               CaptiveIntent = CaptiveSceneIntent.Interrogation
            });

      // Gabriel's correction, in game, 2026-07-18. The ratified voice told the model to weave gestures,
      // speech AND scene into "one flow", so all three landed inside [DIALOGUE] and the violet narration
      // line never appeared in an ordinary conversation (it had only ever been taught to captive scenes).
      // The two are different things and must stay apart: a gesture punctuates the speech it belongs to,
      // while the scene is a camera on the room. Pins that the gestures are told to stay in [DIALOGUE].
      [Test]
      public void GIVEN_a_standard_lord_prompt_WHEN_built_THEN_gestures_are_kept_inside_the_dialogue()
      {
         string prompt = BuildLordPrompt();

         prompt.Should().Contain("YOUR GESTURES BELONG WITH YOUR WORDS, inside [DIALOGUE]");
         prompt.Should().Contain("the rhythm of the speech they punctuate");
      }

      // The other half of the same rule, and the one that produces the violet line: the SETTING is the
      // only thing [NARRATION] carries in an ordinary conversation. Without this the model has no reason
      // to emit the block at all, which is exactly what the Mina transcript showed.
      [Test]
      public void GIVEN_a_standard_lord_prompt_WHEN_built_THEN_the_scene_is_routed_to_narration()
      {
         string prompt = BuildLordPrompt();

         prompt.Should().Contain("THE SCENE ITSELF IS NOT YOURS TO SPEAK: put it in [NARRATION]");
         prompt.Should().Contain("a neutral camera on the SETTING");
      }

      // Gabriel's refinement (2026-07-19), after several conversations produced no violet line at all: the
      // FIRST exchange establishes the scene, and only afterwards does narration go back to being sparing.
      // Rarity alone left the opening to the model's judgement, and it kept deciding the moment was not
      // worth one, so the feature was invisible in play.
      [Test]
      public void GIVEN_the_conversation_is_opening_WHEN_built_THEN_this_turn_is_told_to_set_the_scene()
      {
         string prompt = BuildOpeningPrompt();

         prompt.Should().Contain("THIS TURN OPENS THE SCENE");
         prompt.Should().Contain("the light, the sounds");
      }

      // And the restraint must NOT be preached on the same turn: telling the model the block is rare while
      // asking it to open with one is a split instruction, which a weaker model resolves by omitting it.
      [Test]
      public void GIVEN_the_conversation_is_opening_WHEN_built_THEN_the_contract_does_not_also_call_it_rare()
      {
         string prompt = BuildOpeningPrompt();

         prompt.Should().NotContain("optional, and rare");
         prompt.Should().NotContain("Many turns need none at all");
      }

      // The other side of the same rule: once past the opening, the sparing wording comes back, or every
      // turn would carry a scene note and the violet line would stop meaning anything.
      [Test]
      public void GIVEN_the_conversation_is_under_way_WHEN_built_THEN_narration_is_sparing_again()
      {
         string prompt = BuildLordPrompt();

         prompt.Should().NotContain("THIS TURN OPENS THE SCENE");
         prompt.Should().Contain("Many turns need none at all");
      }

      // Restraint is load-bearing, not politeness: the model reaches for every block the contract lists,
      // so a scene note offered without a rarity rule comes back each turn and the violet line stops
      // meaning anything. Pins that the ordinary format contract offers the block AND bounds it.
      [Test]
      public void GIVEN_a_standard_lord_prompt_WHEN_built_THEN_narration_is_offered_but_marked_rare()
      {
         string prompt = BuildLordPrompt();

         prompt.Should().Contain("[NARRATION]");
         prompt.Should().Contain("only when the scene itself is worth a line");
         prompt.Should().Contain("Many turns need none at all");
      }

      // The core of the ratified style: every ordinary reply is governed by a third-person narrative
      // voice — the player's interiority is never narrated, "you" only for what reaches them from
      // outside.
      [Test]
      public void GIVEN_a_standard_lord_prompt_WHEN_built_THEN_the_narrative_voice_block_is_present()
      {
         string prompt = BuildLordPrompt();

         prompt.Should().Contain("NARRATIVE VOICE (governs every reply)");
         prompt.Should().Contain("never from inside the player's head");
      }

      // Ratified with Gabriel's example turn: the player's *stage directions* are accomplished fact
      // (never contradicted, rewritten, or ignored); what the act MEANS is the NPC's to judge, in
      // character. This asymmetry is what keeps the exchange credible.
      [Test]
      public void GIVEN_a_standard_lord_prompt_WHEN_built_THEN_the_reading_the_players_turn_block_is_present()
      {
         string prompt = BuildLordPrompt();

         prompt.Should().Contain("READING THE PLAYER'S TURN:");
         prompt.Should().Contain("accomplished fact");
         prompt.Should().Contain("what it MEANS is yours to judge, in character");
      }

      // The most insidious repetition is structural: answering every turn with the same
      // *action*-speech-narration template. The vary-the-shape rule lets the form follow the moment —
      // with the guard-rail that a short reply is a dramatic choice, never a lazy one.
      [Test]
      public void GIVEN_a_standard_lord_prompt_WHEN_built_THEN_the_vary_the_shape_block_is_present()
      {
         string prompt = BuildLordPrompt();

         prompt.Should().Contain("VARY THE SHAPE OF YOUR REPLIES, AS A NOVEL DOES:");
         prompt.Should().Contain("A short reply is a dramatic choice, never a lazy one");
      }

      // Captive scenes define their OWN strict voice contract (AppendCaptiveVoiceAndPerspective /
      // AppendPlayerCaptorSceneRules / AppendCaptiveCompanionRules); the general guidance would
      // contradict it, so all three blocks must stay out of a captive prompt.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_the_general_voice_blocks_are_absent()
      {
         string prompt = BuildCaptivePrompt();

         prompt.Should().NotContain("NARRATIVE VOICE (governs every reply)");
         prompt.Should().NotContain("READING THE PLAYER'S TURN:");
         prompt.Should().NotContain("VARY THE SHAPE OF YOUR REPLIES, AS A NOVEL DOES:");
      }

      // Lean is a hard token budget for small local models (pinned by LeanPromptPolicyTests): ~2k
      // chars of style guidance would bust it, so the voice blocks ride the Full prompt only.
      [Test]
      public void GIVEN_a_lean_prompt_WHEN_built_THEN_the_voice_blocks_are_not_carried()
      {
         string prompt = new PromptBuilder().BuildSystemPrompt(
            Npc(), new WorldState {CurrentDay = 10},
            new EncounterContext {LeanLevel = LeanPromptLevel.Lean});

         prompt.Should().NotContain("NARRATIVE VOICE (governs every reply)");
         prompt.Should().NotContain("VARY THE SHAPE OF YOUR REPLIES, AS A NOVEL DOES:");
      }

      // The 2026-07-17 reversal of the ratified SECOND PERSON design: the captive [NARRATION] is now
      // a NEUTRAL narrator in the third person, and the old teaching must be gone entirely. The
      // demarcation that replaced it: imposed physical sensations are fully narrated (they are what
      // makes the scene dramatic); the player's thoughts, decisions, words and voluntary deeds never.
      [Test]
      public void GIVEN_a_captive_scene_WHEN_built_THEN_narration_is_taught_in_the_third_person_never_the_second()
      {
         string prompt = BuildCaptivePrompt();

         prompt.Should().NotContain("SECOND PERSON");
         prompt.Should().NotContain("MAKE THE PLAYER FEEL IT");
         prompt.Should().Contain("in the third person");
         prompt.Should().Contain("their mind and their choices belong to the player");
      }

      // Same reversal for the companion-victim variant (the player is the bound onlooker): third
      // person neutral narrator, the player's mind and choices still off-limits.
      [Test]
      public void GIVEN_a_companion_victim_scene_WHEN_built_THEN_narration_is_taught_in_the_third_person_never_the_second()
      {
         string prompt = BuildCompanionVictimPrompt();

         prompt.Should().NotContain("SECOND PERSON");
         prompt.Should().Contain("in the third person");
         prompt.Should().Contain("their mind and their choices belong to the");
      }

      // The capteur scene (NPC held by the player) was deliberately NOT rewritten: its "you"
      // designates the player's own outward acts, which is conformant with the ratified style.
      [Test]
      public void GIVEN_a_captor_scene_WHEN_built_THEN_the_captor_voice_contract_is_unchanged()
      {
         string prompt = BuildCaptorScenePrompt();

         prompt.Should().Contain("VOICE AND POINT OF VIEW — THIS SCENE IS SEEN FROM THE CAPTOR'S SIDE:");
         prompt.Should().Contain("wrench her face up");
      }
   }
}
